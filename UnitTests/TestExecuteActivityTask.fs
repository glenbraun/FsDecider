namespace FlowSharp.UnitTests

open FlowSharp
open FlowSharp.UnitTests.OfflineHistory

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open NUnit.Framework
open FsUnit

module TestExecuteActivityTask =

    let ``Execute Activity Task with One Completed Task``() =
        
        let deciderFunc(dt:DecisionTask) =
            FlowSharpDecider.create(dt) {

            // Start and Wait for an Activity Task
            let! result = FlowSharp.ExecuteActivityTask (
                            TestConfiguration.TestActivityType, 
                            input="Activity Instruction: Return Completed(OK)",
                            activityId="Test Activity 1", 
                            taskList=TestConfiguration.TestTaskList, 
                            heartbeatTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToCloseTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            scheduleToStartTimeout=TestConfiguration.TwentyMinuteTimeout, 
                            startToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                        )

            match result with
            | ExecuteActivityTaskResult.Completed(taskresult) -> return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (WorkflowType(Name="Workflow1", Version="1")) (WorkflowExecution(RunId="23bCyBdBGdLPKwjr92SnGyQJKeA8O9C9mXg8vpWTKrlQg=", WorkflowId = "676dd27c-9e4c-4500-86b3-93a5949afd0d"))
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
        //odt()
        
        (*
            for (i, resp) in Helper.PollAndDecide(deciderFunc, offlineFunc, 2)
                match i with
                | 1 -> 
                    Assert.IsTrue(resp.Decisions.Count = 1 && ((resp.Decisions.[0]).DecisionType = DecisionType.ScheduleActivityTask))
                    // Complete Activity, etc.
                    // Respond with decisions
                | 2 -> 
                    Assert...
                    // Respond with decisions
                    // Clean up
                | _ -> Fail

        //
        // Poll
        // Decide
        // Verify correct decisions
        // Respond

        *)

        ()