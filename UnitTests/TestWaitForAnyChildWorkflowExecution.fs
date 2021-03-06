﻿namespace FsDecider.UnitTests

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

module TestWaitForAnyChildWorkflowExecution =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.WorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.LambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TaskList"
        |> Map.add "Identity" "TestConfiguration.Identity"
        |> Map.add "SignalName" "signalName"
        |> Map.add "ParentWorkflowExecution" "WorkflowExecution(RunId=\"Offline RunId\", WorkflowId=workflowId)"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.TaskList" "childTaskList"

    let ``Wait For Any Child Workflow Execution with All Completed Child Workflow Execution``() =
        let workflowId = "Wait For Any Child Workflow Execution with All Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for Any Child Workflow Execution
            do! FsDeciderAction.WaitForAnyChildWorkflowExecution(childList)

            let finishedList = 
                childList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ StartChildWorkflowExecutionResult.Completed(attr1);  StartChildWorkflowExecutionResult.Completed(attr2)] when attr1.Result = childResult && attr2.Result = childResult -> 
                childRunId := attr1.WorkflowExecution.RunId

                return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        let childDeciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId="Child of Wait For Any Child Workflow Execution with All Completed Child Workflow Execution1"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId="Child of Wait For Any Child Workflow Execution with All Completed Child Workflow Execution2"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId="Child of Wait For Any Child Workflow Execution with All Completed Child Workflow Execution1"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=6L, Result="Child Result", StartedEventId=9L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId="Child of Wait For Any Child Workflow Execution with All Completed Child Workflow Execution2"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "2")
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (twice)
                for k = 1 to 2 do 
                    for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc false 1 do
                        match j with
                        | 1 -> 
                            childResp.Decisions.Count                    |> should equal 1
                            childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                            childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                         |> should equal childResult

                            TestHelper.RespondDecisionTaskCompleted childResp

                        | _ -> ()

                // Sleep a little to let all the history make it to the parent workflow
                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleep a little to let all the history make it to the parent workflow.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0))

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet (!childRunId) (childWorkflowId + "1") OfflineHistorySubstitutions

    let ``Wait For Any Child Workflow Execution with One Completed Child Workflow Execution``() =
        let workflowId = "Wait For Any Child Workflow Execution with One Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for Any Child Workflow Execution
            do! FsDeciderAction.WaitForAnyChildWorkflowExecution(childList)

            let finishedList = 
                childList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ StartChildWorkflowExecutionResult.Completed(attr) ] when attr.Result = childResult -> 
                childRunId := attr.WorkflowExecution.RunId

                return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        let childDeciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=12L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "2")
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (once)
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc false 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 1
                        childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                        childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                        |> should equal childResult

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

                // Sleep a little to let all the history make it to the parent workflow
                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleep a little to let all the history make it to the parent workflow.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0))

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Wait For Any Child Workflow Execution with No Completed Child Workflow Execution``() =
        let workflowId = "Wait For Any Child Workflow Execution with No Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FsDeciderAction.StartChildWorkflowExecution
                              (
                                TestConfiguration.WorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.LambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for Any Child Workflow Execution
            do! FsDeciderAction.WaitForAnyChildWorkflowExecution(childList)

            let finishedList = 
                childList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ StartChildWorkflowExecutionResult.Completed(attr) ] when attr.Result = childResult -> 
                childRunId := attr.WorkflowExecution.RunId

                return "TEST PASS"
            | _ -> return "TEST FAIL"
        }

        let childDeciderFunc(dt:DecisionTask) =
            Decider(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=6L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=15L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "2")
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.WorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.WorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.LambdaRole
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

            | 2 -> 
                resp.Decisions.Count                    |> should equal 0

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (once)
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc false 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 1
                        childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                        childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                        |> should equal childResult

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

                // Sleep a little to let all the history make it to the parent workflow
                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleep a little to let all the history make it to the parent workflow.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0))

            | 3 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

