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

module TestForLoop =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "ActivityType" "TestConfiguration.TestActivityType"

    let ``A For To Loop with an empty body expression which results in Unit``() =
        let workflowId = "A For To Loop with an empty body expression which results in Unit"

        let deciderFunc(dt:DecisionTask) =
            let sum = ref 0

            FlowSharp.Builder(dt) {
                for i = 1 to 2 do
                    sum := !sum + i
                    ()

                if !sum = 3 then
                    return "TEST PASS"
                else 
                    return "TEST FAIL"
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
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``A For In Loop with an empty body expression which results in Unit``() =
        let workflowId = "A For In Loop with an empty body expression which results in Unit"

        let deciderFunc(dt:DecisionTask) =
            let sum = ref 0

            FlowSharp.Builder(dt) {
                for i in [1 .. 2] do
                    sum := !sum + i
                    ()

                if !sum = 3 then
                    return "TEST PASS"
                else 
                    return "TEST FAIL"
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
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``A For To Loop with a body that Starts and Waits for an Activity Task with unique results per iteration``() =
        let workflowId = "A For To Loop with a body that Starts and Waits for an Activity Task with unique results per iteration"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"

        let deciderFunc(dt:DecisionTask) =

            FlowSharp.Builder(dt) {
                for i = 1 to 2 do
                    // Schedule and Wait for an Activity Task
                    let! result = FlowSharp.ScheduleAndWaitForActivityTask (
                                    TestConfiguration.TestActivityType, 
                                    activityId, 
                                    input=activityInput,
                                    taskList=TestConfiguration.TestTaskList, 
                                    heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                                )

                    match result with
                    | WaitForActivityTaskResult.Completed(attr) when attr.Result = activityResult + (i.ToString()) -> ()
                    | _ -> return "TEST FAIL"

                return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 1", ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="Test Activity 1 Input", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result1", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 1", ActivityType=TestConfiguration.TestActivityType, Control="2", DecisionTaskCompletedEventId=10L, HeartbeatTimeout="1200", Input="Test Activity 1 Input", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result2", ScheduledEventId=11L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=14L, StartedEventId=15L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=16L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

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
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(fun _ -> activityResult + "1"))

            | 2 -> 
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
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(fun _ -> activityResult + "2"))

            | 3 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``A For In Loop with a body that Starts an Activity Task with unique results per iteration``() =
        let workflowId = "A For In Loop with a body that Starts an Activity Task with unique results per iteration"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"

        let deciderFunc(dt:DecisionTask) =

            FlowSharp.Builder(dt) {
                for i in [ 1 .. 2 ] do
                    // Schedule an Activity Task
                    let! start = FlowSharp.ScheduleActivityTask (
                                    TestConfiguration.TestActivityType, 
                                    activityId + (i.ToString()), 
                                    input=(i.ToString()),
                                    taskList=TestConfiguration.TestTaskList, 
                                    heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                                    startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                                )

                    match start with
                    | ScheduleActivityTaskResult.Scheduling(_, _) -> if i = 2 then return () else ()
                    | ScheduleActivityTaskResult.Scheduled(_) -> if i = 2 then return () else ()
                    | ScheduleActivityTaskResult.Started(attr, t, c, id) ->
                        let! result = FlowSharp.WaitForActivityTask(start)

                        match result with 
                        | WaitForActivityTaskResult.Completed(attr) when attr.Result = activityResult + (i.ToString()) -> 
                            if i = 2 then return "TEST PASS" else ()
                        | _ -> return "TEST FAIL"

                    | _ -> return "TEST FAIL"

                return "TEST FAIL"
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
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 11", ActivityType=TestConfiguration.TestActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="1", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskScheduledEventAttributes(ActivityId="Test Activity 12", ActivityType=TestConfiguration.TestActivityType, Control="2", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input="2", ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result1", ScheduledEventId=5L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 10
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result2", ScheduledEventId=6L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=9L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal (activityId + "1")
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "1"

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal (activityId + "2")
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.TestActivityType.Name
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.TestActivityType.Version
                resp.Decisions.[1].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal "2"

                TestHelper.RespondDecisionTaskCompleted resp
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(fun (at:ActivityTask) -> activityResult + at.Input))
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) (Some(fun (at:ActivityTask) -> activityResult + at.Input))

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
