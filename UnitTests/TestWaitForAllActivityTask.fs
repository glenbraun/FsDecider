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

module TestWaitForAllActivityTask =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.WorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.LambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TaskList"
        |> Map.add "Identity" "TestConfiguration.Identity"
        |> Map.add "ActivityType" "TestConfiguration.ActivityType"
        |> Map.add "ActivityTaskCompleted.Result" "activityResult"


    let ``Wait For All Activity Task with All Completed Activity Tasks``() =
        let workflowId = "Wait For All Activity Task with All Completed Activity Tasks"
        let activityId1 = "Test Activity 1"
        let activityId2 = "Test Activity 2"
        let activityResult = "Test Activity Result"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity1 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId1, 
                                input="1",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let! activity2 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId2, 
                                input="2",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let activityList = [activity1; activity2;]

            // Wait for All Activity Task
            do! FlowSharpAction.WaitForAllActivityTask(activityList)

            let finishedList = 
                activityList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleActivityTaskResult.Completed(attr1);  ScheduleActivityTaskResult.Completed(attr2)] when attr1.Result = activityResult && attr2.Result = activityResult -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.LambdaRole, TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 1", ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="1", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 2", ActivityType=TestConfiguration.ActivityType, Control="2", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="2", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=5L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 10
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=6L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=9L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))
        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId1
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "1"

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId2
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "2"

                TestHelper.RespondDecisionTaskCompleted resp
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For All Activity Task with One Completed Activity Tasks``() =
        let workflowId = "Wait For All Activity Task with One Completed Activity Tasks"
        let activityId1 = "Test Activity 1"
        let activityId2 = "Test Activity 2"
        let activityResult = "Test Activity Result"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity1 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId1, 
                                input="1",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let! activity2 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId2, 
                                input="2",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let activityList = [activity1; activity2;]

            // Wait for All Activity Task
            do! FlowSharpAction.WaitForAllActivityTask(activityList)

            let finishedList = 
                activityList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleActivityTaskResult.Completed(attr1);  ScheduleActivityTaskResult.Completed(attr2)] when attr1.Result = activityResult && attr2.Result = activityResult -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.LambdaRole, TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 1", ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="1", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 2", ActivityType=TestConfiguration.ActivityType, Control="2", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="2", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=9L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=5L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=14L, StartedEventId=15L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=16L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId1
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "1"

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId2
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "2"

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))

            | 2 -> 
                resp.Decisions.Count                    |> should equal 0

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))
                
            | 3 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For All Activity Task with No Completed Activity Tasks``() =
        let workflowId = "Wait For All Activity Task with No Completed Activity Tasks"
        let activityId1 = "Test Activity 1"
        let activityId2 = "Test Activity 2"
        let activityResult = "Test Activity Result"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! activity1 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId1, 
                                input="1",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let! activity2 = FlowSharpAction.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                activityId2, 
                                input="2",
                                taskList=TestConfiguration.TaskList, 
                                heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                            )

            let activityList = [activity1; activity2;]

            // Wait for All Activity Task
            do! FlowSharpAction.WaitForAllActivityTask(activityList)

            let finishedList = 
                activityList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleActivityTaskResult.Completed(attr1);  ScheduleActivityTaskResult.Completed(attr2)] when attr1.Result = activityResult && attr2.Result = activityResult -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.LambdaRole, TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 1", ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="1", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 2", ActivityType=TestConfiguration.ActivityType, Control="2", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="2", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=6L, StartedEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              ActivityTaskCompletedEventAttributes(Result="Test Activity Result", ScheduledEventId=5L, StartedEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=16L))
                          |> OfflineHistoryEvent (        // EventId = 18
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=17L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId1
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "1"

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId2
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "2"

                TestHelper.RespondDecisionTaskCompleted resp

                // Send a signal to generate a decision task
                TestHelper.SignalWorkflow runId workflowId "Test Signal" ""

            | 2 -> 
                resp.Decisions.Count                    |> should equal 0

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.ActivityType) (Some(fun _ -> activityResult))
                
            | 3 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

