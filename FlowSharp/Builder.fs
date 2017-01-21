namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.HistoryWalker
open FlowSharp.EventPatterns
open FlowSharp.ExecutionContext

type Builder (DecisionTask:DecisionTask, ReverseOrder:bool, ContextManager:IContextManager option) =
    let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
    let walker = HistoryWalker(DecisionTask.Events, ReverseOrder)
    let mutable blockFlag = false
    let mutable exceptionEvents = List.empty<int64>

    let AddExceptionEventId (eventId:int64) =
        exceptionEvents <- eventId :: exceptionEvents

    let Wait() =
        blockFlag <- true
        response

    let ReadContext () = 
        match ContextManager with
        | Some(cm) ->
            let completed = walker.FindLatestDecisionTaskCompleted()
            match completed with
            | Some(c) -> 
                cm.Read(c.DecisionTaskCompletedEventAttributes.ExecutionContext)
            | None -> ()
        | None -> ()
    
    let WriteContext () =
        match ContextManager with
        | Some(cm) ->
            response.ExecutionContext <- cm.Write()
        | None -> ()

    new (decisionTask:DecisionTask) = Builder(decisionTask, false, None)
    new (decisionTask:DecisionTask, reverseOrder:bool) = Builder(decisionTask, reverseOrder, None)
    new (decisionTask:DecisionTask, contextManager:IContextManager option) = Builder(decisionTask, false, contextManager)

    member this.Delay(f) =
        if DecisionTask.TaskToken = null then 
            // When PollForDecisionTask times out, the TaskToken is null. There's nothing to decide in this case so null is returned.
            (fun () -> null)
        else
            // There are decisions to be made, so call the decider.
            
            (fun () -> 
                ReadContext()

                let result = f()
                WriteContext()
                result
            )

    member this.Run(f) : RespondDecisionTaskCompletedRequest = 
        // Run is used to call the Delay function, making execution immediate which is what we want.
        f()

    member this.Zero() = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)

    member this.Return(result:ReturnResult) =
        blockFlag <- true

        match result with
        | ReturnResult.RespondDecisionTaskCompleted ->
            ()

        | ReturnResult.CompleteWorkflowExecution(r) -> 
            // Look for possible return failures
            let exceptionEvent = walker.FindWorkflowException(EventType.CompleteWorkflowExecutionFailed, exceptionEvents)

            match (exceptionEvent) with
            | None -> 
                let decision = new Decision();
                decision.DecisionType <- DecisionType.CompleteWorkflowExecution
                decision.CompleteWorkflowExecutionDecisionAttributes <- new CompleteWorkflowExecutionDecisionAttributes();
                decision.CompleteWorkflowExecutionDecisionAttributes.Result <- r
                response.Decisions.Add(decision)

            | SomeEventOfType(EventType.CompleteWorkflowExecutionFailed) hev ->
                // A previous attempt was made to complete this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (hev.EventId)
                raise (CompleteWorkflowExecutionFailedException(hev.CompleteWorkflowExecutionFailedEventAttributes))

            | _ -> failwith "error"

        | ReturnResult.CancelWorkflowExecution(details) ->
            let exceptionEvent = walker.FindWorkflowException(EventType.CancelWorkflowExecutionFailed, exceptionEvents)

            match (exceptionEvent) with
            | None ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.CancelWorkflowExecution
                decision.CancelWorkflowExecutionDecisionAttributes <- new CancelWorkflowExecutionDecisionAttributes();
                decision.CancelWorkflowExecutionDecisionAttributes.Details <- details
                response.Decisions.Add(decision)

            | SomeEventOfType(EventType.CancelWorkflowExecutionFailed) hev ->
                // A previous attempt was made to cancel this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (hev.EventId)
                raise (CancelWorkflowExecutionFailedException(hev.CancelWorkflowExecutionFailedEventAttributes))

            | _ -> failwith "error"

        | ReturnResult.FailWorkflowExecution(reason, details) ->
            let exceptionEvent = walker.FindWorkflowException(EventType.FailWorkflowExecutionFailed, exceptionEvents)

            match (exceptionEvent) with
            | None ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.FailWorkflowExecution
                decision.FailWorkflowExecutionDecisionAttributes <- new FailWorkflowExecutionDecisionAttributes();
                decision.FailWorkflowExecutionDecisionAttributes.Reason <- reason
                decision.FailWorkflowExecutionDecisionAttributes.Details <- details
                response.Decisions.Add(decision)

            | SomeEventOfType(EventType.FailWorkflowExecutionFailed) hev ->
                // A previous attempt was made to fail this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (hev.EventId)
                raise (FailWorkflowExecutionFailedException(hev.FailWorkflowExecutionFailedEventAttributes))

            | _ -> failwith "error"

        | ReturnResult.ContinueAsNewWorkflowExecution(attr) ->
            let exceptionEvent = walker.FindWorkflowException(EventType.ContinueAsNewWorkflowExecutionFailed, exceptionEvents)

            match (exceptionEvent) with
            | None ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.ContinueAsNewWorkflowExecution
                decision.ContinueAsNewWorkflowExecutionDecisionAttributes <- attr;
                response.Decisions.Add(decision)

            | SomeEventOfType(EventType.ContinueAsNewWorkflowExecutionFailed) hev ->
                // A previous attempt was made to continue this workflow as new, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (hev.EventId)
                raise (ContinueAsNewWorkflowExecutionFailedException(hev.ContinueAsNewWorkflowExecutionFailedEventAttributes))

            | _ -> failwith "error"

        response

    member this.Return(result:string) = this.Return(ReturnResult.CompleteWorkflowExecution(result))
    member this.Return(result:unit) = this.Return(ReturnResult.RespondDecisionTaskCompleted)
            
    // Schedule Activity Task
    member this.Bind(action:ScheduleActivityTaskAction, f:(ScheduleActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with 
        | ScheduleActivityTaskAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindActivityTask(attr)

            match (combinedHistory) with
            // Scheduling
            | None ->
                let d = new Decision();
                d.DecisionType <- DecisionType.ScheduleActivityTask
                d.ScheduleActivityTaskDecisionAttributes <- attr
                response.Decisions.Add(d)

                f(ScheduleActivityTaskResult.Scheduling(attr))
            
            // Completed
            | SomeEventOfType(EventType.ActivityTaskCompleted) hev ->
                let result = ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Canceled
            | SomeEventOfType(EventType.ActivityTaskCanceled) hev ->
                let result = ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Failed
            | SomeEventOfType(EventType.ActivityTaskFailed) hev ->
                let result = ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)
        
            // TimedOut
            | SomeEventOfType(EventType.ActivityTaskTimedOut) hev ->
                let result = ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // ScheduleFailed
            | SomeEventOfType(EventType.ScheduleActivityTaskFailed) hev ->
                let result = ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Started
            | SomeEventOfType(EventType.ActivityTaskStarted) hev ->
                f(ScheduleActivityTaskResult.Started(hev.ActivityTaskStartedEventAttributes, hev.ActivityTaskScheduledEventAttributes))

            // Scheduled
            | SomeEventOfType(EventType.ActivityTaskScheduled) hev ->
                f(ScheduleActivityTaskResult.Scheduled(hev.ActivityTaskScheduledEventAttributes))

            | _ -> failwith "error"

        | ScheduleActivityTaskAction.ResultFromContext(_, result) ->
            f(result)

    // Wait For Activity Task
    member this.Bind(WaitForActivityTaskAction.ScheduleResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait()

    // Wait For Any Activity Tasks
    member this.Bind(WaitForAnyActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Activity Tasks
    member this.Bind(WaitForAllActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Request Cancel Activity Task 
    member this.Bind(RequestCancelActivityTaskAction.ScheduleResult(schedule), f:(RequestCancelActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        match schedule with 
        // Scheduling
        | ScheduleActivityTaskResult.Scheduling(_) -> 
            Wait()

        // ScheduleFailed
        | ScheduleActivityTaskResult.ScheduleFailed(_) -> f(RequestCancelActivityTaskResult.ActivityScheduleFailed)

        | _ -> 
            if (schedule.IsFinished()) then
                f(RequestCancelActivityTaskResult.ActivityFinished)
            else
                let activityId = 
                    match schedule with
                    | ScheduleActivityTaskResult.Scheduled(sched)  -> sched.ActivityId
                    | ScheduleActivityTaskResult.Started(_, sched) -> sched.ActivityId
                    | _ -> failwith "error"

                let cancelHistory = walker.FindRequestCancelActivityTask(activityId)
                
                match cancelHistory with
                | None -> 
                    let d = new Decision()
                    d.DecisionType <- DecisionType.RequestCancelActivityTask
                    d.RequestCancelActivityTaskDecisionAttributes <- new RequestCancelActivityTaskDecisionAttributes()
                    d.RequestCancelActivityTaskDecisionAttributes.ActivityId <- activityId
                    response.Decisions.Add(d)
                    Wait()

                | SomeEventOfType(EventType.ActivityTaskCancelRequested) hev ->
                    f(RequestCancelActivityTaskResult.CancelRequested(hev.ActivityTaskCancelRequestedEventAttributes))

                | SomeEventOfType(EventType.RequestCancelActivityTaskFailed) hev ->
                    f(RequestCancelActivityTaskResult.RequestCancelFailed(hev.RequestCancelActivityTaskFailedEventAttributes))

                | _ -> failwith "error"

    // Start Child Workflow Execution
    member this.Bind(action:StartChildWorkflowExecutionAction, f:(StartChildWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | StartChildWorkflowExecutionAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindChildWorkflowExecution(attr)
        
            match (combinedHistory) with
            // Not Started
            | None ->
                let d = new Decision();
                d.DecisionType <- DecisionType.StartChildWorkflowExecution
                d.StartChildWorkflowExecutionDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(StartChildWorkflowExecutionResult.Starting(d.StartChildWorkflowExecutionDecisionAttributes))

            // Completed
            | SomeEventOfType(EventType.ChildWorkflowExecutionCompleted) hev ->
                let result = StartChildWorkflowExecutionResult.Completed(hev.ChildWorkflowExecutionCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)
                 
            // Canceled
            | SomeEventOfType(EventType.ChildWorkflowExecutionCanceled) hev ->
                let result = StartChildWorkflowExecutionResult.Canceled(hev.ChildWorkflowExecutionCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // TimedOut
            | SomeEventOfType(EventType.ChildWorkflowExecutionTimedOut) hev ->
                let result = StartChildWorkflowExecutionResult.TimedOut(hev.ChildWorkflowExecutionTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Failed
            | SomeEventOfType(EventType.ChildWorkflowExecutionFailed) hev ->
                let result = StartChildWorkflowExecutionResult.Failed(hev.ChildWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Terminated
            | SomeEventOfType(EventType.ChildWorkflowExecutionTerminated) hev ->
                let result = StartChildWorkflowExecutionResult.Terminated(hev.ChildWorkflowExecutionTerminatedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // StartChildWorkflowExecutionFailed
            | SomeEventOfType(EventType.StartChildWorkflowExecutionFailed) hev ->
                let result = StartChildWorkflowExecutionResult.StartFailed(hev.StartChildWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Started
            | SomeEventOfType(EventType.ChildWorkflowExecutionStarted) hev ->
                f(StartChildWorkflowExecutionResult.Started(hev.ChildWorkflowExecutionStartedEventAttributes))

            // Initiated
            | SomeEventOfType(EventType.StartChildWorkflowExecutionInitiated) hev ->
                f(StartChildWorkflowExecutionResult.Initiated(hev.StartChildWorkflowExecutionInitiatedEventAttributes))

            | _ -> failwith "error"

        | StartChildWorkflowExecutionAction.ResultFromContext(_, result) ->
            f(result)

    // Wait For Child Workflow Execution
    member this.Bind(WaitForChildWorkflowExecutionAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait() 

    // Wait For Any Child Workflow Execution
    member this.Bind(WaitForAnyChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Child Workflow Execution
    member this.Bind(WaitForAllChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Request Cancel External Workflow Execution
    member this.Bind(RequestCancelExternalWorkflowExecutionAction.Attributes(attr), f:(RequestCancelExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = walker.FindRequestCancelExternalWorkflowExecution(attr)
        
        match (combinedHistory) with
        | None ->
            let d = new Decision();
            d.DecisionType <- DecisionType.RequestCancelExternalWorkflowExecution
            d.RequestCancelExternalWorkflowExecutionDecisionAttributes <- attr
            response.Decisions.Add(d)
                
            f(RequestCancelExternalWorkflowExecutionResult.Requesting(attr))
            
        // Request Delivered
        | SomeEventOfType(EventType.ExternalWorkflowExecutionCancelRequested) hev ->
            f(RequestCancelExternalWorkflowExecutionResult.Delivered(hev.ExternalWorkflowExecutionCancelRequestedEventAttributes))

        // Request Failed
        | SomeEventOfType(EventType.RequestCancelExternalWorkflowExecutionFailed) hev ->
            f(RequestCancelExternalWorkflowExecutionResult.Failed(hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes))
 
        // Request Initiated
        | SomeEventOfType(EventType.RequestCancelExternalWorkflowExecutionInitiated) hev ->
            f(RequestCancelExternalWorkflowExecutionResult.Initiated(hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes))

        | _ -> failwith "error"

    // Schedule Lambda Function
    member this.Bind(action:ScheduleLambdaFunctionAction, f:(ScheduleLambdaFunctionResult -> RespondDecisionTaskCompletedRequest)) = 
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | ScheduleLambdaFunctionAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindLambdaFunction(attr)
        
            match (combinedHistory) with
            // Not Scheduled
            | None ->
                let d = new Decision();
                d.DecisionType <- DecisionType.ScheduleLambdaFunction
                d.ScheduleLambdaFunctionDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(ScheduleLambdaFunctionResult.Scheduling(attr))

            // Lambda Function Completed
            | SomeEventOfType(EventType.LambdaFunctionCompleted) hev -> 
                let result = ScheduleLambdaFunctionResult.Completed(hev.LambdaFunctionCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Lambda Function Failed
            | SomeEventOfType(EventType.LambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.Failed(hev.LambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Lambda Function TimedOut
            | SomeEventOfType(EventType.LambdaFunctionTimedOut) hev -> 
                let result = ScheduleLambdaFunctionResult.TimedOut(hev.LambdaFunctionTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // StartLambdaFunctionFailed
            | SomeEventOfType(EventType.StartLambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.StartFailed(hev.StartLambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // ScheduleLambdaFunctionFailed
            | SomeEventOfType(EventType.ScheduleLambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.ScheduleFailed(hev.ScheduleLambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // LambdaFunctionStarted
            | SomeEventOfType(EventType.LambdaFunctionStarted) hev ->
                f(ScheduleLambdaFunctionResult.Started(hev.LambdaFunctionStartedEventAttributes, hev.LambdaFunctionScheduledEventAttributes))

            // LambdaFunctionScheduled
            | SomeEventOfType(EventType.LambdaFunctionScheduled) hev ->
                f(ScheduleLambdaFunctionResult.Scheduled(hev.LambdaFunctionScheduledEventAttributes))

            | _ -> failwith "error"

        | ScheduleLambdaFunctionAction.ResultFromContext(_, result) ->
            f(result)

    // Wait For Lambda Function
    member this.Bind(WaitForLambdaFunctionAction.ScheduleResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait() 

    // Wait For Any Lambda Function
    member this.Bind(WaitForAnyLambdaFunctionAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Lambda Function
    member this.Bind(WaitForAllLambdaFunctionAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Start Timer
    member this.Bind(action:StartTimerAction, f:(StartTimerResult -> RespondDecisionTaskCompletedRequest)) = 
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with 
        | StartTimerAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindTimer(attr)

            match (combinedHistory) with
            // Timer Not Started
            | None ->
                let d = new Decision();
                d.DecisionType <- DecisionType.StartTimer
                d.StartTimerDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(StartTimerResult.Starting(d.StartTimerDecisionAttributes))

            // Fired
            | SomeEventOfType(EventType.TimerFired) hev ->
                let result = StartTimerResult.Fired(hev.TimerFiredEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Canceled
            | SomeEventOfType(EventType.TimerCanceled) hev ->
                let result = StartTimerResult.Canceled(hev.TimerCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // StartTimerFailed
            | SomeEventOfType(EventType.StartTimerFailed) hev ->
                let result = StartTimerResult.StartTimerFailed(hev.StartTimerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // TimerStarted
            | SomeEventOfType(EventType.TimerStarted) hev ->
                f(StartTimerResult.Started(hev.TimerStartedEventAttributes))

            | _ -> failwith "error"

        | StartTimerAction.ResultFromContext(_, result) -> 
            f(result)

    // Wait For Timer
    member this.Bind(WaitForTimerAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match result with 
        // Canceled | Fired | StartTimerFailed
        | StartTimerResult.Canceled(_)
        | StartTimerResult.Fired(_)
        | StartTimerResult.StartTimerFailed(_) -> f()

        // Started | Starting
        | StartTimerResult.Started(_)
        | StartTimerResult.Starting(_) -> 
            Wait()

    // Cancel Timer
    member this.Bind(CancelTimerAction.StartResult(result), f:(CancelTimerResult -> RespondDecisionTaskCompletedRequest)) =
        match result with 
        // Starting
        | StartTimerResult.Starting(_) -> 
            Wait()

        // Canceled
        | StartTimerResult.Canceled(attr) -> f(CancelTimerResult.Canceled(attr))

        // Fired
        | StartTimerResult.Fired(attr) -> f(CancelTimerResult.Fired(attr))

        // StartTimerFailed
        | StartTimerResult.StartTimerFailed(attr) -> f(CancelTimerResult.StartTimerFailed(attr))

        // Started
        | StartTimerResult.Started(attr) -> 
            let combinedHistory = walker.FindCancelTimer(attr.TimerId)

            match (combinedHistory) with
            | None ->
                // This timer has not been canceled yet, make cancel decision
                let d = new Decision();
                d.DecisionType <- DecisionType.CancelTimer
                d.CancelTimerDecisionAttributes <- new CancelTimerDecisionAttributes(TimerId=attr.TimerId)
                response.Decisions.Add(d)

                f(CancelTimerResult.Canceling)

            // CancelTimerFailed
            | SomeEventOfType(EventType.CancelTimerFailed) hev ->
                f(CancelTimerResult.CancelTimerFailed(hev.CancelTimerFailedEventAttributes))

            | _ -> failwith "error"

    // Marker Recorded
    member this.Bind(action:MarkerRecordedAction, f:(MarkerRecordedResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | MarkerRecordedAction.Attributes(markerName, pushToContext) ->
            let combinedHistory = walker.FindMarker(markerName)

            match combinedHistory with
            // NotRecorded
            | None ->
                f(MarkerRecordedResult.NotRecorded)

            // RecordMarkerFailed
            | SomeEventOfType(EventType.RecordMarkerFailed) hev ->
                let result = MarkerRecordedResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(markerName, result)
                f(result)

            // MarkerRecorded
            | SomeEventOfType(EventType.MarkerRecorded) hev ->
                let result = MarkerRecordedResult.Recorded(hev.MarkerRecordedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(markerName, result)
                f(result)

            | _ -> failwith "error"

        | MarkerRecordedAction.ResultFromContext(_, result) ->
            f(result)

    // Record Marker
    member this.Bind(action:RecordMarkerAction, f:(RecordMarkerResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | RecordMarkerAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindMarker(attr.MarkerName)

            match combinedHistory with
            // The marker was never recorded, record it now
            | None ->
                let d = new Decision();
                d.DecisionType <- DecisionType.RecordMarker
                d.RecordMarkerDecisionAttributes <- attr
                response.Decisions.Add(d)

                f(RecordMarkerResult.Recording)
            
            // RecordMarkerFailed
            | SomeEventOfType(EventType.RecordMarkerFailed) hev ->
                let result = RecordMarkerResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // MarkerRecorded
            | SomeEventOfType(EventType.MarkerRecorded) hev ->
                let result = RecordMarkerResult.Recorded(hev.MarkerRecordedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            | _ -> failwith "error"
        | RecordMarkerAction.ResultFromContext(_, result) ->
            f(result)

    // Signal External Workflow Execution
    member this.Bind(action:SignalExternalWorkflowExecutionAction, f:(SignalExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | SignalExternalWorkflowExecutionAction.Attributes(attr, pushToContext) ->
            let combinedHistory = walker.FindSignalExternalWorkflow(attr)

            match combinedHistory with
            // Signaling
            | None -> 
                // The signal was never sent, send it now                
                let d = new Decision();
                d.DecisionType <- DecisionType.SignalExternalWorkflowExecution
                d.SignalExternalWorkflowExecutionDecisionAttributes <- attr
                response.Decisions.Add(d)
                f(SignalExternalWorkflowExecutionResult.Signaling)

            // Signaled
            | SomeEventOfType(EventType.ExternalWorkflowExecutionSignaled) hev ->
                let result = SignalExternalWorkflowExecutionResult.Signaled(hev.ExternalWorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)

            // Failed
            | SomeEventOfType(EventType.SignalExternalWorkflowExecutionFailed) hev ->
                let result = SignalExternalWorkflowExecutionResult.Failed(hev.SignalExternalWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                f(result)
        
            // Initiated
            | SomeEventOfType(EventType.SignalExternalWorkflowExecutionInitiated) hev ->
                f(SignalExternalWorkflowExecutionResult.Initiated(hev.SignalExternalWorkflowExecutionInitiatedEventAttributes))

            | _ -> failwith "error"
        | SignalExternalWorkflowExecutionAction.ResultFromContext(_, result) ->
            f(result)
            
    // Workflow Execution Signaled
    member this.Bind(action:WorkflowExecutionSignaledAction, f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | WorkflowExecutionSignaledAction.Attributes(signalName, pushToContext) ->
            let combinedHistory = walker.FindSignaled(signalName)

            match combinedHistory with
            // Not Signaled
            | None ->
                f(WorkflowExecutionSignaledResult.NotSignaled)

            // Signaled
            | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
                let result = WorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(signalName, result)
                f(result)
        
            | _ -> failwith "error"

        | WorkflowExecutionSignaledAction.ResultFromContext(_, result) ->
            f(result)

    // Wait For Workflow Execution Signaled
    member this.Bind(action:WaitForWorkflowExecutionSignaledAction, f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | WaitForWorkflowExecutionSignaledAction.Attributes(signalName, pushToContext) ->
            let combinedHistory = walker.FindSignaled(signalName)

            match combinedHistory with
            // Not Signaled, keep waiting
            | None ->
                Wait()

            // Signaled
            | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
                let result = WorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(signalName, result)
                f(result)
        
            | _ -> failwith "error"

        | WaitForWorkflowExecutionSignaledAction.ResultFromContext(_, result) ->
            f(result)

    // Workflow Execution Cancel Requested
    member this.Bind(WorkflowExecutionCancelRequestedAction.Attributes(), f:(WorkflowExecutionCancelRequestedResult -> RespondDecisionTaskCompletedRequest)) =
        let cancelRequestedEvent = walker.FindWorkflowExecutionCancelRequested()

        match cancelRequestedEvent with
        // NotRequested
        | None ->
            f(WorkflowExecutionCancelRequestedResult.NotRequested)            

        // Workflow Cancel Requsted
        | SomeEventOfType(EventType.WorkflowExecutionCancelRequested) hev ->
            f(WorkflowExecutionCancelRequestedResult.CancelRequested(hev.WorkflowExecutionCancelRequestedEventAttributes))
            
        | _ -> failwith "error"

    // Get Workflow Execution Input
    member this.Bind(GetWorkflowExecutionInputAction.Attributes(), f:(string -> RespondDecisionTaskCompletedRequest)) =

        let startedEvent = walker.FindWorkflowExecutionStarted()

        match (startedEvent) with
        | None ->
            f(null)

        | SomeEventOfType(EventType.WorkflowExecutionStarted) hev ->
            f(hev.WorkflowExecutionStartedEventAttributes.Input)

        | _ -> failwith "error"

    // Get Execution Context
    member this.Bind(GetExecutionContextAction.Attributes(), f:(string -> RespondDecisionTaskCompletedRequest)) =

        let completedEvent = walker.FindLatestDecisionTaskCompleted()

        match (completedEvent) with
        | None ->
            f(null)

        | SomeEventOfType(EventType.DecisionTaskCompleted) hev ->
            f(hev.DecisionTaskCompletedEventAttributes.ExecutionContext)

        | _ -> failwith "error"

    // Set Execution Context
    member this.Bind(SetExecutionContextAction.Attributes(context), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        response.ExecutionContext <- context
        f()

    // Remove From Context
    member this.Bind(action:RemoveFromContextAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        if ContextManager.IsSome then ContextManager.Value.Remove(action)
        f()

    // For Loop
    member this.For(enumeration:seq<'T>, f:(_ -> RespondDecisionTaskCompletedRequest)) =

        let processForBlock x = 
            if not blockFlag then f(x) |> ignore
            (not blockFlag)

        enumeration |>
        Seq.takeWhile processForBlock |>
        Seq.iter (fun x -> ())

    // While Loop
    member this.While(condition:(unit -> bool), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        while (not blockFlag) && condition() do
            f() |> ignore

    // Combine
    member this.Combine(exprBefore, fAfter) =
        // We assume the exprBefore decisions have been added to the response already
        // Just need to run the expression after this, which will add their decisions while executing
        if blockFlag then response else fAfter()

    // Try Finally
    member this.TryFinally(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprFinally:(unit -> unit)) =
        try 
            exprInside()
        finally
            if not blockFlag then exprFinally()

    // Try With
    member this.TryWith(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprWith:(Exception -> RespondDecisionTaskCompletedRequest)) =
        try
            exprInside()
        with
        | e ->
            exprWith e

