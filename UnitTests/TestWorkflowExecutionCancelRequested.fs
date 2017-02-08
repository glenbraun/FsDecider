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

module TestWorkflowExecutionCancelRequested =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.WorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.LambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TaskList"
        |> Map.add "Identity" "TestConfiguration.Identity"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.TaskList" "childTaskList"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "StartChildWorkflowExecutionFailedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "ChildWorkflowExecutionStartedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution" "WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)"
        |> Map.add "ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution" "WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)"
        |> Map.add "ParentWorkflowExecution" "WorkflowExecution(RunId=\"Offline RunId\", WorkflowId=workflowId)"
        |> Map.add "ExternalWorkflowExecution" "WorkflowExecution(RunId=\"Offline RunId\", WorkflowId=workflowId)"
        
    let ``Workflow Execution Cancel Requested with result of CancelRequested``() =
        let workflowId = "Workflow Execution Cancel Requested with result of CancelRequested"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childRunId = ref ""
        let cause = WorkflowExecutionCancelRequestedCause.CHILD_POLICY_APPLIED

        let deciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {

            // Start a Child Workflow Execution
            let! start = FlowSharpAction.StartChildWorkflowExecution
                          (
                            TestConfiguration.WorkflowType,
                            childWorkflowId,
                            input=childInput,
                            childPolicy=ChildPolicy.TERMINATE,
                            lambdaRole=TestConfiguration.LambdaRole,
                            taskList=childTaskList,
                            executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                            taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                          )

            match start with
            | StartChildWorkflowExecutionResult.Starting(_) ->
                do! FlowSharpAction.Wait()
            | StartChildWorkflowExecutionResult.Initiated(_) ->
                do! FlowSharpAction.Wait()
            | StartChildWorkflowExecutionResult.Started(attr) when 
                attr.WorkflowType.Name = TestConfiguration.WorkflowType.Name &&
                attr.WorkflowType.Version = TestConfiguration.WorkflowType.Version &&
                attr.WorkflowExecution.WorkflowId = childWorkflowId -> 
                
                childRunId := attr.WorkflowExecution.RunId
                return "DONE - CHECK TEST RESULT FROM CHILD WORKFLOW EXECUTION"

            | _ -> return "TEST FAIL"
        }

        let childDeciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {
            let! cancel = FlowSharpAction.WorkflowExecutionCancelRequested()
                
            match cancel with
            | WorkflowExecutionCancelRequestedResult.NotRequested ->
                do! FlowSharpAction.Wait()

            | WorkflowExecutionCancelRequestedResult.CancelRequested(attr) when
                attr.Cause = cause &&
                attr.ExternalWorkflowExecution.WorkflowId = workflowId -> return "TEST PASS"

            | _ -> return "TEST FAIL"

            }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.REQUEST_CANCEL, ExecutionStartToCloseTimeout="1200", LambdaRole=TestConfiguration.LambdaRole, TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId, WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId", WorkflowId=childWorkflowId), WorkflowType=TestConfiguration.WorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="DONE - CHECK TEST RESULT FROM CHILD WORKFLOW EXECUTION"))

        // OfflineDecisionTask
        let childOfflineFunc1 = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                              |> OfflineHistoryEvent (        // EventId = 1
                                  WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
                              |> OfflineHistoryEvent (        // EventId = 2
                                  DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                              |> OfflineHistoryEvent (        // EventId = 3
                                  DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))

        let childOfflineFunc2 = OfflineDecisionTask (TestConfiguration.WorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                              |> OfflineHistoryEvent (        // EventId = 1
                                  WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.LambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.WorkflowType))
//                              |> OfflineHistoryEvent (        // EventId = 2
//                                  DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
//                              |> OfflineHistoryEvent (        // EventId = 3
//                                  DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=2L))
//                              |> OfflineHistoryEvent (        // EventId = 4
//                                  DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                              |> OfflineHistoryEvent (        // EventId = 5
                                  WorkflowExecutionCancelRequestedEventAttributes(Cause=WorkflowExecutionCancelRequestedCause.CHILD_POLICY_APPLIED, ExternalWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId)))
                              |> OfflineHistoryEvent (        // EventId = 6
                                  DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                              |> OfflineHistoryEvent (        // EventId = 7
                                  DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=6L))
                              |> OfflineHistoryEvent (        // EventId = 8
                                  DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                              |> OfflineHistoryEvent (        // EventId = 9
                                  WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None (Some(ChildPolicy.REQUEST_CANCEL))

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal childWorkflowId
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

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc1 false 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 0

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "DONE - CHECK TEST RESULT FROM CHILD WORKFLOW EXECUTION"

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc2 false 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 1
                        childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                        childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                     |> should equal "TEST PASS"

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet (!childRunId) childWorkflowId OfflineHistorySubstitutions

    let ``Workflow Execution Cancel Requested with result of NotRequested``() =
        let workflowId = "Workflow Execution Cancel Requested with result of NotRequested"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {

            let! cancel = FlowSharpAction.WorkflowExecutionCancelRequested()
                
            match cancel with
            | WorkflowExecutionCancelRequestedResult.NotRequested -> return "TEST PASS"

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
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 1 do
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

