namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.HistoryWalker

type Builder (DecisionTask:DecisionTask) =
    let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
    let walker = HistoryWalker(ResizeArray<HistoryEvent>(DecisionTask.Events))
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



(*

    let (|EventOfType|_|) (etype:EventType) (hev:HistoryEvent) =
        if hev.EventType = etype then
            Some(hev)
        else
            None


    let FindSignalHistory (decisionTask:DecisionTask) (signalName:string) =
        let combinedHistory = new HistoryEvent()

        let findSignal =
            decisionTask.Events
            |> Seq.tryFindBack (fun hev -> hev.EventType = EventType.WorkflowExecutionSignaled && hev.WorkflowExecutionSignaledEventAttributes.SignalName = signalName)

        match findSignal with
        | Some(h) -> 
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp
            combinedHistory.WorkflowExecutionSignaledEventAttributes <- h.WorkflowExecutionSignaledEventAttributes
        | None -> ()
        
        // Return the combined history
        combinedHistory

    let FindMarkerHistory (decisionTask:DecisionTask) (markerName:string) =
        let combinedHistory = new HistoryEvent()

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.MarkerRecorded && hev.MarkerRecordedEventAttributes.MarkerName = markerName then
                setCommonProperties(hev)
                combinedHistory.MarkerRecordedEventAttributes <- hev.MarkerRecordedEventAttributes

            elif hev.EventType = EventType.RecordMarkerFailed && hev.RecordMarkerFailedEventAttributes.MarkerName = markerName then
                setCommonProperties(hev)
                combinedHistory.RecordMarkerFailedEventAttributes <- hev.RecordMarkerFailedEventAttributes
        
        // Return the combined history
        combinedHistory

    let FindChildWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (workflowType:WorkflowType) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()

            // StartChildWorkflowExecutionInitiated
            elif hev.EventType = EventType.StartChildWorkflowExecutionInitiated &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionInitiatedEventAttributes <- hev.StartChildWorkflowExecutionInitiatedEventAttributes

            // StartChildWorkflowExecutionFailed
            elif hev.EventType = EventType.StartChildWorkflowExecutionFailed &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionFailedEventAttributes <- hev.StartChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionStarted
            elif hev.EventType = EventType.ChildWorkflowExecutionStarted &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionStartedEventAttributes <- hev.ChildWorkflowExecutionStartedEventAttributes

            // ChildWorkflowExecutionCompleted
            elif hev.EventType = EventType.ChildWorkflowExecutionCompleted &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCompletedEventAttributes <- hev.ChildWorkflowExecutionCompletedEventAttributes

            // ChildWorkflowExecutionFailed
            elif hev.EventType = EventType.ChildWorkflowExecutionFailed &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionFailedEventAttributes <- hev.ChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionTimedOut
            elif hev.EventType = EventType.ChildWorkflowExecutionTimedOut &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionTimedOutEventAttributes <- hev.ChildWorkflowExecutionTimedOutEventAttributes

            // ChildWorkflowExecutionCanceled
            elif hev.EventType = EventType.ChildWorkflowExecutionCanceled &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCanceledEventAttributes <- hev.ChildWorkflowExecutionCanceledEventAttributes

            // ChildWorkflowExecutionTerminated
            elif hev.EventType = EventType.ChildWorkflowExecutionTerminated &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionTerminatedEventAttributes <- hev.ChildWorkflowExecutionTerminatedEventAttributes

        // Return the combined history
        combinedHistory

    let FindRequestCancelExternalWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable initiatedEventId = 0L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.RequestCancelExternalWorkflowExecutionInitiated && 
                               hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
                               hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes 
                initiatedEventId <- hev.EventId

            elif hev.EventType = EventType.RequestCancelExternalWorkflowExecutionFailed &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.RequestCancelExternalWorkflowExecutionFailedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes 

            elif hev.EventType = EventType.ExternalWorkflowExecutionCancelRequested && 
                               hev.ExternalWorkflowExecutionCancelRequestedEventAttributes.InitiatedEventId = initiatedEventId &&
                               hev.ExternalWorkflowExecutionCancelRequestedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ExternalWorkflowExecutionCancelRequestedEventAttributes <- hev.ExternalWorkflowExecutionCancelRequestedEventAttributes

        // Return the combined history
        combinedHistory

    let FindSignalExternalWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (signalName:string) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable initiatedEventId = 0L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.SignalExternalWorkflowExecutionInitiated && 
               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.SignalName = signalName && 
               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                
                setCommonProperties(hev)
                combinedHistory.SignalExternalWorkflowExecutionInitiatedEventAttributes <- hev.SignalExternalWorkflowExecutionInitiatedEventAttributes
                initiatedEventId <- hev.EventId

            elif hev.EventType = EventType.SignalExternalWorkflowExecutionFailed &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then

                setCommonProperties(hev)
                combinedHistory.SignalExternalWorkflowExecutionFailedEventAttributes <- hev.SignalExternalWorkflowExecutionFailedEventAttributes

            elif hev.EventType = EventType.ExternalWorkflowExecutionSignaled &&
                 hev.ExternalWorkflowExecutionSignaledEventAttributes.InitiatedEventId = initiatedEventId && 
                 hev.ExternalWorkflowExecutionSignaledEventAttributes.WorkflowExecution.WorkflowId = workflowId then

                setCommonProperties(hev)
                combinedHistory.ExternalWorkflowExecutionSignaledEventAttributes <- hev.ExternalWorkflowExecutionSignaledEventAttributes
        
        // Return the combined history
        combinedHistory
        
    let FindReturnExceptionHistory (decisionTask:DecisionTask) =
        let combinedHistory = new HistoryEvent()

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if HasExceptionBeenThrownForEvent (hev.EventId) then
                // If an exception has been thrown once for this event, don't let it be flagged twice
                ()
            else
                if hev.EventType = EventType.CompleteWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.CompleteWorkflowExecutionFailedEventAttributes <- hev.CompleteWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.CancelWorkflowExecutionFailed  then
                    setCommonProperties(hev)
                    combinedHistory.CancelWorkflowExecutionFailedEventAttributes <- hev.CancelWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.FailWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.FailWorkflowExecutionFailedEventAttributes <- hev.FailWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.ContinueAsNewWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.ContinueAsNewWorkflowExecutionFailedEventAttributes <- hev.ContinueAsNewWorkflowExecutionFailedEventAttributes
                        
        // Return the combined history
        combinedHistory

    let FindScheduleActivityTaskResult (decision:ScheduleActivityTaskDecisionAttributes) : ScheduleActivityTaskResult =  

        let rec TryFindFinished (index:int) (activityTaskStarted:HistoryEvent) (activityTaskScheduled:HistoryEvent) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option = 
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                // Completed
                | EventOfType EventType.ActivityTaskCompleted _ when 
                        hev.ActivityTaskCompletedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCompletedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // Canceled
                | EventOfType EventType.ActivityTaskCanceled _ when 
                        hev.ActivityTaskCanceledEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCanceledEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // Failed
                | EventOfType EventType.ActivityTaskFailed _ when 
                        hev.ActivityTaskFailedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskFailedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut _ when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes, 
                                                                  Some(activityTaskStarted.ActivityTaskStartedEventAttributes), 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindFinished (index+1) activityTaskStarted activityTaskScheduled hev

                | _ ->
                    TryFindFinished (index+1) activityTaskStarted activityTaskScheduled decisionTaskCompleted


        let rec TryFindStartedOrTimedOut (index:int) (activityTaskScheduled:HistoryEvent) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with

                // Started
                | EventOfType EventType.ActivityTaskStarted _ when 
                        hev.ActivityTaskStartedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId ->
                
                    let tryFinished = TryFindFinished (index+1) hev activityTaskScheduled decisionTaskCompleted
                    match tryFinished with
                    | Some(_)  -> tryFinished
                    | None     -> Some(ScheduleActivityTaskResult.Started(hev.ActivityTaskStartedEventAttributes, 
                                                                          activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                          decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut _ when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = 0L &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes, 
                                                                  None, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindStartedOrTimedOut (index+1) activityTaskScheduled hev

                | _ ->
                    TryFindStartedOrTimedOut (index+1) activityTaskScheduled decisionTaskCompleted


        let rec TryFindScheduledOrScheduleFailed (index:int) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                | EventOfType EventType.ActivityTaskScheduled _ when
                                hev.ActivityTaskScheduledEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityId = decision.ActivityId &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityType.Name = decision.ActivityType.Name &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityType.Version = decision.ActivityType.Version ->

                    let tryStarted = TryFindStartedOrTimedOut (index+1) hev decisionTaskCompleted
                    match tryStarted with
                    | Some(_)   -> tryStarted
                    | None      -> Some(ScheduleActivityTaskResult.Scheduled(hev.ActivityTaskScheduledEventAttributes, 
                                                                             decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                
                | EventOfType EventType.ScheduleActivityTaskFailed _ when
                                hev.ScheduleActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityId = decision.ActivityId &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = decision.ActivityType.Name &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = decision.ActivityType.Version ->

                    Some(ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes, 
                                                                   decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindScheduledOrScheduleFailed (index+1) hev

                | _ ->
                    TryFindScheduledOrScheduleFailed (index+1) decisionTaskCompleted

                
        let rec TryFindDecisionTaskCompleted (index:int) : ScheduleActivityTaskResult option =
            if index >= events.Count then
                None
            else
                let hev = events.[index]
                
                if hev.EventType = EventType.DecisionTaskCompleted then
                    let tryScheduled = TryFindScheduledOrScheduleFailed (index+1) hev
                    match tryScheduled with
                    | Some(_)   -> tryScheduled
                    | None      -> TryFindDecisionTaskCompleted (index+1)
                else
                    TryFindDecisionTaskCompleted (index+1)

        let tryDecisionTaskCompleted = TryFindDecisionTaskCompleted 1
        match tryDecisionTaskCompleted with
        | Some(result)  -> result
        | None          -> ScheduleActivityTaskResult.Scheduling(decision)


    let FindRequestCancelActivityTaskResult (eventId:int) (activityId:string) : (RequestCancelActivityTaskResult option) = 

        let rec TryFindRequestedOrFailed (index:int) (decisionTaskCompleted:HistoryEvent) : RequestCancelActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                | EventOfType EventType.ActivityTaskCancelRequested _ when
                            hev.ActivityTaskCancelRequestedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                            hev.ActivityTaskCancelRequestedEventAttributes.ActivityId = activityId ->

                    Some(RequestCancelActivityTaskResult.CancelRequested(hev.ActivityTaskCancelRequestedEventAttributes, 
                                                                         decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.RequestCancelActivityTaskFailed _ when
                            hev.RequestCancelActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                            hev.RequestCancelActivityTaskFailedEventAttributes.ActivityId = activityId ->

                    Some(RequestCancelActivityTaskResult.RequestCancelFailed(hev.RequestCancelActivityTaskFailedEventAttributes, 
                                                                             decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindRequestedOrFailed (index+1) hev

                | _ ->
                    TryFindRequestedOrFailed (index+1) decisionTaskCompleted

        let rec TryFindDecisionTaskCompleted (index:int) : RequestCancelActivityTaskResult option =
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                if hev.EventType = EventType.DecisionTaskCompleted then
                    TryFindRequestedOrFailed (index+1) hev                    
                else 
                    TryFindDecisionTaskCompleted (index+1) 

        TryFindDecisionTaskCompleted eventId
                    
    
    let FindLambdaFunctionResult (id:string) (name:string) : ScheduleAndWaitForLambdaFunctionResult option =
        None

    let FindTimerHistory (decisionTask:DecisionTask) (control:string) =
        let combinedHistory = new HistoryEvent()
        let mutable startedEventId = -1L
        let mutable timerId : string = null
            
        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()
            
            // CancelTimerFailed
            elif hev.EventType = EventType.CancelTimerFailed && hev.CancelTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.CancelTimerFailedEventAttributes <- hev.CancelTimerFailedEventAttributes

            // StartTimerFailed
            elif hev.EventType = EventType.StartTimerFailed && hev.StartTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.StartTimerFailedEventAttributes <- hev.StartTimerFailedEventAttributes

            // TimerStarted
            elif hev.EventType = EventType.TimerStarted && hev.TimerStartedEventAttributes.Control = control then
                setCommonProperties(hev)
                combinedHistory.TimerStartedEventAttributes <- hev.TimerStartedEventAttributes
                startedEventId <- hev.EventId
                timerId <- hev.TimerStartedEventAttributes.TimerId

            // TimerFired
            elif hev.EventType = EventType.TimerFired && hev.TimerFiredEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.TimerFiredEventAttributes <- hev.TimerFiredEventAttributes

            // TimerCanceled
            elif hev.EventType = EventType.TimerCanceled && hev.TimerCanceledEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.TimerCanceledEventAttributes <- hev.TimerCanceledEventAttributes

        // Return the combined history
        combinedHistory
*)