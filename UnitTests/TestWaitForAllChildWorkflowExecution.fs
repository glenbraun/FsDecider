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

module TestWaitForAllChildWorkflowExecution =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "SignalName" "signalName"
        |> Map.add "ParentWorkflowExecution" "WorkflowExecution(RunId=\"Parent RunId\", WorkflowId=workflowId)"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.TaskList" "childTaskList"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "StartChildWorkflowExecutionFailedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "ChildWorkflowExecutionStartedEventAttributes.WorkflowId" "childWorkflowId"

    let ``Wait For All Child Workflow Execution with All Completed Child Workflow Execution``() =
        let workflowId = "Wait For All Child Workflow Execution with All Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for All Child Workflow Execution
            do! FlowSharp.WaitForAllChildWorkflowExecution(childList)

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
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 11
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=6L, Result="Child Result", StartedEventId=9L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
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
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (twice)
                for k = 1 to 2 do 
                    for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc 1 do
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

    let ``Wait For All Child Workflow Execution with One Completed Child Workflow Execution``() =
        let workflowId = "Wait For All Child Workflow Execution with One Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for All Child Workflow Execution
            do! FlowSharp.WaitForAllChildWorkflowExecution(childList)

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
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=6L, Result="Child Result", StartedEventId=9L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=15L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=16L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
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
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal childTaskList.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (once)
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 1
                        childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                        childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                        |> should equal childResult

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

            | 2 ->
                resp.Decisions.Count                    |> should equal 0

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions (once)
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc 1 do
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

    let ``Wait For All Child Workflow Execution with No Completed Child Workflow Execution``() =
        let workflowId = "Wait For All Child Workflow Execution with No Completed Child Workflow Execution"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let childResult = "Child Result"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Start a Child Workflow Execution
            let! start1 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "1",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let! start2 = FlowSharp.StartChildWorkflowExecution
                              (
                                TestConfiguration.TestWorkflowType,
                                childWorkflowId + "2",
                                input=childInput,
                                childPolicy=ChildPolicy.TERMINATE,
                                lambdaRole=TestConfiguration.TestLambdaRole,
                                taskList=childTaskList,
                                executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                                taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                              )

            let childList = [start1; start2;]

            // Wait for Any Child Workflow Execution
            do! FlowSharp.WaitForAnyChildWorkflowExecution(childList)

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
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
                return childResult
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"1", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="2", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId+"2", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="Child Result", StartedEventId=7L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 1", WorkflowId=childWorkflowId+"1"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 14
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=6L, Result="Child Result", StartedEventId=9L, WorkflowExecution=WorkflowExecution(RunId="Child RunId 2", WorkflowId=childWorkflowId+"2"), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 15
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=13L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=13L, StartedEventId=15L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=16L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Child RunId 1", WorkflowId = childWorkflowId + "1"))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Offline RunId", WorkflowId=workflowId), TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="Child Result"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 3 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal (childWorkflowId + "1")
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
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
                                                        |> should equal TestConfiguration.TestWorkflowType.Name
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal TestConfiguration.TestWorkflowType.Version
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[1].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
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

                // Process Child Workflow Decisions (twice)
                for k = 1 to 2 do 
                    for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc 1 do
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

