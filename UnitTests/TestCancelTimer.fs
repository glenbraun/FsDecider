namespace FlowSharp.UnitTests

open FlowSharp
open FlowSharp.Actions
open FlowSharp.UnitTests.TestHelper
open FlowSharp.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestCancelTimer =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "TimerId" "timerId"

    let ``Cancel Timer with result of Canceled``() =
        let workflowId = "Cancel Timer with result of Canceled"
        let signalName = "Test Signal"
        let timerId = "timer1"
        let startToFireTimeout = TimeSpan.FromDays(100.0).TotalSeconds.ToString()

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Timer
            let! timer1 = FlowSharp.StartTimer (timerId=timerId, startToFireTimeout = startToFireTimeout)

            match timer1 with
            | StartTimerResult.Starting(_) ->
                return ()

            | StartTimerResult.Started(attr) when attr.TimerId = timerId -> 
                let! wait = FlowSharp.CancelTimer(timer1)

                match wait with
                | CancelTimerResult.Canceling(_) ->
                    return ()

                | _ -> return "TEST FAIL" 

            | StartTimerResult.Canceled(attr) when attr.TimerId = timerId -> return "TEST PASS" 
                
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              TimerStartedEventAttributes(Control="1", DecisionTaskCompletedEventId=4L, StartToFireTimeout="8640000", TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              TimerCanceledEventAttributes(DecisionTaskCompletedEventId=9L, StartedEventId=5L, TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=12L, StartedEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=14L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                        |> should equal timerId
                resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                        |> should equal (startToFireTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                // Send a signal to trigger a decision task
                TestHelper.SignalWorkflow runId workflowId "Test Signal" ""

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CancelTimer
                resp.Decisions.[0].CancelTimerDecisionAttributes.TimerId
                                                        |> should equal timerId
                
                TestHelper.RespondDecisionTaskCompleted resp

                // Send a signal to trigger a decision task
                TestHelper.SignalWorkflow runId workflowId "Test Signal" ""

            | 3 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Cancel Timer with result of Canceling``() =
        let workflowId = "Cancel Timer with result of Canceling"
        let signalName = "Test Signal"
        let timerId = "timer1"
        let startToFireTimeout = TimeSpan.FromDays(100.0).TotalSeconds.ToString()

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Timer
            let! timer1 = FlowSharp.StartTimer (timerId=timerId, startToFireTimeout = startToFireTimeout)

            match timer1 with
            | StartTimerResult.Starting(_) ->
                return ()

            | StartTimerResult.Started(attr) when attr.TimerId = timerId && attr.StartToFireTimeout = (startToFireTimeout.ToString()) -> 
                let! cancel = FlowSharp.CancelTimer(timer1)

                match cancel with
                | CancelTimerResult.Canceling -> 
                    return "TEST PASS"

                | _ -> return "TEST FAIL"

            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              TimerStartedEventAttributes(Control="1", DecisionTaskCompletedEventId=4L, StartToFireTimeout="8640000", TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName=signalName))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              TimerCanceledEventAttributes(DecisionTaskCompletedEventId=9L, StartedEventId=5L, TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                        |> should equal timerId
                resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                        |> should equal (startToFireTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.SignalWorkflow runId workflowId signalName ""
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CancelTimer
                resp.Decisions.[0].CancelTimerDecisionAttributes.TimerId 
                                                        |> should equal timerId
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Add("SignalName", "signalName"))

    let ``Cancel Timer with result of Fired``() =
        let workflowId = "Cancel Timer with result of Fired"
        let timerId = "timer1"
        let startToFireTimeout = "2"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Timer
            let! timer1 = FlowSharp.StartTimer(timerId=timerId, startToFireTimeout = startToFireTimeout)

            let! wait = FlowSharp.WaitForTimer(timer1)

            let! cancel = FlowSharp.CancelTimer(timer1)

            match cancel with
            | CancelTimerResult.Fired(attr) when attr.TimerId = timerId -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              TimerStartedEventAttributes(Control="1", DecisionTaskCompletedEventId=4L, StartToFireTimeout="2", TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 6
                              TimerFiredEventAttributes(StartedEventId=5L, TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                        |> should equal timerId
                resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                        |> should equal (startToFireTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Cancel Timer with result of StartTimerFailed``() =
        let workflowId = "Cancel Timer with result of StartTimerFailed"
        let signalName = "Test Signal"
        let timerId = "timer1"
        let cause = StartTimerFailedCause.TIMER_ID_ALREADY_IN_USE
        let startToFireTimeout = TimeSpan.FromDays(100.0).TotalSeconds.ToString()

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Timer
            let! timer1 = FlowSharp.StartTimer(timerId=timerId, startToFireTimeout = startToFireTimeout)

            match timer1 with
            | StartTimerResult.Starting(_) ->
                return ()

            | StartTimerResult.StartTimerFailed(attr) when attr.TimerId = timerId && attr.Cause = cause -> 
                let! cancel = FlowSharp.CancelTimer(timer1)
                
                match cancel with
                | CancelTimerResult.StartTimerFailed(_) -> return "TEST PASS"
                | _ -> return "TEST FAIL"
            | _ -> return "TEST FAIL"                        
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              StartTimerFailedEventAttributes(Cause=StartTimerFailedCause.TIMER_ID_ALREADY_IN_USE, DecisionTaskCompletedEventId=9L, TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        if TestConfiguration.IsConnected then
            // Only offline supported for this test
            ()
        else
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                    resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                            |> should equal timerId
                    resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                            |> should equal (startToFireTimeout.ToString())

                    TestHelper.RespondDecisionTaskCompleted resp

                | 2 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp
                | _ -> ()

    let ``Cancel Timer with result of CancelTimerFailed``() =
        let workflowId = "Cancel Timer with result of CancelTimerFailed"
        let signalName = "Test Signal"
        let timerId = "timer1"
        let fakeTimerId = "fakeTimer_3CBD02C6-6B59-4787-B935-9AFAA6268457"
        let cause = CancelTimerFailedCause.TIMER_ID_UNKNOWN
        let startToFireTimeout = TimeSpan.FromDays(100.0).TotalSeconds.ToString()

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Timer
            let! timer1 = FlowSharp.StartTimer (timerId=timerId, startToFireTimeout = startToFireTimeout)

            match timer1 with
            | StartTimerResult.Starting(_) ->
                return ()

            | StartTimerResult.Started(attr) when attr.TimerId = timerId && attr.StartToFireTimeout = (startToFireTimeout.ToString()) -> 
                let! cancel = FlowSharp.CancelTimer(timer1)

                match cancel with
                | CancelTimerResult.Canceling -> 
                    attr.TimerId <- fakeTimerId
                    let! cancel2 = FlowSharp.CancelTimer(timer1)
                    attr.TimerId <- timerId

                    match cancel2 with
                    | CancelTimerResult.Canceling ->
                        let! wait2 = FlowSharp.WaitForTimer(timer1)
                        ()
                    | CancelTimerResult.CancelTimerFailed(attr) when attr.TimerId = fakeTimerId && attr.Cause = cause -> 
                        return "TEST PASS"

                    | _ -> return "TEST FAIL"

                | _ -> return "TEST FAIL"

            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              TimerStartedEventAttributes(Control="1", DecisionTaskCompletedEventId=4L, StartToFireTimeout="8640000", TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName=signalName))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              CancelTimerFailedEventAttributes(Cause=CancelTimerFailedCause.TIMER_ID_UNKNOWN, DecisionTaskCompletedEventId=9L, TimerId=fakeTimerId))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=11L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              TimerCanceledEventAttributes(DecisionTaskCompletedEventId=13L, StartedEventId=5L, TimerId=timerId))
                          |> OfflineHistoryEvent (        // EventId = 15
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartTimer
                resp.Decisions.[0].StartTimerDecisionAttributes.TimerId
                                                        |> should equal timerId
                resp.Decisions.[0].StartTimerDecisionAttributes.StartToFireTimeout
                                                        |> should equal (startToFireTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.SignalWorkflow runId workflowId signalName ""
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CancelTimer
                resp.Decisions.[0].CancelTimerDecisionAttributes.TimerId 
                                                        |> should equal timerId
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CancelTimer
                resp.Decisions.[1].CancelTimerDecisionAttributes.TimerId 
                                                        |> should equal fakeTimerId

                // Interntionally change decsion to an unknown timer id to force test condition
                resp.Decisions.RemoveAt(0)

                TestHelper.RespondDecisionTaskCompleted resp

            | 3 ->
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CancelTimer
                resp.Decisions.[0].CancelTimerDecisionAttributes.TimerId 
                                                        |> should equal timerId
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Add("SignalName", "signalName"))
