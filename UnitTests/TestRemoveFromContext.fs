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

