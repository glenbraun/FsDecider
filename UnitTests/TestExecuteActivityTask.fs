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

module TestExecuteActivityTask =

    let Foo() = 
        let gg = FlowSharp.Builder(new DecisionTask()) {
                return "OK"
            }

        ()


    let ``Execute Activity Task with One Completed Activity Task``() =
        let workflowId = "Execute Activity Task with One Completed Activity Task"
        let activityId = "Test Activity 1"
        let input = "Test Activity 1 Input"
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start and Wait for an Activity Task
            let! result = FlowSharp.ExecuteActivityTask (
                            TestConfiguration.TestActivityType, 
                            input=input,
                            activityId=activityId, 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            match result with
            | ExecuteActivityTaskResult.Completed(taskresult) -> return "TEST PASS"
            | _ -> ()
            return "TEST FAIL"            
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (WorkflowType(Name="Workflow1", Version="1")) (WorkflowExecution(RunId="23bCyBdBGdLPKwjr92SnGyQJKeA8O9C9mXg8vpWTKrlQg=", WorkflowId = workflowId))
                            |> OfflineHistoryEvent (        // EventId = 1
                                WorkflowExecutionStartedEventAttributes(
                                    ChildPolicy=ChildPolicy.TERMINATE,
                                    ExecutionStartToCloseTimeout="1200",
                                    Input="Workflow input, 1/2/2017 8:06:05 PM",
                                    LambdaRole="arn:aws:iam::538386600280:role/swf-lambda",
                                    TaskList=TaskList(Name="main"),
                                    TaskStartToCloseTimeout="1200",
                                    WorkflowType=WorkflowType(Name="Workflow1", Version="1")))

                            |> OfflineHistoryEvent (        // EventId = 2
                                DecisionTaskScheduledEventAttributes(
                                    StartToCloseTimeout="1200",
                                    TaskList=TaskList(Name="main")))

                            |> OfflineHistoryEvent (        // EventId = 3
                                DecisionTaskStartedEventAttributes(
                                    Identity="DeciderTests",
                                    ScheduledEventId=2L))

                            |> OfflineHistoryEvent (        // EventId = 4
                                DecisionTaskCompletedEventAttributes(
                                    ScheduledEventId=2L,
                                    StartedEventId=3L))

                            |> OfflineHistoryEvent (        // EventId = 5
                                ActivityTaskScheduledEventAttributes(
                                    ActivityId="772a9e72-dbb3-427d-a59a-ed516e3ecb74",
                                    ActivityType=ActivityType(Name="Activity1", Version="2"),
                                    Control="1",
                                    DecisionTaskCompletedEventId=4L,
                                    HeartbeatTimeout="1200",
                                    Input="Activity1 input, 1/2/2017 8:06:05 PM",
                                    ScheduleToCloseTimeout="1200",
                                    ScheduleToStartTimeout="1200",
                                    StartToCloseTimeout="1200",
                                    TaskList=TaskList(Name="main")))

                            |> OfflineHistoryEvent (        // EventId = 6
                                ActivityTaskStartedEventAttributes(
                                    Identity="DeciderTests",
                                    ScheduledEventId=5L))

                            |> OfflineHistoryEvent (        // EventId = 7
                                ActivityTaskCompletedEventAttributes(
                                    Result="Activity1 result, 1/2/2017 8:06:05 PM",
                                    ScheduledEventId=5L,
                                    StartedEventId=6L))

                            |> OfflineHistoryEvent (        // EventId = 8
                                DecisionTaskScheduledEventAttributes(
                                    StartToCloseTimeout="1200",
                                    TaskList=TaskList(Name="main")))

                            |> OfflineHistoryEvent (        // EventId = 9
                                DecisionTaskStartedEventAttributes(
                                    Identity="DeciderTests",
                                    ScheduledEventId=8L))

                            |> OfflineHistoryEvent (        // EventId = 10
                                DecisionTaskCompletedEventAttributes(
                                    ScheduledEventId=8L,
                                    StartedEventId=9L))

                            |> OfflineHistoryEvent (        // EventId = 11
                                WorkflowExecutionCompletedEventAttributes(
                                    DecisionTaskCompletedEventId=10L,
                                    Result="Activity1 result, 1/2/2017 8:06:05 PM"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide deciderFunc offlineFunc 2 do
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
                                                        |> should equal input

                TestHelper.RespondDecisionTaskCompleted resp
                TestHelper.PollAndCompleteActivityTask (TestConfiguration.TestActivityType) None                
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        