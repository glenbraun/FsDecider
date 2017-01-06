namespace FlowSharp.UnitTests

open FlowSharp
open FlowSharp.UnitTests.TestHelper
open FlowSharp.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestRequestCancelActivityTask =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "ActivityId" "activityId"
        |> Map.add "ActivityType" "TestConfiguration.TestActivityType"
        |> Map.add "ActivityTaskScheduledEventAttributes.Input" "activityInput"
        |> Map.add "ActivityTaskCompleted.Result" "activityResult"
        |> Map.add "ActivityTaskCanceled.Details" "activityDetails"


    let ``Request Cancel Activity Task with result of Canceled``() =
        let workflowId = "Request Cancel Activity Task with result of Canceled"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Signal for RequestCancelActivityTask"
        let mutable taskToken = ""
        let cancelDetails = "Activity Task Canceled Details"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.Canceled(attr) 
                when attr.Details = cancelDetails -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskCancelRequestedEventAttributes(ActivityId=activityId, DecisionTaskCompletedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ActivityTaskCanceledEventAttributes(Details="Activity Task Canceled Details", LatestCancelRequestedEventId=11L, ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=15L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                taskToken <- TestHelper.PollAndStartActivityTask (TestConfiguration.TestActivityType)

                // Send a signal to force a decider task
                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.RequestCancelActivityTask
                resp.Decisions.[0].RequestCancelActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                
                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.HeartbeatAndCancelActivityTask taskToken cancelDetails

            | 3 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Request Cancel Activity Task with result of Completed``() =
        let workflowId = "Request Cancel Activity Task with result of Completed"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Signal for RequestCancelActivityTask"
        let completedResult = "Activity Task Completed"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.Completed(attr) 
                when attr.Result = completedResult -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskCompletedEventAttributes(Result="Activity Task Completed", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=11L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(completedResult))

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Request Cancel Activity Task with result of Failed``() =
        let workflowId = "Request Cancel Activity Task with result of Failed"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Signal for RequestCancelActivityTask"
        let reason = "Activity Task Failed Reason"
        let details = "Failed details"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.Failed(attr) 
                when attr.Reason = reason && attr.Details = details -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskFailedEventAttributes(Details="Failed details", Reason="Activity Task Failed Reason", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=11L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndFailActivityTask (TestConfiguration.TestActivityType) (Some(reason)) (Some(details))

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Request Cancel Activity Task with result of TimedOut``() =
        let workflowId = "Request Cancel Activity Task with result of TimedOut"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Signal for RequestCancelActivityTask"
        let timeoutType = ActivityTaskTimeoutType.SCHEDULE_TO_START
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=5u, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.TimedOut(attr) 
                when attr.TimeoutType = timeoutType && attr.Details = null -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="5", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskTimedOutEventAttributes(ScheduledEventId=5L, TimeoutType=ActivityTaskTimeoutType.SCHEDULE_TO_START))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp
                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleeping to wait for activity task to timeout.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(6.0)) // Sleep one second longer than the schedule to start timeout

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Request Cancel Activity Task with result of CancelRequested``() =
        let workflowId = "Request Cancel Activity Task with result of CancelRequested"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Signal for RequestCancelActivityTask"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.CancelRequested -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskCancelRequestedEventAttributes(ActivityId=activityId, DecisionTaskCompletedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=15L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndStartActivityTask (TestConfiguration.TestActivityType) |> ignore

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.RequestCancelActivityTask
                resp.Decisions.[0].RequestCancelActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                
                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 3 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Request Cancel Activity Task with result of ScheduleFailed``() =
        let workflowId = "Request Cancel Activity Task with result of ScheduleFailed"
        let activityType = ActivityType(Name="ActivityDoesNotExist_7A52FAEF-3EF9-41E6-8ED7-412309755C79", Version="1")
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityCause = ScheduleActivityTaskFailedCause.ACTIVITY_TYPE_DOES_NOT_EXIST
        let signalName = "Signal for RequestCancelActivityTask"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            activityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            let! cancel = FlowSharp.RequestCancelActivityTask(start)

            match cancel with
            | RequestCancelActivityTaskResult.ScheduleFailed(attr) 
                when attr.ActivityId = activityId &&
                     attr.ActivityType.Name = activityType.Name &&
                     attr.ActivityType.Version = activityType.Version &&
                     attr.Cause = activityCause -> return "TEST PASS"
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
                              ScheduleActivityTaskFailedEventAttributes(ActivityId=activityId, ActivityType=activityType, Cause=ScheduleActivityTaskFailedCause.ACTIVITY_TYPE_DOES_NOT_EXIST, DecisionTaskCompletedEventId=4L))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal activityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal activityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Remove("ActivityType").Add("ActivityType", "activityType"))

    let ``Request Cancel Activity Task with result of RequestCancelFailed``() =
        let workflowId = "Request Cancel Activity Task with result of RequestCancelFailed"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let cancelCause = RequestCancelActivityTaskFailedCause.ACTIVITY_ID_UNKNOWN
        let signalName = "Signal for RequestCancelActivityTask"
        let fakeActivityId = activityId + "_DoesNotExist_E8F98536-F45F-4D3B-BA86-8EA9CAF5D674"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! start = FlowSharp.StartActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=activityInput,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )
            
            let! wait = FlowSharp.WaitForWorkflowExecutionSignaled(signalName)

            // Improper usage here but requried to force RequestCancelFailed 
            let fakeStart = 
                match start with
                | StartActivityTaskResult.Started(attr, at, control, id) -> StartActivityTaskResult.Started(attr, at, control, fakeActivityId)
                | s -> s

            let! cancel = FlowSharp.RequestCancelActivityTask(fakeStart)

            match cancel with
            | RequestCancelActivityTaskResult.RequestCancelFailed(attr) 
                when attr.ActivityId = fakeActivityId &&
                     attr.Cause = cancelCause -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="Signal Input", SignalName="Signal for RequestCancelActivityTask"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              RequestCancelActivityTaskFailedEventAttributes(ActivityId=fakeActivityId, Cause=RequestCancelActivityTaskFailedCause.ACTIVITY_ID_UNKNOWN, DecisionTaskCompletedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=12L, StartedEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=14L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndStartActivityTask (TestConfiguration.TestActivityType) |> ignore

                TestHelper.SignalWorkflow runId workflowId signalName "Signal Input"

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.RequestCancelActivityTask
                resp.Decisions.[0].RequestCancelActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal fakeActivityId
                
                TestHelper.RespondDecisionTaskCompleted resp

            | 3 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Add("RequestCancelActivityTaskFailedEventAttributes.ActivityId", "fakeActivityId"))
