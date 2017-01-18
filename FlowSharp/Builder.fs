namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.HistoryWalker

type Builder (DecisionTask:DecisionTask, ReverseOrder:bool) =
    let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
    let walker = HistoryWalker(DecisionTask.Events, ReverseOrder)
    let mutable blockFlag = false
    let mutable exceptionEvents = List.empty<int64>

    let AddExceptionEventId (eventId:int64) =
        exceptionEvents <- eventId :: exceptionEvents

    let Wait() =
        blockFlag <- true
        response

    member this.Delay(f) =
        if DecisionTask.TaskToken = null then 
            // When PollForDecisionTask times out, the TaskToken is null. There's nothing to decide in this case so null is returned.
            (fun () -> null)
        else
            // There are decisions to be made, so call the decider.
            (fun () -> f())

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
    member this.Bind(ScheduleActivityTaskAction.Attributes(attr), f:(ScheduleActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
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
            f(ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes))

        // Canceled
        | SomeEventOfType(EventType.ActivityTaskCanceled) hev ->
            f(ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes))

        // Failed
        | SomeEventOfType(EventType.ActivityTaskFailed) hev ->
            f(ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes))
        
        // TimedOut
        | SomeEventOfType(EventType.ActivityTaskTimedOut) hev ->
            f(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes))

        // ScheduleFailed
        | SomeEventOfType(EventType.ScheduleActivityTaskFailed) hev ->
            f(ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes))

        // Started
        | SomeEventOfType(EventType.ActivityTaskStarted) hev ->
            f(ScheduleActivityTaskResult.Started(hev.ActivityTaskStartedEventAttributes, hev.ActivityTaskScheduledEventAttributes))

        // Scheduled
        | SomeEventOfType(EventType.ActivityTaskScheduled) hev ->
            f(ScheduleActivityTaskResult.Scheduled(hev.ActivityTaskScheduledEventAttributes))

        | _ -> failwith "error"

    // Schedule and Wait for Activity Task
    member this.Bind(ScheduleAndWaitForActivityTaskAction.Attributes(attr), f:(ScheduleActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        let combinedHistory = walker.FindActivityTask(attr)

        match (combinedHistory) with
        // Not scheduled yet
        | None ->
            let d = new Decision();
            d.DecisionType <- DecisionType.ScheduleActivityTask
            d.ScheduleActivityTaskDecisionAttributes <- attr
            response.Decisions.Add(d)
            Wait()
            
        // Completed
        | SomeEventOfType(EventType.ActivityTaskCompleted) hev ->
            f(ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes))

        // Canceled
        | SomeEventOfType(EventType.ActivityTaskCanceled) hev ->
            f(ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes))

        // Failed
        | SomeEventOfType(EventType.ActivityTaskFailed) hev ->
            f(ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes))
        
        // TimedOut
        | SomeEventOfType(EventType.ActivityTaskTimedOut) hev ->
            f(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes))

        // ScheduleFailed
        | SomeEventOfType(EventType.ScheduleActivityTaskFailed) hev ->
            f(ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes))

        // Started
        // Scheduled
        | SomeEventOfType(EventType.ActivityTaskScheduled) hev ->
            Wait()

        | _ -> failwith "error"

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
    member this.Bind(StartChildWorkflowExecutionAction.Attributes(attr), f:(StartChildWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =

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
            f(StartChildWorkflowExecutionResult.Completed(hev.ChildWorkflowExecutionCompletedEventAttributes))
                 
        // Canceled
        | SomeEventOfType(EventType.ChildWorkflowExecutionCanceled) hev ->
            f(StartChildWorkflowExecutionResult.Canceled(hev.ChildWorkflowExecutionCanceledEventAttributes))

        // TimedOut
        | SomeEventOfType(EventType.ChildWorkflowExecutionTimedOut) hev ->
            f(StartChildWorkflowExecutionResult.TimedOut(hev.ChildWorkflowExecutionTimedOutEventAttributes))

        // Failed
        | SomeEventOfType(EventType.ChildWorkflowExecutionFailed) hev ->
            f(StartChildWorkflowExecutionResult.Failed(hev.ChildWorkflowExecutionFailedEventAttributes))

        // Terminated
        | SomeEventOfType(EventType.ChildWorkflowExecutionTerminated) hev ->
            f(StartChildWorkflowExecutionResult.Terminated(hev.ChildWorkflowExecutionTerminatedEventAttributes))

        // Started
        | SomeEventOfType(EventType.ChildWorkflowExecutionStarted) hev ->
            f(StartChildWorkflowExecutionResult.Started(hev.ChildWorkflowExecutionStartedEventAttributes))

        // StartChildWorkflowExecutionFailed
        | SomeEventOfType(EventType.StartChildWorkflowExecutionFailed) hev ->
            f(StartChildWorkflowExecutionResult.StartFailed(hev.StartChildWorkflowExecutionFailedEventAttributes))

        // Initiated
        | SomeEventOfType(EventType.StartChildWorkflowExecutionInitiated) hev ->
            f(StartChildWorkflowExecutionResult.Initiated(hev.StartChildWorkflowExecutionInitiatedEventAttributes))

        | _ -> failwith "error"

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

    // Start and Wait for Lambda Function
    member this.Bind(ScheduleAndWaitForLambdaFunctionAction.Attributes(attr), f:(ScheduleAndWaitForLambdaFunctionResult -> RespondDecisionTaskCompletedRequest)) = 

        let combinedHistory = walker.FindLambdaFunction(attr)
        
        match (combinedHistory) with
        // Not Scheduled
        | None ->
            let d = new Decision();
            d.DecisionType <- DecisionType.ScheduleLambdaFunction
            d.ScheduleLambdaFunctionDecisionAttributes <- attr
            response.Decisions.Add(d)
            Wait()

        // Lambda Function Completed
        | SomeEventOfType(EventType.LambdaFunctionCompleted) hev -> 
            f(ScheduleAndWaitForLambdaFunctionResult.Completed(hev.LambdaFunctionCompletedEventAttributes))

        // Lambda Function Failed
        | SomeEventOfType(EventType.LambdaFunctionFailed) hev -> 
            f(ScheduleAndWaitForLambdaFunctionResult.Failed(hev.LambdaFunctionFailedEventAttributes))

        // Lambda Function TimedOut
        | SomeEventOfType(EventType.LambdaFunctionTimedOut) hev -> 
            f(ScheduleAndWaitForLambdaFunctionResult.TimedOut(hev.LambdaFunctionTimedOutEventAttributes))

        // StartLambdaFunctionFailed
        | SomeEventOfType(EventType.StartLambdaFunctionFailed) hev -> 
            f(ScheduleAndWaitForLambdaFunctionResult.StartFailed(hev.StartLambdaFunctionFailedEventAttributes))

        // ScheduleLambdaFunctionFailed
        | SomeEventOfType(EventType.ScheduleLambdaFunctionFailed) hev -> 
            f(ScheduleAndWaitForLambdaFunctionResult.ScheduleFailed(hev.ScheduleLambdaFunctionFailedEventAttributes))

        | _ -> 
            // This lambda function is still running, continue blocking
            Wait()

    // Start Timer
    member this.Bind(StartTimerAction.Attributes(attr), f:(StartTimerResult -> RespondDecisionTaskCompletedRequest)) = 

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
            f(StartTimerResult.Fired(hev.TimerFiredEventAttributes))

        // Canceled
        | SomeEventOfType(EventType.TimerCanceled) hev ->
            f(StartTimerResult.Canceled(hev.TimerCanceledEventAttributes))

        // StartTimerFailed
        | SomeEventOfType(EventType.StartTimerFailed) hev ->
            f(StartTimerResult.StartTimerFailed(hev.StartTimerFailedEventAttributes))

        // TimerStarted
        | SomeEventOfType(EventType.TimerStarted) hev ->
            f(StartTimerResult.Started(hev.TimerStartedEventAttributes))

        | _ -> failwith "error"

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
    member this.Bind(MarkerRecordedAction.Attributes(markerName:string), f:(MarkerRecordedResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = walker.FindMarker(markerName)

        match combinedHistory with
        // NotRecorded
        | None ->
            f(MarkerRecordedResult.NotRecorded)

        // RecordMarkerFailed
        | SomeEventOfType(EventType.RecordMarkerFailed) hev ->
            f(MarkerRecordedResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes))

        // MarkerRecorded
        | SomeEventOfType(EventType.MarkerRecorded) hev ->
            f(MarkerRecordedResult.MarkerRecorded(hev.MarkerRecordedEventAttributes))

        | _ -> failwith "error"

    // Record Marker
    member this.Bind(RecordMarkerAction.Attributes(attr), f:(RecordMarkerResult -> RespondDecisionTaskCompletedRequest)) =
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
            f(RecordMarkerResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes))

        // MarkerRecorded
        | SomeEventOfType(EventType.MarkerRecorded) hev ->
            f(RecordMarkerResult.MarkerRecorded(hev.MarkerRecordedEventAttributes))

        | _ -> failwith "error"

    // Signal External Workflow Execution
    member this.Bind(SignalExternalWorkflowExecutionAction.Attributes(attr), f:(SignalExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =

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
            f(SignalExternalWorkflowExecutionResult.Signaled(hev.ExternalWorkflowExecutionSignaledEventAttributes))

        // Failed
        | SomeEventOfType(EventType.SignalExternalWorkflowExecutionFailed) hev ->
            f(SignalExternalWorkflowExecutionResult.Failed(hev.SignalExternalWorkflowExecutionFailedEventAttributes))
        
        // Initiated
        | SomeEventOfType(EventType.SignalExternalWorkflowExecutionInitiated) hev ->
            f(SignalExternalWorkflowExecutionResult.Initiated(hev.SignalExternalWorkflowExecutionInitiatedEventAttributes))

        | _ -> failwith "error"

    // Workflow Execution Signaled
    member this.Bind(WorkflowExecutionSignaledAction.Attributes(signalName), f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = walker.FindSignaled(signalName)

        match combinedHistory with
        // Not Signaled
        | None ->
            f(WorkflowExecutionSignaledResult.NotSignaled)

        // Signaled
        | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
            f(WorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes))
        
        | _ -> failwith "error"

    member this.Bind(contextAction:WorkflowExecutionSignaledFromContextAction, f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        match contextAction with
        | WorkflowExecutionSignaledFromContextAction.NotInContext(action) -> 
            this.Bind(action, f)
        | WorkflowExecutionSignaledFromContextAction.ResultFromContext(result) ->
            f(result)
        | WorkflowExecutionSignaledFromContextAction.ExpectedButNotFound(ex) -> 
            raise ex

    // Wait For Workflow Execution Signaled
    member this.Bind(WaitForWorkflowExecutionSignaledAction.Attributes(signalName), f:(WaitForWorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = walker.FindSignaled(signalName)

        match combinedHistory with
        // Not Signaled, keep waiting
        | None ->
            Wait()

        // Signaled
        | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
            f(WaitForWorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes))
        
        | _ -> failwith "error"

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

        let completedEvent = walker.FindDecisionTaskCompleted()

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

    member this.For(enumeration:seq<'T>, f:(_ -> RespondDecisionTaskCompletedRequest)) =

        let processForBlock x = 
            if not blockFlag then f(x) |> ignore
            (not blockFlag)

        enumeration |>
        Seq.takeWhile processForBlock |>
        Seq.iter (fun x -> ())

    member this.While(condition:(unit -> bool), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        while (not blockFlag) && condition() do
            f() |> ignore

    member this.Combine(exprBefore, fAfter) =
        // We assume the exprBefore decisions have been added to the response already
        // Just need to run the expression after this, which will add their decisions while executing
        if blockFlag then response else fAfter()

    member this.TryFinally(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprFinally:(unit -> unit)) =
        try 
            exprInside()
        finally
            if not blockFlag then exprFinally()

    member this.TryWith(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprWith:(Exception -> RespondDecisionTaskCompletedRequest)) =
        try
            exprInside()
        with
        | e ->
            exprWith e

