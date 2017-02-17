namespace FsDecider.UnitTests

open FsDecider
open FsDecider.Actions
open FsDecider.UnitTests.TestHelper
open FsDecider.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestScheduleActivityTask =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.WorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.LambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TaskList"
        |> Map.add "Identity" "TestConfiguration.Identity"
        |> Map.add "ActivityId" "activityId"
        |> Map.add "ActivityType" "TestConfiguration.ActivityType"
        |> Map.add "ActivityTaskScheduledEventAttributes.Input" "activityInput"
        |> Map.add "ActivityTaskCompleted.Result" "activityResult"
        |> Map.add "ActivityTaskCanceled.Details" "activityDetails"


    let ``Schedule Activity Task with result of Scheduling``() =
        let workflowId = "Schedule Activity Task with result of Scheduling"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            match result with
            | ScheduleActivityTaskResult.Scheduling(attr) 
                when attr.ActivityType.Name = TestConfiguration.ActivityType.Name &&
                     attr.ActivityType.Version = TestConfiguration.ActivityType.Version &&
                     attr.ActivityId=activityId -> return "TEST PASS"
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
                              WorkflowExecutionTerminatedEventAttributes(Cause=WorkflowExecutionTerminatedCause.OPERATOR_INITIATED, ChildPolicy=ChildPolicy.TERMINATE, Details="Terminated intentionally", Reason="FsDecider Unit Tests"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.TerminateWorkflow runId workflowId "FsDecider Unit Tests" "Terminated intentionally"
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Activity Task with result of Scheduled``() =
        let workflowId = "Schedule Activity Task with result of Scheduled"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Test Signal"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Start Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            let! wait = FsDeciderAction.WaitForWorkflowExecutionSignaled(signalName)

            match result with
            | ScheduleActivityTaskResult.Scheduled(attr) 
                when attr.ActivityId = activityId &&
                     attr.ActivityType.Name = TestConfiguration.ActivityType.Name &&
                     attr.ActivityType.Version = TestConfiguration.ActivityType.Version &&
                     attr.Input = activityInput -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                // Send a signal to force a decision task
                TestHelper.SignalWorkflow runId workflowId signalName ""
            
            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Activity Task with result of Started``() =
        let workflowId = "Schedule Activity Task with result of Started"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        let signalName = "Test Signal"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Start Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            let! wait = FsDeciderAction.WaitForWorkflowExecutionSignaled(signalName)

            match result with
            | ScheduleActivityTaskResult.Started(started, scheduled) 
                when started.Identity = TestConfiguration.Identity &&
                     scheduled.ActivityType.Name = TestConfiguration.ActivityType.Name &&
                     scheduled.ActivityType.Version = TestConfiguration.ActivityType.Version &&
                     scheduled.ActivityId = activityId -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                // Start the actvity task but don't complete it
                TestHelper.PollAndStartActivityTask (TestConfiguration.ActivityType) |> ignore

                // Send a signal to force a decision task
                TestHelper.SignalWorkflow runId workflowId signalName ""
            
            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Activity Task with result of Completed``() =
        let workflowId = "Schedule Activity Task with result of Completed"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            match result with
            | ScheduleActivityTaskResult.Scheduling(_) -> do! FsDeciderAction.Wait()
            | ScheduleActivityTaskResult.Completed(attr) 
                when attr.Result = activityResult -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskCompletedEventAttributes(Result="Test Activity 1 Result", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

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

    let ``Schedule Activity Task with result of Canceled``() =
        let workflowId = "Schedule Activity Task with result of Canceled"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityDetails = "Test Activity 1 Canceled Details"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )


            match result with
            | ScheduleActivityTaskResult.Scheduling(_) -> do! FsDeciderAction.Wait()
            | ScheduleActivityTaskResult.Canceled(attr) 
                when attr.Details = activityDetails -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskCanceledEventAttributes(Details="Test Activity 1 Canceled Details", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp

                TestHelper.PollAndCancelActivityTask (TestConfiguration.ActivityType) (Some(activityDetails))
            
            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Activity Task with result of TimedOut``() =
        let workflowId = "Schedule Activity Task with result of TimedOut"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityTimeoutType = ActivityTaskTimeoutType.SCHEDULE_TO_START
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout="5", 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )


            match result with
            | ScheduleActivityTaskResult.Scheduling(_) -> do! FsDeciderAction.Wait()
            | ScheduleActivityTaskResult.TimedOut(attr) 
                when attr.TimeoutType = activityTimeoutType -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="5", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskTimedOutEventAttributes(ScheduledEventId=5L, TimeoutType=ActivityTaskTimeoutType.SCHEDULE_TO_START))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
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

    let ``Schedule Activity Task with result of Failed``() =
        let workflowId = "Schedule Activity Task with result of Failed"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityReason = "Test Activity 1 Failed Reason"
        let activityDetails = "Test Activity 1 Failed Details"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )


            match result with
            | ScheduleActivityTaskResult.Scheduling(_) -> do! FsDeciderAction.Wait()
            | ScheduleActivityTaskResult.Failed(attr) when attr.Reason = activityReason && attr.Details = activityDetails -> return "TEST PASS"
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
                              ActivityTaskScheduledEventAttributes(ActivityId=activityId, ActivityType=TestConfiguration.ActivityType, Control="1", DecisionTaskCompletedEventId=4L, HeartbeatTimeout="1200", Input=activityInput, ScheduleToCloseTimeout="1200", ScheduleToStartTimeout="1200", StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ActivityTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ActivityTaskFailedEventAttributes(Details="Test Activity 1 Failed Details", Reason="Test Activity 1 Failed Reason", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput

                TestHelper.RespondDecisionTaskCompleted resp
                TestHelper.PollAndFailActivityTask (TestConfiguration.ActivityType) (Some(activityReason)) (Some(activityDetails))
            
            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Activity Task with Schedule Failure``() =
        let workflowId = "Schedule Activity Task with Schedule Failure"
        let activityType = ActivityType(Name="ActivityDoesNotExist_D8E3FFCA-E25A-4D2E-B328-584F2A387EF8", Version="1")
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityCause = ScheduleActivityTaskFailedCause.ACTIVITY_TYPE_DOES_NOT_EXIST
        let signalName = "Test Signal"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            let! result = FsDeciderAction.ScheduleActivityTask (
                            activityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout,
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            match result with
            | ScheduleActivityTaskResult.Scheduling(_) ->
                do! FsDeciderAction.Wait()

            | ScheduleActivityTaskResult.ScheduleFailed(attr) 
                when attr.Cause = activityCause &&
                     attr.ActivityId = activityId &&
                     attr.ActivityType.Name = activityType.Name &&
                     attr.ActivityType.Version = activityType.Version -> return "TEST PASS"
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
                              ScheduleActivityTaskFailedEventAttributes(ActivityId=activityId, ActivityType=activityType, Cause=ScheduleActivityTaskFailedCause.ACTIVITY_TYPE_DOES_NOT_EXIST, DecisionTaskCompletedEventId=4L))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
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


    let ``Schedule Activity Task using do!``() =
        let workflowId = "Schedule Activity Task using do!"
        let activityId = "Test Activity 1"
        let activityInput = "Test Activity 1 Input"
        let activityResult = "Test Activity 1 Result"
        
        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule Activity Task
            do! FsDeciderAction.ScheduleActivityTask (
                            TestConfiguration.ActivityType, 
                            activityId, 
                            input=activityInput,
                            taskList=TestConfiguration.TaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            return "TEST PASS"
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
                              WorkflowExecutionTerminatedEventAttributes(Cause=WorkflowExecutionTerminatedCause.OPERATOR_INITIATED, ChildPolicy=ChildPolicy.TERMINATE, Details="Terminated intentionally", Reason="FsDecider Unit Tests"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleActivityTask
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityId
                                                        |> should equal activityId
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Name 
                                                        |> should equal TestConfiguration.ActivityType.Name
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.ActivityType.Version 
                                                        |> should equal TestConfiguration.ActivityType.Version
                resp.Decisions.[0].ScheduleActivityTaskDecisionAttributes.Input  
                                                        |> should equal activityInput
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result
                                                        |> should equal "TEST PASS"

                TestHelper.TerminateWorkflow runId workflowId "FsDecider Unit Tests" "Terminated intentionally"
                
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
