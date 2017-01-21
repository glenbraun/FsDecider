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

module TestWaitForActivityTask =
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


    let ``Wait For Activity Task with One Completed Activity Task``() =
        let workflowId = "Wait For Activity Task with One Completed Activity Task"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.TestActivityType, 
                                activityId, 
                                input=activityInput,
                                taskList=TestConfiguration.TestTaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            // Wait for Activity Task
            do! FlowSharp.WaitForActivityTask(activity)

            match activity with
            | ScheduleActivityTaskResult.Completed(attr) when attr.Result = activityResult -> return "TEST PASS"
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
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
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
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(fun _ -> activityResult))
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For Activity Task with One Canceled Activity Task``() =
        let workflowId = "Wait For Activity Task with One Canceled Activity Task"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityDetails = "Test Activity 1 Canceled Details"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.TestActivityType, 
                                activityId, 
                                input=activityInput,
                                taskList=TestConfiguration.TestTaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            // Wait for Activity Task
            do! FlowSharp.WaitForActivityTask(activity)

            match activity with
            | ScheduleActivityTaskResult.Canceled(attr) when attr.Details = activityDetails -> return "TEST PASS"
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
                              ActivityTaskCanceledEventAttributes(Details="Test Activity 1 Canceled Details", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
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
                TestHelper.PollAndCancelActivityTask (TestConfiguration.TestActivityType) (Some(activityDetails))
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For Activity Task with One Failed Activity Task``() =
        let workflowId = "Wait For Activity Task with One Failed Activity Task"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityReason = "Test Activity 1 Failed Reason"
        let activityDetails = "Test Activity 1 Failed Details"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.TestActivityType, 
                                activityId, 
                                input=activityInput,
                                taskList=TestConfiguration.TestTaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            // Wait for Activity Task
            do! FlowSharp.WaitForActivityTask(activity)

            match activity with
            | ScheduleActivityTaskResult.Failed(attr) when attr.Reason = activityReason && attr.Details = activityDetails -> return "TEST PASS"
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
                              ActivityTaskFailedEventAttributes(Details="Test Activity 1 Failed Details", Reason="Test Activity 1 Failed Reason", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
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
                TestHelper.PollAndFailActivityTask (TestConfiguration.TestActivityType) (Some(activityReason)) (Some(activityDetails))
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For Activity Task with One Timed Out Activity Task``() =
        let workflowId = "Wait For Activity Task with One Timed Out Activity Task"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityTimeoutType = ActivityTaskTimeoutType.SCHEDULE_TO_START
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.TestActivityType, 
                                activityId, 
                                input=activityInput,
                                taskList=TestConfiguration.TestTaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout="5", // set to 5 second timeout
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            do! FlowSharp.WaitForActivityTask(activity)

            match activity with
            | ScheduleActivityTaskResult.TimedOut(attr) 
                when attr.TimeoutType = ActivityTaskTimeoutType.SCHEDULE_TO_START &&
                     attr.Details = null -> return "TEST PASS"
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
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
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
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For Activity Task with Activity Task Schedule Failure``() =
        let workflowId = "Wait For Activity Task with Activity Task Schedule Failure"
        let activityType = ActivityType(Name="ActivityDoesNotExist_3F0F9E3C-2DDB-4480-81FE-3041BA10231A", Version="1")
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityCause = ScheduleActivityTaskFailedCause.ACTIVITY_TYPE_DOES_NOT_EXIST
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule an Activity Task
            let! activity = FlowSharp.ScheduleActivityTask (
                                activityType, 
                                activityId, 
                                input=activityInput,
                                taskList=TestConfiguration.TestTaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout,
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            do! FlowSharp.WaitForActivityTask(activity)

            match activity with
            | ScheduleActivityTaskResult.ScheduleFailed(attr) 
                when attr.Cause = activityCause &&
                     attr.ActivityId = activityId &&
                     attr.ActivityType.Name = activityType.Name &&
                     attr.ActivityType.Version = activityType.Version -> return "TEST PASS"
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
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
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
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Remove("ActivityType").Add("ActivityType", "activityType"))
