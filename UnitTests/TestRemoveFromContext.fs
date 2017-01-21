namespace FlowSharp.UnitTests

open FlowSharp
open FlowSharp.Actions
open FlowSharp.ExecutionContext
open FlowSharp.UnitTests.TestHelper
open FlowSharp.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestRemoveFromContext =
    let workflowId = "TestExecutionContextManager"

    let ``Test context for RemoveFromContext using ScheduleActivityTaskAction``() =
        let ScheduleActivityTask = 
            let decision = ScheduleActivityTaskDecisionAttributes()
            decision.ActivityId <- "ActivityId1"
            decision.ActivityType <- TestConfiguration.TestActivityType
            decision.Control <- "Control1"
            decision.Input <- "Activity Input"            
            decision

        let event = ActivityTaskCompletedEventAttributes()
        event.Result <- "OK"
            
        let context = ExecutionContextManager()
        context.Push(ScheduleActivityTask, ScheduleActivityTaskResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = ScheduleActivityTaskAction.Attributes(ScheduleActivityTask, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | ScheduleActivityTaskResult.Completed(hev)                
                when hev.Result = event.Result -> 
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | ScheduleActivityTaskResult.Scheduling(attr)
                    when attr.ActivityId = ScheduleActivityTask.ActivityId &&
                         attr.ActivityType.Name = ScheduleActivityTask.ActivityType.Name &&
                         attr.ActivityType.Version = ScheduleActivityTask.ActivityType.Version &&
                         attr.Control = ScheduleActivityTask.Control -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                    resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                            |> should equal ScheduleActivityTask.ActivityId
                    resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                            |> should equal ScheduleActivityTask.ActivityType.Name
                    resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                            |> should equal ScheduleActivityTask.ActivityType.Version
                    resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                            |> should equal ScheduleActivityTask.Input

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()


    let ``Test context for RemoveFromContext using ScheduleLambdaFunctionAction``() =
        let ScheduleLambdaFunction = 
            let decision = ScheduleLambdaFunctionDecisionAttributes()
            decision.Id <- "lambda1"
            decision.Input <- "Lambda Input"
            decision.Name <- TestConfiguration.TestLambdaName
            decision

        let event = LambdaFunctionCompletedEventAttributes()
        event.Result <- TestConfiguration.TestLambdaResult
            
        let context = ExecutionContextManager()
        context.Push(ScheduleLambdaFunction, ScheduleLambdaFunctionResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = ScheduleLambdaFunctionAction.Attributes(ScheduleLambdaFunction, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | ScheduleLambdaFunctionResult.Completed(hev)                
                when hev.Result = event.Result -> 
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | ScheduleLambdaFunctionResult.Scheduling(attr)
                    when attr.Id = ScheduleLambdaFunction.Id &&
                         attr.Name = ScheduleLambdaFunction.Name &&
                         attr.Input = ScheduleLambdaFunction.Input -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal ScheduleLambdaFunction.Id
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal ScheduleLambdaFunction.Input
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal ScheduleLambdaFunction.Name

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ``Test context for RemoveFromContext using StartChildWorkflowExecutionAction``() =
        let StartChildWorkflowExecution = 
            let decision = StartChildWorkflowExecutionDecisionAttributes()
            decision.ChildPolicy <- ChildPolicy.TERMINATE
            decision.Control <- "Control 1"
            decision.Input <- "Child Input"
            decision.LambdaRole <- TestConfiguration.TestLambdaResult
            decision.WorkflowId <- "Child Workflow Id"
            decision.WorkflowType <- TestConfiguration.TestWorkflowType
            decision

        let event = ChildWorkflowExecutionCompletedEventAttributes()
        event.Result <- "Child Result"
        event.WorkflowExecution <- WorkflowExecution(RunId="Child RunId", WorkflowId=StartChildWorkflowExecution.WorkflowId)
        event.WorkflowType <- StartChildWorkflowExecution.WorkflowType        
            
        let context = ExecutionContextManager()
        context.Push(StartChildWorkflowExecution, StartChildWorkflowExecutionResult.Completed(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = StartChildWorkflowExecutionAction.Attributes(StartChildWorkflowExecution, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | StartChildWorkflowExecutionResult.Completed(hev)                
                when hev.Result = event.Result &&
                        hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                        hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId &&
                        hev.WorkflowType.Name = event.WorkflowType.Name &&
                        hev.WorkflowType.Version = event.WorkflowType.Version ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | StartChildWorkflowExecutionResult.Starting(hev) 
                    when hev.ChildPolicy = StartChildWorkflowExecution.ChildPolicy &&
                         hev.Control = StartChildWorkflowExecution.Control &&
                         hev.Input = StartChildWorkflowExecution.Input &&
                         hev.LambdaRole = StartChildWorkflowExecution.LambdaRole &&
                         hev.WorkflowId = StartChildWorkflowExecution.WorkflowId &&
                         hev.WorkflowType.Name = StartChildWorkflowExecution.WorkflowType.Name &&
                         hev.WorkflowType.Version = StartChildWorkflowExecution.WorkflowType.Version -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy
                                                            |> should equal StartChildWorkflowExecution.ChildPolicy
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Control
                                                            |> should equal StartChildWorkflowExecution.Control
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input
                                                            |> should equal StartChildWorkflowExecution.Input
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole
                                                            |> should equal StartChildWorkflowExecution.LambdaRole
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId
                                                            |> should equal StartChildWorkflowExecution.WorkflowId
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name
                                                            |> should equal StartChildWorkflowExecution.WorkflowType.Name
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version
                                                            |> should equal StartChildWorkflowExecution.WorkflowType.Version

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ``Test context for RemoveFromContext using StartTimerAction``() =
        let StartTimer = 
            let decision = StartTimerDecisionAttributes()
            decision.Control <- "Control 1"
            decision.TimerId <- "Timer 1"
            decision

        let event = TimerFiredEventAttributes()
        event.TimerId <- StartTimer.TimerId
            
        let context = ExecutionContextManager()
        context.Push(StartTimer, StartTimerResult.Fired(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = StartTimerAction.Attributes(StartTimer, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | StartTimerResult.Fired(hev) 
                when hev.TimerId = event.TimerId ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | StartTimerResult.Starting(hev)
                    when hev.Control = StartTimer.Control &&
                         hev.StartToFireTimeout = StartTimer.StartToFireTimeout &&
                         hev.TimerId = StartTimer.TimerId -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                    resp.Decisions.[0].StartTimerDecisionAttributes.Control
                                                            |> should equal StartTimer.Control
                    resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                            |> should equal StartTimer.StartToFireTimeout
                    resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                            |> should equal StartTimer.TimerId

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ``Test context for RemoveFromContext using WorkflowExecutionSignaledAction``() =
        let WorkflowExecutionSignaled = 
            let signalName = "Test Signal"
            signalName

        let event = WorkflowExecutionSignaledEventAttributes()
        event.SignalName <- WorkflowExecutionSignaled
        event.Input <- "Signal Input"
            
        let context = ExecutionContextManager()
        context.Push(WorkflowExecutionSignaled, WorkflowExecutionSignaledResult.Signaled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = WorkflowExecutionSignaledAction.Attributes(WorkflowExecutionSignaled, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | WorkflowExecutionSignaledResult.Signaled(hev) 
                when hev.SignalName = event.SignalName &&
                     hev.Input = event.Input ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | WorkflowExecutionSignaledResult.NotSignaled -> return "TEST PASS"
                | _ -> return "TEST FAIL"

            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()


    let ``Test context for RemoveFromContext using SignalExternalWorkflowExecutionAction``() =
        let SignalExternalWorkflowExecution = 
            let decision = SignalExternalWorkflowExecutionDecisionAttributes()
            decision.Control <- "Control 1"
            decision.Input <- "Signal Input"
            decision.RunId <- "Signal RunId"
            decision.SignalName <- "Signal Name"
            decision.WorkflowId <- "Signal WorkflowId"
            decision

        let event = ExternalWorkflowExecutionSignaledEventAttributes()
        event.WorkflowExecution <- WorkflowExecution(RunId=SignalExternalWorkflowExecution.RunId, WorkflowId=SignalExternalWorkflowExecution.WorkflowId)
            
        let context = ExecutionContextManager()
        context.Push(SignalExternalWorkflowExecution, SignalExternalWorkflowExecutionResult.Signaled(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = SignalExternalWorkflowExecutionAction.Attributes(SignalExternalWorkflowExecution, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | SignalExternalWorkflowExecutionResult.Signaled(hev) 
                when hev.WorkflowExecution.RunId = event.WorkflowExecution.RunId &&
                     hev.WorkflowExecution.WorkflowId = event.WorkflowExecution.WorkflowId ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | SignalExternalWorkflowExecutionResult.Signaling -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.SignalExternalWorkflowExecution
                    resp.Decisions.[0].SignalExternalWorkflowExecutionDecisionAttributes.Control
                                                            |> should equal SignalExternalWorkflowExecution.Control
                    resp.Decisions.[0].SignalExternalWorkflowExecutionDecisionAttributes.Input
                                                            |> should equal SignalExternalWorkflowExecution.Input
                    resp.Decisions.[0].SignalExternalWorkflowExecutionDecisionAttributes.RunId
                                                            |> should equal SignalExternalWorkflowExecution.RunId
                    resp.Decisions.[0].SignalExternalWorkflowExecutionDecisionAttributes.SignalName
                                                            |> should equal SignalExternalWorkflowExecution.SignalName
                    resp.Decisions.[0].SignalExternalWorkflowExecutionDecisionAttributes.WorkflowId
                                                            |> should equal SignalExternalWorkflowExecution.WorkflowId

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ``Test context for RemoveFromContext using RecordMarkerAction``() =
        let RecordMarker = 
            let decision = RecordMarkerDecisionAttributes()
            decision.Details <- "Record Marker Details"
            decision.MarkerName <- "Marker Name"
            decision

        let event = MarkerRecordedEventAttributes()
        event.Details <- RecordMarker.Details
        event.MarkerName <- RecordMarker.Details
            
        let context = ExecutionContextManager()
        context.Push(RecordMarker, RecordMarkerResult.Recorded(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = RecordMarkerAction.Attributes(RecordMarker, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | RecordMarkerResult.Recorded(hev) 
                when hev.Details = event.Details &&
                     hev.MarkerName = event.MarkerName ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | RecordMarkerResult.Recording -> return "TEST PASS"

                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.RecordMarker
                    resp.Decisions.[0].RecordMarkerDecisionAttributes.Details
                                                            |> should equal RecordMarker.Details
                    resp.Decisions.[0].RecordMarkerDecisionAttributes.MarkerName
                                                            |> should equal RecordMarker.MarkerName

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()

    let ``Test context for RemoveFromContext using MarkerRecordedAction``() =
        let MarkerRecorded = 
            let markerName = "Test Marner"
            markerName

        let event = MarkerRecordedEventAttributes()
        event.Details <- "Record Marker Details"
        event.MarkerName <- MarkerRecorded
            
        let context = ExecutionContextManager()
        context.Push(MarkerRecorded, MarkerRecordedResult.Recorded(event))
        let contextString = context.Write()

        let context = ExecutionContextManager()
        context.Read(contextString)

        let action = MarkerRecordedAction.Attributes(MarkerRecorded, true)
            
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder, Some(context :> IContextManager)) {
            
            let! result = action

            match result with
            | MarkerRecordedResult.Recorded(hev) 
                when hev.Details = event.Details &&
                     hev.MarkerName = event.MarkerName ->
                
                do! FlowSharp.RemoveFromContext(action)
                
                let! newresult = action

                match newresult with
                | MarkerRecordedResult.NotRecorded -> return "TEST PASS"
                | _ -> return "TEST FAIL"

            | _ -> return "TEST FAIL"
        }

        if TestConfiguration.IsConnected then
            // Only offline mode is supported
            ()
        else
    
            let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                |> OfflineHistoryEvent (        // EventId = 1
                                    WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                |> OfflineHistoryEvent (        // EventId = 2
                                    DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                |> OfflineHistoryEvent (        // EventId = 3
                                    DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                            |> should equal "TEST PASS"
                | _ -> ()
