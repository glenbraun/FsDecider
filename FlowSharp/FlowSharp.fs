namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.HistoryWalker
open FlowSharp.EventPatterns
open FlowSharp.ExecutionContext

exception FlowSharpBuilderException of HistoryEvent option * string

type FlowSharp (DecisionTask:DecisionTask, ReverseOrder:bool, ContextManager:IContextManager option) =
    let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
    let walker = HistoryWalker(DecisionTask.Events, ReverseOrder)
    let mutable blockFlag = false
    let mutable exceptionEvents = List.empty<int64>

    do Trace.BuilderCreated DecisionTask ReverseOrder ContextManager

    let AddExceptionEventId (eventId:int64) =
        exceptionEvents <- eventId :: exceptionEvents

    let Wait() =
        Trace.BuilderWait DecisionTask response
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

    new (decisionTask:DecisionTask) = FlowSharp(decisionTask, false, None)
    new (decisionTask:DecisionTask, reverseOrder:bool) = FlowSharp(decisionTask, reverseOrder, None)
    new (decisionTask:DecisionTask, contextManager:IContextManager option) = FlowSharp(decisionTask, false, contextManager)

    member this.Delay(f) =
        Trace.BuilderDelay(DecisionTask)

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
        Trace.BuilderRun(DecisionTask)
        f()

    member this.Zero() = 
        Trace.BuilderZero(DecisionTask)
        new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)

    member this.Return(result:ReturnResult) =
        blockFlag <- true

        match result with
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

            | _ -> raise (FlowSharpBuilderException(exceptionEvent, "Unexpected state of CompleteWorkflowExecution during return operation."))

            Trace.BuilderReturn DecisionTask result response exceptionEvent (EventType.CompleteWorkflowExecutionFailed)

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

            | _ -> raise (FlowSharpBuilderException(exceptionEvent, "Unexpected state of CancelWorkflowExecution during return operation."))

            Trace.BuilderReturn DecisionTask result response exceptionEvent (EventType.CancelWorkflowExecutionFailed)

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

            | _ -> raise (FlowSharpBuilderException(exceptionEvent, "Unexpected state of FailWorkflowExecution during return operation."))

            Trace.BuilderReturn DecisionTask result response exceptionEvent (EventType.FailWorkflowExecutionFailed)
            
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

            | _ -> raise (FlowSharpBuilderException(exceptionEvent, "Unexpected state of ContinueAsNewWorkflowExecution during return operation."))

            Trace.BuilderReturn DecisionTask result response exceptionEvent (EventType.ContinueAsNewWorkflowExecutionFailed)

        response

    member this.Return(result:string) = this.Return(ReturnResult.CompleteWorkflowExecution(result))
    member this.Return(result:unit) = this.Return(ReturnResult.CompleteWorkflowExecution(null))

    // Wait
    member this.Bind(action:WaitAction, f:(unit -> RespondDecisionTaskCompletedRequest)) = 
        Wait()

    // Schedule Activity Task (do!)
    member this.Bind(action:ScheduleActivityTaskAction, f:(unit -> RespondDecisionTaskCompletedRequest)) = 
        let DoFunction (result:ScheduleActivityTaskResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Schedule Activity Task (let!)
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

                let result = ScheduleActivityTaskResult.Scheduling(attr)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)
            
            // Completed
            | SomeEventOfType(EventType.ActivityTaskCompleted) hev ->
                let result = ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            // Canceled
            | SomeEventOfType(EventType.ActivityTaskCanceled) hev ->
                let result = ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            // Failed
            | SomeEventOfType(EventType.ActivityTaskFailed) hev ->
                let result = ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)
        
            // TimedOut
            | SomeEventOfType(EventType.ActivityTaskTimedOut) hev ->
                let result = ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            // ScheduleFailed
            | SomeEventOfType(EventType.ScheduleActivityTaskFailed) hev ->
                let result = ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            // Started
            | SomeEventOfType(EventType.ActivityTaskStarted) hev ->
                let result = ScheduleActivityTaskResult.Started(hev.ActivityTaskStartedEventAttributes, hev.ActivityTaskScheduledEventAttributes)
                
                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            // Scheduled
            | SomeEventOfType(EventType.ActivityTaskScheduled) hev ->
                let result = ScheduleActivityTaskResult.Scheduled(hev.ActivityTaskScheduledEventAttributes)

                Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result false

                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of ScheduleActivityTaskAction."))

        | ScheduleActivityTaskAction.ResultFromContext(_, result) ->
            Trace.BuilderBindScheduleActivityTaskAction DecisionTask action result true
            f(result)

    // Wait For Activity Task (do!)
    member this.Bind(WaitForActivityTaskAction.ScheduleResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderBindWaitForActivityTaskAction DecisionTask result

        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait()

    // Wait For Any Activity Tasks (do!)
    member this.Bind(WaitForAnyActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAnyActivityTaskAction DecisionTask results anyFinished

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Activity Tasks (do!)
    member this.Bind(WaitForAllActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAllActivityTaskAction DecisionTask results allFinished

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Request Cancel Activity Task (do!)
    member this.Bind(action:RequestCancelActivityTaskAction, f:(unit -> RespondDecisionTaskCompletedRequest)) = 
        let DoFunction (result:RequestCancelActivityTaskResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Request Cancel Activity Task (let!)
    member this.Bind(RequestCancelActivityTaskAction.ScheduleResult(schedule), f:(RequestCancelActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        let action = RequestCancelActivityTaskAction.ScheduleResult(schedule)
        match schedule with 
        // Scheduling
        | ScheduleActivityTaskResult.Scheduling(_) -> 
            Trace.BuilderBindRequestCancelActivityTaskAction DecisionTask action None

            Wait()

        // ScheduleFailed
        | ScheduleActivityTaskResult.ScheduleFailed(_) -> f(RequestCancelActivityTaskResult.ActivityScheduleFailed)

        | _ -> 
            if (schedule.IsFinished()) then
                let result = RequestCancelActivityTaskResult.ActivityFinished

                Trace.BuilderBindRequestCancelActivityTaskAction DecisionTask action (Some(result))

                f(result)
            else
                let activityId = 
                    match schedule with
                    | ScheduleActivityTaskResult.Scheduled(sched)  -> sched.ActivityId
                    | ScheduleActivityTaskResult.Started(_, sched) -> sched.ActivityId
                    | _ -> raise (Exception("Unable to determine activity id"))

                let cancelHistory = walker.FindRequestCancelActivityTask(activityId)
                
                match cancelHistory with
                | None -> 
                    let d = new Decision()
                    d.DecisionType <- DecisionType.RequestCancelActivityTask
                    d.RequestCancelActivityTaskDecisionAttributes <- new RequestCancelActivityTaskDecisionAttributes()
                    d.RequestCancelActivityTaskDecisionAttributes.ActivityId <- activityId
                    response.Decisions.Add(d)

                    let result = RequestCancelActivityTaskResult.Requesting

                    Trace.BuilderBindRequestCancelActivityTaskAction DecisionTask action (Some(result))

                    f(result)

                | SomeEventOfType(EventType.ActivityTaskCancelRequested) hev ->
                    let result = RequestCancelActivityTaskResult.CancelRequested(hev.ActivityTaskCancelRequestedEventAttributes)

                    Trace.BuilderBindRequestCancelActivityTaskAction DecisionTask action (Some(result))

                    f(result)

                | SomeEventOfType(EventType.RequestCancelActivityTaskFailed) hev ->
                    let result = RequestCancelActivityTaskResult.RequestCancelFailed(hev.RequestCancelActivityTaskFailedEventAttributes)

                    Trace.BuilderBindRequestCancelActivityTaskAction DecisionTask action (Some(result))

                    f(result)

                | _ -> raise (FlowSharpBuilderException(cancelHistory, "Unexpected event history of RequestCancelActivityTaskAction."))

    // Start Child Workflow Execution (do!)
    member this.Bind(action:StartChildWorkflowExecutionAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let DoFunction (result:StartChildWorkflowExecutionResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Start Child Workflow Execution (let!)
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
                
                let result = StartChildWorkflowExecutionResult.Starting(d.StartChildWorkflowExecutionDecisionAttributes)

                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                
                f(result)

            // Completed
            | SomeEventOfType(EventType.ChildWorkflowExecutionCompleted) hev ->
                let result = StartChildWorkflowExecutionResult.Completed(hev.ChildWorkflowExecutionCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)
                 
            // Canceled
            | SomeEventOfType(EventType.ChildWorkflowExecutionCanceled) hev ->
                let result = StartChildWorkflowExecutionResult.Canceled(hev.ChildWorkflowExecutionCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // TimedOut
            | SomeEventOfType(EventType.ChildWorkflowExecutionTimedOut) hev ->
                let result = StartChildWorkflowExecutionResult.TimedOut(hev.ChildWorkflowExecutionTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Failed
            | SomeEventOfType(EventType.ChildWorkflowExecutionFailed) hev ->
                let result = StartChildWorkflowExecutionResult.Failed(hev.ChildWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Terminated
            | SomeEventOfType(EventType.ChildWorkflowExecutionTerminated) hev ->
                let result = StartChildWorkflowExecutionResult.Terminated(hev.ChildWorkflowExecutionTerminatedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // StartChildWorkflowExecutionFailed
            | SomeEventOfType(EventType.StartChildWorkflowExecutionFailed) hev ->
                let result = StartChildWorkflowExecutionResult.StartFailed(hev.StartChildWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Started
            | SomeEventOfType(EventType.ChildWorkflowExecutionStarted) hev ->
                let result = StartChildWorkflowExecutionResult.Started(hev.ChildWorkflowExecutionStartedEventAttributes)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Initiated
            | SomeEventOfType(EventType.StartChildWorkflowExecutionInitiated) hev ->
                let result = StartChildWorkflowExecutionResult.Initiated(hev.StartChildWorkflowExecutionInitiatedEventAttributes)
                Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of StartChildWorkflowExecutionAction."))

        | StartChildWorkflowExecutionAction.ResultFromContext(_, result) ->
            Trace.BuilderBindStartChildWorkflowExecutionAction DecisionTask action result true
            f(result)

    // Wait For Child Workflow Execution (do!)
    member this.Bind(WaitForChildWorkflowExecutionAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderBindWaitForChildWorkflowExecutionAction DecisionTask result

        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait() 

    // Wait For Any Child Workflow Execution (do!)
    member this.Bind(WaitForAnyChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAnyChildWorkflowExecutionAction DecisionTask results anyFinished

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Child Workflow Execution (do!)
    member this.Bind(WaitForAllChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAllChildWorkflowExecutionAction DecisionTask results allFinished

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Request Cancel External Workflow Execution (do!)
    member this.Bind(action:RequestCancelExternalWorkflowExecutionAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let DoFunction (result:RequestCancelExternalWorkflowExecutionResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Request Cancel External Workflow Execution (let!)
    member this.Bind(RequestCancelExternalWorkflowExecutionAction.Attributes(attr), f:(RequestCancelExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let action = RequestCancelExternalWorkflowExecutionAction.Attributes(attr)

        let combinedHistory = walker.FindRequestCancelExternalWorkflowExecution(attr)
        
        match (combinedHistory) with
        | None ->
            let d = new Decision();
            d.DecisionType <- DecisionType.RequestCancelExternalWorkflowExecution
            d.RequestCancelExternalWorkflowExecutionDecisionAttributes <- attr
            response.Decisions.Add(d)
                
            let result = RequestCancelExternalWorkflowExecutionResult.Requesting(attr)
            Trace.BuilderBindRequestCancelExternalWorkflowExecutionAction DecisionTask action result
            f(result)
            
        // Request Delivered
        | SomeEventOfType(EventType.ExternalWorkflowExecutionCancelRequested) hev ->
            let result = RequestCancelExternalWorkflowExecutionResult.Delivered(hev.ExternalWorkflowExecutionCancelRequestedEventAttributes)
            f(result)

        // Request Failed
        | SomeEventOfType(EventType.RequestCancelExternalWorkflowExecutionFailed) hev ->
            let result = RequestCancelExternalWorkflowExecutionResult.Failed(hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes)
            Trace.BuilderBindRequestCancelExternalWorkflowExecutionAction DecisionTask action result
            f(result)
 
        // Request Initiated
        | SomeEventOfType(EventType.RequestCancelExternalWorkflowExecutionInitiated) hev ->
            let result = RequestCancelExternalWorkflowExecutionResult.Initiated(hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes)
            Trace.BuilderBindRequestCancelExternalWorkflowExecutionAction DecisionTask action result
            f(result)

        | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of RequestCancelExternalWorkflowExecutionAction."))

    // Schedule Lambda Function (do!)
    member this.Bind(action:ScheduleLambdaFunctionAction, f:(unit -> RespondDecisionTaskCompletedRequest)) = 
        let DoFunction (result:ScheduleLambdaFunctionResult) : RespondDecisionTaskCompletedRequest =
            f()

        this.Bind(action, DoFunction)

    // Schedule Lambda Function (let!)
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
                
                let result = ScheduleLambdaFunctionResult.Scheduling(attr)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // Lambda Function Completed
            | SomeEventOfType(EventType.LambdaFunctionCompleted) hev -> 
                let result = ScheduleLambdaFunctionResult.Completed(hev.LambdaFunctionCompletedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // Lambda Function Failed
            | SomeEventOfType(EventType.LambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.Failed(hev.LambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // Lambda Function TimedOut
            | SomeEventOfType(EventType.LambdaFunctionTimedOut) hev -> 
                let result = ScheduleLambdaFunctionResult.TimedOut(hev.LambdaFunctionTimedOutEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // StartLambdaFunctionFailed
            | SomeEventOfType(EventType.StartLambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.StartFailed(hev.StartLambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // ScheduleLambdaFunctionFailed
            | SomeEventOfType(EventType.ScheduleLambdaFunctionFailed) hev -> 
                let result = ScheduleLambdaFunctionResult.ScheduleFailed(hev.ScheduleLambdaFunctionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // LambdaFunctionStarted
            | SomeEventOfType(EventType.LambdaFunctionStarted) hev ->
                let result = ScheduleLambdaFunctionResult.Started(hev.LambdaFunctionStartedEventAttributes, hev.LambdaFunctionScheduledEventAttributes)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            // LambdaFunctionScheduled
            | SomeEventOfType(EventType.LambdaFunctionScheduled) hev ->
                let result = ScheduleLambdaFunctionResult.Scheduled(hev.LambdaFunctionScheduledEventAttributes)
                Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of ScheduleLambdaFunctionAction."))

        | ScheduleLambdaFunctionAction.ResultFromContext(_, result) ->
            Trace.BuilderBindScheduleLambdaFunctionAction DecisionTask action result true
            f(result)

    // Wait For Lambda Function (do!)
    member this.Bind(WaitForLambdaFunctionAction.ScheduleResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderBindWaitForLambdaFunctionAction DecisionTask result

        match (result.IsFinished()) with 
        | true  -> f()
        | false -> Wait() 

    // Wait For Any Lambda Function (do!)
    member this.Bind(WaitForAnyLambdaFunctionAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAnyLambdaFunctionAction DecisionTask results anyFinished

        match anyFinished with
        | true  -> f()
        | false -> Wait()

    // Wait For All Lambda Function (do!)
    member this.Bind(WaitForAllLambdaFunctionAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        Trace.BuilderBindWaitForAllLambdaFunctionAction DecisionTask results allFinished

        match allFinished with
        | true  -> f()
        | false -> Wait()

    // Start Timer (do!)
    member this.Bind(action:StartTimerAction, f:(unit -> RespondDecisionTaskCompletedRequest)) = 
        let DoFunction (result:StartTimerResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Start Timer (let!)
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
                
                let result = StartTimerResult.Starting(d.StartTimerDecisionAttributes)
                Trace.BuilderBindStartTimerAction DecisionTask action result false
                f(result)

            // Fired
            | SomeEventOfType(EventType.TimerFired) hev ->
                let result = StartTimerResult.Fired(hev.TimerFiredEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartTimerAction DecisionTask action result false
                f(result)

            // Canceled
            | SomeEventOfType(EventType.TimerCanceled) hev ->
                let result = StartTimerResult.Canceled(hev.TimerCanceledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartTimerAction DecisionTask action result false
                f(result)

            // StartTimerFailed
            | SomeEventOfType(EventType.StartTimerFailed) hev ->
                let result = StartTimerResult.StartTimerFailed(hev.StartTimerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindStartTimerAction DecisionTask action result false
                f(result)

            // TimerStarted
            | SomeEventOfType(EventType.TimerStarted) hev ->
                let result = StartTimerResult.Started(hev.TimerStartedEventAttributes)
                Trace.BuilderBindStartTimerAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of StartTimerAction."))

        | StartTimerAction.ResultFromContext(_, result) -> 
            Trace.BuilderBindStartTimerAction DecisionTask action result true
            f(result)

    // Wait For Timer (do!)
    member this.Bind(WaitForTimerAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let action = WaitForTimerAction.StartResult(result)
        Trace.BuilderBindWaitForTimerAction DecisionTask action

        match result with 
        // Canceled | Fired | StartTimerFailed
        | StartTimerResult.Canceled(_)
        | StartTimerResult.Fired(_)
        | StartTimerResult.StartTimerFailed(_) -> f()

        // Started | Starting
        | StartTimerResult.Started(_)
        | StartTimerResult.Starting(_) -> 
            Wait()

    // Cancel Timer (do!)
    member this.Bind(action:CancelTimerAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let DoFunction (result:CancelTimerResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Cancel Timer (let!)
    member this.Bind(CancelTimerAction.StartResult(result), f:(CancelTimerResult -> RespondDecisionTaskCompletedRequest)) =
        let action = CancelTimerAction.StartResult(result)

        match result with 
        // Starting
        | StartTimerResult.Starting(_) -> 
            Trace.BuilderBindCancelTimerAction DecisionTask action None
            Wait()

        // Canceled
        | StartTimerResult.Canceled(attr) -> 
            let result = CancelTimerResult.Canceled(attr)
            Trace.BuilderBindCancelTimerAction DecisionTask action (Some(result))
            f(result)

        // Fired
        | StartTimerResult.Fired(attr) -> 
            let result = CancelTimerResult.Fired(attr)
            Trace.BuilderBindCancelTimerAction DecisionTask action (Some(result))
            f(result)

        // StartTimerFailed
        | StartTimerResult.StartTimerFailed(attr) -> 
            let result = CancelTimerResult.StartTimerFailed(attr)
            Trace.BuilderBindCancelTimerAction DecisionTask action (Some(result))
            f(result)

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

                let result = CancelTimerResult.Canceling
                Trace.BuilderBindCancelTimerAction DecisionTask action (Some(result))
                f(result)

            // CancelTimerFailed
            | SomeEventOfType(EventType.CancelTimerFailed) hev ->
                let result = CancelTimerResult.CancelTimerFailed(hev.CancelTimerFailedEventAttributes)
                Trace.BuilderBindCancelTimerAction DecisionTask action (Some(result))
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of CancelTimerAction."))

    // Marker Recorded (let!)
    member this.Bind(action:MarkerRecordedAction, f:(MarkerRecordedResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | MarkerRecordedAction.Attributes(markerName, pushToContext) ->
            let combinedHistory = walker.FindMarker(markerName)

            match combinedHistory with
            // NotRecorded
            | None ->
                let result = MarkerRecordedResult.NotRecorded
                Trace.BuilderBindMarkerRecordedAction DecisionTask action result false
                f(result)

            // RecordMarkerFailed
            | SomeEventOfType(EventType.RecordMarkerFailed) hev ->
                let result = MarkerRecordedResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(markerName, result)
                Trace.BuilderBindMarkerRecordedAction DecisionTask action result false
                f(result)

            // MarkerRecorded
            | SomeEventOfType(EventType.MarkerRecorded) hev ->
                let result = MarkerRecordedResult.Recorded(hev.MarkerRecordedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(markerName, result)
                Trace.BuilderBindMarkerRecordedAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of MarkerRecordedAction."))

        | MarkerRecordedAction.ResultFromContext(_, result) ->
            Trace.BuilderBindMarkerRecordedAction DecisionTask action result true
            f(result)

    // Record Marker (do!)
    member this.Bind(action:RecordMarkerAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let DoFunction (result:RecordMarkerResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Record Marker (let!)
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

                let result = RecordMarkerResult.Recording
                Trace.BuilderBindRecordMarkerAction DecisionTask action result false
                f(result)
            
            // RecordMarkerFailed
            | SomeEventOfType(EventType.RecordMarkerFailed) hev ->
                let result = RecordMarkerResult.RecordMarkerFailed(hev.RecordMarkerFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindRecordMarkerAction DecisionTask action result false
                f(result)

            // MarkerRecorded
            | SomeEventOfType(EventType.MarkerRecorded) hev ->
                let result = RecordMarkerResult.Recorded(hev.MarkerRecordedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindRecordMarkerAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of RecordMarkerAction."))

        | RecordMarkerAction.ResultFromContext(_, result) ->
            Trace.BuilderBindRecordMarkerAction DecisionTask action result true
            f(result)

    // Signal External Workflow Execution (do!)
    member this.Bind(action:SignalExternalWorkflowExecutionAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let DoFunction (result:SignalExternalWorkflowExecutionResult) : RespondDecisionTaskCompletedRequest =            
            f()

        this.Bind(action, DoFunction)

    // Signal External Workflow Execution (let!)
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

                let result = SignalExternalWorkflowExecutionResult.Signaling
                Trace.BuilderBindSignalExternalWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Signaled
            | SomeEventOfType(EventType.ExternalWorkflowExecutionSignaled) hev ->
                let result = SignalExternalWorkflowExecutionResult.Signaled(hev.ExternalWorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindSignalExternalWorkflowExecutionAction DecisionTask action result false
                f(result)

            // Failed
            | SomeEventOfType(EventType.SignalExternalWorkflowExecutionFailed) hev ->
                let result = SignalExternalWorkflowExecutionResult.Failed(hev.SignalExternalWorkflowExecutionFailedEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(attr, result)
                Trace.BuilderBindSignalExternalWorkflowExecutionAction DecisionTask action result false
                f(result)
        
            // Initiated
            | SomeEventOfType(EventType.SignalExternalWorkflowExecutionInitiated) hev ->
                let result = SignalExternalWorkflowExecutionResult.Initiated(hev.SignalExternalWorkflowExecutionInitiatedEventAttributes)
                Trace.BuilderBindSignalExternalWorkflowExecutionAction DecisionTask action result false
                f(result)

            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of SignalExternalWorkflowExecutionAction."))

        | SignalExternalWorkflowExecutionAction.ResultFromContext(_, result) ->
            Trace.BuilderBindSignalExternalWorkflowExecutionAction DecisionTask action result true
            f(result)
            
    // Wait For Workflow Execution Signaled (do!)
    member this.Bind(action:WaitForWorkflowExecutionSignaledAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | WaitForWorkflowExecutionSignaledAction.Attributes(signalName, pushToContext) ->
            let combinedHistory = walker.FindSignaled(signalName)

            match combinedHistory with
            // Not Signaled, keep waiting
            | None ->
                Trace.BuilderBindWaitForWorkflowExecutionSignaledAction DecisionTask action None false
                Wait()

            // Signaled
            | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
                let result = WorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(signalName, result)
                Trace.BuilderBindWaitForWorkflowExecutionSignaledAction DecisionTask action (Some(result)) false
                f()
        
            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of WaitForWorkflowExecutionSignaledAction."))

        | WaitForWorkflowExecutionSignaledAction.ResultFromContext(_, result) ->
            Trace.BuilderBindWaitForWorkflowExecutionSignaledAction DecisionTask action (Some(result)) true
            f()

    // Workflow Execution Signaled (let!)
    member this.Bind(action:WorkflowExecutionSignaledAction, f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let action = if ContextManager.IsSome then ContextManager.Value.Pull(action) else action
        match action with
        | WorkflowExecutionSignaledAction.Attributes(signalName, pushToContext) ->
            let combinedHistory = walker.FindSignaled(signalName)

            match combinedHistory with
            // Not Signaled
            | None ->
                let result = WorkflowExecutionSignaledResult.NotSignaled
                Trace.BuilderBindWorkflowExecutionSignaledAction DecisionTask action result false
                f(result)

            // Signaled
            | SomeEventOfType(EventType.WorkflowExecutionSignaled) hev ->
                let result = WorkflowExecutionSignaledResult.Signaled(hev.WorkflowExecutionSignaledEventAttributes)
                if (pushToContext && ContextManager.IsSome) then ContextManager.Value.Push(signalName, result)
                Trace.BuilderBindWorkflowExecutionSignaledAction DecisionTask action result false
                f(result)
        
            | _ -> raise (FlowSharpBuilderException(combinedHistory, "Unexpected event history of WorkflowExecutionSignaledAction."))

        | WorkflowExecutionSignaledAction.ResultFromContext(_, result) ->
            Trace.BuilderBindWorkflowExecutionSignaledAction DecisionTask action result true
            f(result)

    // Workflow Execution Cancel Requested (let!)
    member this.Bind(action:WorkflowExecutionCancelRequestedAction, f:(WorkflowExecutionCancelRequestedResult -> RespondDecisionTaskCompletedRequest)) =
        let cancelRequestedEvent = walker.FindWorkflowExecutionCancelRequested()

        match cancelRequestedEvent with
        // NotRequested
        | None ->
            let result = WorkflowExecutionCancelRequestedResult.NotRequested
            Trace.BuilderBindWorkflowExecutionCancelRequestedAction DecisionTask action result
            f(result)

        // Workflow Cancel Requsted
        | SomeEventOfType(EventType.WorkflowExecutionCancelRequested) hev ->
            let result = WorkflowExecutionCancelRequestedResult.CancelRequested(hev.WorkflowExecutionCancelRequestedEventAttributes)
            Trace.BuilderBindWorkflowExecutionCancelRequestedAction DecisionTask action result
            f(result)
            
        | _ -> raise (FlowSharpBuilderException(cancelRequestedEvent, "Unexpected event history of WorkflowExecutionCancelRequestedAction."))

    // Get Workflow Execution Input (let!)
    member this.Bind(action:GetWorkflowExecutionInputAction, f:(string -> RespondDecisionTaskCompletedRequest)) =

        let startedEvent = walker.FindWorkflowExecutionStarted()

        match (startedEvent) with
        | None ->
            Trace.BuilderBindGetWorkflowExecutionInputAction DecisionTask action null
            f(null)

        | SomeEventOfType(EventType.WorkflowExecutionStarted) hev ->
            let input = hev.WorkflowExecutionStartedEventAttributes.Input
            Trace.BuilderBindGetWorkflowExecutionInputAction DecisionTask action input
            f(input)

        | _ -> raise (FlowSharpBuilderException(startedEvent, "Unexpected event history of GetWorkflowExecutionInputAction."))

    // Get Execution Context (let!)
    member this.Bind(action:GetExecutionContextAction, f:(string -> RespondDecisionTaskCompletedRequest)) =

        let completedEvent = walker.FindLatestDecisionTaskCompleted()

        match (completedEvent) with
        | None ->
            Trace.BuilderBindGetExecutionContextAction DecisionTask action null
            f(null)

        | SomeEventOfType(EventType.DecisionTaskCompleted) hev ->
            let context = hev.DecisionTaskCompletedEventAttributes.ExecutionContext
            Trace.BuilderBindGetExecutionContextAction DecisionTask action context
            f(context)

        | _ -> raise (FlowSharpBuilderException(completedEvent, "Unexpected event history of GetExecutionContextAction."))

    // Set Execution Context (do!)
    member this.Bind(SetExecutionContextAction.Attributes(context), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderBindSetExecutionContextAction DecisionTask (SetExecutionContextAction.Attributes(context))
        response.ExecutionContext <- context
        f()

    // Remove From Context (do!)
    member this.Bind(action:RemoveFromContextAction, f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderBindRemoveFromContextAction DecisionTask action
        if ContextManager.IsSome then ContextManager.Value.Remove(action)
        f()

    // For Loop
    member this.For(enumeration:seq<'T>, f:(_ -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderForLoop DecisionTask

        let processForBlock x = 
            Trace.BuilderForLoopIteration DecisionTask blockFlag
            if not blockFlag then f(x) |> ignore
            (not blockFlag)

        enumeration |>
        Seq.takeWhile processForBlock |>
        Seq.iter (fun x -> ())

    // While Loop
    member this.While(condition:(unit -> bool), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderWhileLoop DecisionTask

        while (not blockFlag) && condition() do
            Trace.BuilderWhileLoopIteration DecisionTask blockFlag
            f() |> ignore

    // Combine
    member this.Combine(exprBefore, fAfter) =
        // We assume the exprBefore decisions have been added to the response already
        // Just need to run the expression after this, which will add their decisions while executing
        Trace.BuilderCombine DecisionTask blockFlag
        if blockFlag then response else fAfter()

    // Try Finally
    member this.TryFinally(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprFinally:(unit -> unit)) =
        Trace.BuilderTryFinallyTry DecisionTask
        try 
            exprInside()
        finally
            Trace.BuilderTryFinallyFinally DecisionTask blockFlag
            if not blockFlag then exprFinally()

    // Try With
    member this.TryWith(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprWith:(Exception -> RespondDecisionTaskCompletedRequest)) =
        Trace.BuilderTryWithTry DecisionTask
        try
            exprInside()
        with
        | e ->
            Trace.BuilderTryWithWith DecisionTask e
            exprWith e

