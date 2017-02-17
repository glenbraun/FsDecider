namespace FsDecider.UnitTests

open FsDecider
open FsDecider.Actions
open FsDecider.ExecutionContext
open FsDecider.UnitTests.TestHelper
open FsDecider.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestExecutionContextManager =
    let OfflinePollAndDecide deciderFunc =
        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
            let workflowId = "TestExecutionContextManager"
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.LambdaRole, TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ScheduleActivityTask = 
        let decision = ScheduleActivityTaskDecisionAttributes()
        decision.ActivityId <- "ActivityId1"
        decision.ActivityType <- TestConfiguration.ActivityType
        decision.Control <- "Control1"
        decision.Input <- "Activity Input"            
        decision


    let ``Test context for ScheduleActivityTask with Completed result``() =
        let event = ActivityTaskCompletedEventAttributes()
        event.Result <- "OK"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)

            match result with
            | ScheduleActivityTaskResult.Completed(hev) 
                when hev.Result = event.Result -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc
  
    let ``Test context for ScheduleActivityTask with Canceled result``() =
        let event = ActivityTaskCanceledEventAttributes()
        event.Details <- "Cancel Details"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.Canceled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)

            match result with
            | ScheduleActivityTaskResult.Canceled(hev) 
                when hev.Details = event.Details -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc
  
    let ``Test context for ScheduleActivityTask with TimedOut result``() =
        let event = ActivityTaskTimedOutEventAttributes()
        event.Details <- "TimedOut Details"
        event.TimeoutType <- ActivityTaskTimeoutType.HEARTBEAT        
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.TimedOut(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)

            match result with
            | ScheduleActivityTaskResult.TimedOut(hev) 
                when hev.Details = event.Details &&
                     hev.TimeoutType = event.TimeoutType -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc
            

    let ``Test context for ScheduleActivityTask with Failed result``() =
        let event = ActivityTaskFailedEventAttributes()
        event.Details <- "Failed Details"
        event.Reason <- "Failed Reason"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.Failed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)

            match result with
            | ScheduleActivityTaskResult.Failed(hev) 
                when hev.Details = event.Details &&
                     hev.Reason = event.Reason -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc
            
    let ``Test context for ScheduleActivityTask with ScheduleFailed result``() =
        let event = ScheduleActivityTaskFailedEventAttributes()
        event.ActivityId <- ScheduleActivityTask.ActivityId
        event.ActivityType <- ScheduleActivityTask.ActivityType
        event.Cause <- ScheduleActivityTaskFailedCause.OPEN_ACTIVITIES_LIMIT_EXCEEDED        
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.ScheduleFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)

            match result with
            | ScheduleActivityTaskResult.ScheduleFailed(hev) 
                when hev.ActivityId = event.ActivityId &&
                     hev.ActivityType.Name = event.ActivityType.Name &&
                     hev.ActivityType.Version = event.ActivityType.Version &&
                     hev.Cause = event.Cause -> return "TEST PASS"

            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ScheduleLambdaFunction = 
        let decision = ScheduleLambdaFunctionDecisionAttributes()
        decision.Id <- "lambda1"
        decision.Input <- "Lambda Input"
        decision.Name <- TestConfiguration.LambdaName
        decision

    let ``Test context for ScheduleLambdaFunction with Completed result``() =
        let event = LambdaFunctionCompletedEventAttributes()
        event.Result <- "Lambda Result"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)

            match result with
            | ScheduleLambdaFunctionResult.Completed(hev) 
                when hev.Result = event.Result -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for ScheduleLambdaFunction with Failed result``() =
        let event = LambdaFunctionFailedEventAttributes()
        event.Details <- "Failed Details"
        event.Reason <- "Failed Reason"        
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.Failed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)

            match result with
            | ScheduleLambdaFunctionResult.Failed(hev) 
                when hev.Details = event.Details &&
                     hev.Reason = event.Reason  -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for ScheduleLambdaFunction with TimedOut result``() =
        let event = LambdaFunctionTimedOutEventAttributes()
        event.TimeoutType <- LambdaFunctionTimeoutType.START_TO_CLOSE
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.TimedOut(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)

            match result with
            | ScheduleLambdaFunctionResult.TimedOut(hev) 
                when hev.TimeoutType = event.TimeoutType -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for ScheduleLambdaFunction with StartFailed result``() =
        let event = StartLambdaFunctionFailedEventAttributes()
        event.Cause <- StartLambdaFunctionFailedCause.ASSUME_ROLE_FAILED
        event.Message <- "Start Failed Message"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.StartFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)

            match result with
            | ScheduleLambdaFunctionResult.StartFailed(hev) 
                when hev.Cause = event.Cause &&
                     hev.Message = event.Message -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for ScheduleLambdaFunction with ScheduleFailed result``() =
        let event = ScheduleLambdaFunctionFailedEventAttributes()
        event.Cause <- ScheduleLambdaFunctionFailedCause.LAMBDA_SERVICE_NOT_AVAILABLE_IN_REGION
        event.Id <- ScheduleLambdaFunction.Id
        event.Name <- ScheduleLambdaFunction.Name
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.ScheduleFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)

            match result with
            | ScheduleLambdaFunctionResult.ScheduleFailed(hev) 
                when hev.Cause = event.Cause &&
                     hev.Id = event.Id &&
                     hev.Name = event.Name -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let StartChildWorkflowExecution = 
        let decision = StartChildWorkflowExecutionDecisionAttributes()
        decision.ChildPolicy <- ChildPolicy.TERMINATE
        decision.Control <- "Control 1"
        decision.Input <- "Child Input"
        decision.LambdaRole <- TestConfiguration.LambdaResult
        decision.WorkflowId <- "Child Workflow Id"
        decision.WorkflowType <- TestConfiguration.WorkflowType
        decision

    let ``Test context for StartChildWorkflowExecution with Completed result``() =
        let event = ChildWorkflowExecutionCompletedEventAttributes()
        event.Result <- "Child Result"
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.Completed(hev) 
                when hev.Result = event.Result &&
                     hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartChildWorkflowExecution with Canceled result``() =
        let event = ChildWorkflowExecutionCanceledEventAttributes()
        event.Details <- "Child Details"
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.Canceled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.Canceled(hev) 
                when hev.Details = event.Details &&
                     hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartChildWorkflowExecution with Failed result``() =
        let event = ChildWorkflowExecutionFailedEventAttributes()
        event.Details <- "Child Details"
        event.Reason <- "Child Reason"
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.Failed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.Failed(hev) 
                when hev.Details = event.Details &&
                     hev.Reason = event.Reason &&
                     hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartChildWorkflowExecution with TimedOut result``() =
        let event = ChildWorkflowExecutionTimedOutEventAttributes()
        event.TimeoutType <- WorkflowExecutionTimeoutType.START_TO_CLOSE
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.TimedOut(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.TimedOut(hev) 
                when hev.TimeoutType = event.TimeoutType &&
                     hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartChildWorkflowExecution with Terminated result``() =
        let event = ChildWorkflowExecutionTerminatedEventAttributes()
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.Terminated(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.Terminated(hev) 
                when hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartChildWorkflowExecution with StartFailed result``() =
        let event = StartChildWorkflowExecutionFailedEventAttributes()
        event.Cause <- StartChildWorkflowExecutionFailedCause.DEFAULT_EXECUTION_START_TO_CLOSE_TIMEOUT_UNDEFINED
        event.Control <- "Control 1"
        event.WorkflowId <- StartChildWorkflowExecution.WorkflowId
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.StartFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)

            match result with
            | StartChildWorkflowExecutionResult.StartFailed(hev) 
                when hev.Cause = event.Cause &&
                     hev.Control = event.Control &&
                     hev.WorkflowId = event.WorkflowId &&
                     hev.WorkflowType.Name = event.WorkflowType.Name &&
                     hev.WorkflowType.Version = event.WorkflowType.Version -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let StartTimer = 
        let decision = StartTimerDecisionAttributes()
        decision.Control <- "Control 1"
        decision.TimerId <- "Timer 1"
        decision

    let ``Test context for StartTimer with Fired result``() =
        let event = TimerFiredEventAttributes()
        event.TimerId <- StartTimer.TimerId
            
        let context = ExecutionContextManager()
        context.Push(StartTimer, StartTimerResult.Fired(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartTimerAction.Attributes(StartTimer, true)

            match result with
            | StartTimerResult.Fired(hev) 
                when hev.TimerId = event.TimerId -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartTimer with Canceled result``() =
        let event = TimerCanceledEventAttributes()
        event.TimerId <- StartTimer.TimerId
            
        let context = ExecutionContextManager()
        context.Push(StartTimer, StartTimerResult.Canceled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartTimerAction.Attributes(StartTimer, true)

            match result with
            | StartTimerResult.Canceled(hev) 
                when hev.TimerId = event.TimerId -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for StartTimer with StartTimerFailed result``() =
        let event = StartTimerFailedEventAttributes()
        event.TimerId <- StartTimer.TimerId
        event.Cause <- StartTimerFailedCause.OPEN_TIMERS_LIMIT_EXCEEDED
            
        let context = ExecutionContextManager()
        context.Push(StartTimer, StartTimerResult.StartTimerFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = StartTimerAction.Attributes(StartTimer, true)

            match result with
            | StartTimerResult.StartTimerFailed(hev) 
                when hev.TimerId = event.TimerId &&
                     hev.Cause = event.Cause -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let WorkflowExecutionSignaled = 
        let signalName = "Test Signal"
        signalName

    let ``Test context for WorkflowExecutionSignaled with Signaled result``() =
        let event = WorkflowExecutionSignaledEventAttributes()
        event.SignalName <- WorkflowExecutionSignaled
        event.Input <- "Signal Input"
            
        let context = ExecutionContextManager()
        context.Push(WorkflowExecutionSignaled, WorkflowExecutionSignaledResult.Signaled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = WorkflowExecutionSignaledAction.Attributes(WorkflowExecutionSignaled, true)

            match result with
            | WorkflowExecutionSignaledResult.Signaled(hev) 
                when hev.SignalName = event.SignalName &&
                     hev.Input = event.Input -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let SignalExternalWorkflowExecution = 
        let decision = SignalExternalWorkflowExecutionDecisionAttributes()
        decision.Control <- "Control 1"
        decision.Input <- "Signal Input"
        decision.RunId <- "Signal RunId"
        decision.SignalName <- "Signal Name"
        decision.WorkflowId <- "Signal WorkflowId"
        decision

    let ``Test context for SignalExternalWorkflowExecution with Signaled result``() =
        let event = ExternalWorkflowExecutionSignaledEventAttributes()
        event.WorkflowExecution <- WorkflowExecution(RunId=SignalExternalWorkflowExecution.RunId, WorkflowId=SignalExternalWorkflowExecution.WorkflowId)
            
        let context = ExecutionContextManager()
        context.Push(SignalExternalWorkflowExecution, SignalExternalWorkflowExecutionResult.Signaled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = SignalExternalWorkflowExecutionAction.Attributes(SignalExternalWorkflowExecution, true)

            match result with
            | SignalExternalWorkflowExecutionResult.Signaled(hev) 
                when hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for SignalExternalWorkflowExecution with Failed result``() =
        let event = SignalExternalWorkflowExecutionFailedEventAttributes()
        event.Cause <- SignalExternalWorkflowExecutionFailedCause.UNKNOWN_EXTERNAL_WORKFLOW_EXECUTION
        event.Control <- SignalExternalWorkflowExecution.Control
        event.RunId <- SignalExternalWorkflowExecution.RunId
        event.WorkflowId <- SignalExternalWorkflowExecution.WorkflowId
            
        let context = ExecutionContextManager()
        context.Push(SignalExternalWorkflowExecution, SignalExternalWorkflowExecutionResult.Failed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = SignalExternalWorkflowExecutionAction.Attributes(SignalExternalWorkflowExecution, true)

            match result with
            | SignalExternalWorkflowExecutionResult.Failed(hev) 
                when hev.Cause = event.Cause &&
                     hev.Control = hev.Control &&
                     hev.RunId = event.RunId &&
                     hev.WorkflowId = event.WorkflowId -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let RecordMarker = 
        let decision = RecordMarkerDecisionAttributes()
        decision.Details <- "Record Marker Details"
        decision.MarkerName <- "Marker Name"
        decision

    let ``Test context for RecordMarker with Recorded result``() =
        let event = MarkerRecordedEventAttributes()
        event.Details <- RecordMarker.Details
        event.MarkerName <- RecordMarker.Details
            
        let context = ExecutionContextManager()
        context.Push(RecordMarker, RecordMarkerResult.Recorded(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = RecordMarkerAction.Attributes(RecordMarker, true)

            match result with
            | RecordMarkerResult.Recorded(hev) 
                when hev.Details = event.Details &&
                     hev.MarkerName = event.MarkerName -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for RecordMarker with RecordMarkerFailed result``() =
        let event = RecordMarkerFailedEventAttributes()
        event.Cause <- RecordMarkerFailedCause.OPERATION_NOT_PERMITTED
        event.MarkerName <- RecordMarker.Details
            
        let context = ExecutionContextManager()
        context.Push(RecordMarker, RecordMarkerResult.RecordMarkerFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = RecordMarkerAction.Attributes(RecordMarker, true)

            match result with
            | RecordMarkerResult.RecordMarkerFailed(hev) 
                when hev.Cause = event.Cause &&
                     hev.MarkerName = event.MarkerName -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let MarkerRecorded = 
        let markerName = "Test Marner"
        markerName

    let ``Test context for MarkerRecorded with Recorded result``() =
        let event = MarkerRecordedEventAttributes()
        event.Details <- RecordMarker.Details
        event.MarkerName <- MarkerRecorded
            
        let context = ExecutionContextManager()
        context.Push(MarkerRecorded, MarkerRecordedResult.Recorded(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = MarkerRecordedAction.Attributes(MarkerRecorded, true)

            match result with
            | MarkerRecordedResult.Recorded(hev) 
                when hev.Details = event.Details &&
                     hev.MarkerName = event.MarkerName -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

    let ``Test context for MarkerRecorded with RecordMarkerFailed result``() =
        let event = RecordMarkerFailedEventAttributes()
        event.Cause <- RecordMarkerFailedCause.OPERATION_NOT_PERMITTED
        event.MarkerName <- MarkerRecorded
            
        let context = ExecutionContextManager()
        context.Push(MarkerRecorded, MarkerRecordedResult.RecordMarkerFailed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)
            
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = MarkerRecordedAction.Attributes(MarkerRecorded, true)

            match result with
            | MarkerRecordedResult.RecordMarkerFailed(hev) 
                when hev.Cause = event.Cause &&
                     hev.MarkerName = event.MarkerName -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

         OfflinePollAndDecide deciderFunc

