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

module TestStartChildWorkflowExecution =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "SignalName" "signalName"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.TaskList" "childTaskList"
        |> Map.add "StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "StartChildWorkflowExecutionFailedEventAttributes.WorkflowId" "childWorkflowId"
        |> Map.add "ChildWorkflowExecutionStartedEventAttributes.WorkflowId" "childWorkflowId"

    let ``Start Child Workflow Execution with result of Started``() =
        let workflowId = "Start Child Workflow Execution with result of Started"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let childTaskList = TaskList(Name="Child")
        let signalName = "Test Signal"
        let childRunId = ref ""

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start a Child Workflow Execution
            let! start = FlowSharp.StartChildWorkflowExecution
                          (
                            TestConfiguration.TestWorkflowType,
                            childWorkflowId,
                            input=childInput,
                            childPolicy=ChildPolicy.TERMINATE,
                            lambdaRole=TestConfiguration.TestLambdaRole,
                            taskList=childTaskList,
                            executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                            taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                          )

            match start with
            | StartChildWorkflowExecutionResult.Scheduling ->
                return ()
            | StartChildWorkflowExecutionResult.Initiated(_) ->
                return ()
            | StartChildWorkflowExecutionResult.Started(attr, c) when 
                attr.WorkflowType.Name = TestConfiguration.TestWorkflowType.Name &&
                attr.WorkflowType.Version = TestConfiguration.TestWorkflowType.Version &&
                attr.WorkflowExecution.WorkflowId = childWorkflowId -> 
                
                childRunId := attr.WorkflowExecution.RunId

                return "TEST PASS"
            | _ -> return "TEST FAIL"                        
        }

        let childDeciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
                return "OK"
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=childTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId, WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              ChildWorkflowExecutionStartedEventAttributes(InitiatedEventId=5L, WorkflowExecution=WorkflowExecution(RunId="Child RunId", WorkflowId=childWorkflowId), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              ChildWorkflowExecutionCompletedEventAttributes(InitiatedEventId=5L, Result="OK", StartedEventId=6L, WorkflowExecution=WorkflowExecution(RunId="Child RunId", WorkflowId=childWorkflowId), WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // OfflineDecisionTask
        let childOfflineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                                  |> OfflineHistoryEvent (        // EventId = 1
                                      WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, ParentInitiatedEventId=5L, ParentWorkflowExecution=WorkflowExecution(RunId="Child RunId", WorkflowId=childWorkflowId), TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                                  |> OfflineHistoryEvent (        // EventId = 2
                                      DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                                  |> OfflineHistoryEvent (        // EventId = 3
                                      DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                                  |> OfflineHistoryEvent (        // EventId = 4
                                      DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                                  |> OfflineHistoryEvent (        // EventId = 5
                                      WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="OK"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal childWorkflowId
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

                TestHelper.RespondDecisionTaskCompleted resp

                // Process Child Workflow Decisions
                for (j, childResp) in TestHelper.PollAndDecide childTaskList childDeciderFunc childOfflineFunc 1 do
                    match j with
                    | 1 -> 
                        childResp.Decisions.Count                    |> should equal 1
                        childResp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                        childResp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                                     |> should equal "OK"

                        TestHelper.RespondDecisionTaskCompleted childResp

                    | _ -> ()

            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Add("ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution", "WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)").Add("ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution","WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)"))
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet (!childRunId) childWorkflowId OfflineHistorySubstitutions

    let ``Start Child Workflow Execution with result of Scheduling``() =
        let workflowId = "Start Child Workflow Execution with result of Scheduling"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let signalName = "Test Signal"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start a Child Workflow Execution
            let! start = FlowSharp.StartChildWorkflowExecution
                          (
                            TestConfiguration.TestWorkflowType,
                            childWorkflowId,
                            input=childInput,
                            childPolicy=ChildPolicy.TERMINATE,
                            lambdaRole=TestConfiguration.TestLambdaRole,
                            taskList=TestConfiguration.TestTaskList,
                            executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                            taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                          )

            match start with
            | StartChildWorkflowExecutionResult.Scheduling -> return "TEST PASS"
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId, WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal childWorkflowId
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
                                                        |> should equal TestConfiguration.TestTaskList.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Start Child Workflow Execution with result of Initiated``() =
        let workflowId = "Start Child Workflow Execution with result of Initiated"
        let childWorkflowId = "Child of " + workflowId
        let childInput = "Test Child Input"
        let signalName = "Test Signal"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start a Child Workflow Execution
            let! start = FlowSharp.StartChildWorkflowExecution
                          (
                            TestConfiguration.TestWorkflowType,
                            childWorkflowId,
                            input=childInput,
                            childPolicy=ChildPolicy.TERMINATE,
                            lambdaRole=TestConfiguration.TestLambdaRole,
                            taskList=TestConfiguration.TestTaskList,
                            executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                            taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                          )

            match start with
            | StartChildWorkflowExecutionResult.Scheduling ->
               return ()
                               
            | StartChildWorkflowExecutionResult.Initiated(attr) when
                    attr.ChildPolicy = ChildPolicy.TERMINATE &&
                    attr.Input = childInput &&
                    attr.LambdaRole = TestConfiguration.TestLambdaRole &&
                    attr.TaskList.Name = TestConfiguration.TestTaskList.Name &&
                    attr.WorkflowId = childWorkflowId &&
                    attr.WorkflowType.Name = TestConfiguration.TestWorkflowType.Name &&
                    attr.WorkflowType.Version = TestConfiguration.TestWorkflowType.Version &&
                    attr.ExecutionStartToCloseTimeout = (TestConfiguration.TwentyMinuteTimeout.ToString()) &&
                    attr.TaskStartToCloseTimeout = (TestConfiguration.TwentyMinuteTimeout.ToString()) -> return "TEST PASS"
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
                              StartChildWorkflowExecutionInitiatedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, Control="1", DecisionTaskCompletedEventId=4L, ExecutionStartToCloseTimeout="1200", Input="Test Child Input", LambdaRole=TestConfiguration.TestLambdaRole, TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowId=childWorkflowId, WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        if TestConfiguration.IsConnected then
            // This unit test only supports offline
            ()
        else
            // Start the workflow
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                            |> should equal childWorkflowId
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
                                                            |> should equal TestConfiguration.TestTaskList.Name
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                            |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                    resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                            |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())

                    TestHelper.RespondDecisionTaskCompleted resp

                | 2 ->
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp
                | _ -> ()

    let ``Start Child Workflow Execution with result of StartFailed``() =
        let workflowId = "Start Child Workflow Execution with result of StartFailed"
        let childWorkflowId = "Child of " + workflowId
        let childWorkflowType = WorkflowType(Name="DoesNotExist_D8206D86-A00E-4C5B-8A5F-DF0C5DD974FC", Version="1")
        let childInput = "Test Child Input"
        let cause = StartChildWorkflowExecutionFailedCause.WORKFLOW_TYPE_DOES_NOT_EXIST
        let signalName = "Test Signal"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt) {
            
            // Start a Child Workflow Execution
            let! start = FlowSharp.StartChildWorkflowExecution
                          (
                            childWorkflowType,
                            childWorkflowId,
                            input=childInput,
                            childPolicy=ChildPolicy.TERMINATE,
                            lambdaRole=TestConfiguration.TestLambdaRole,
                            taskList=TestConfiguration.TestTaskList,
                            executionStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout,
                            taskStartToCloseTimeout=TestConfiguration.TwentyMinuteTimeout
                          )

            match start with
            | StartChildWorkflowExecutionResult.Scheduling -> 
                return ()

            | StartChildWorkflowExecutionResult.StartFailed(attr) when
                    attr.WorkflowId = childWorkflowId &&
                    attr.WorkflowType.Name = childWorkflowType.Name &&
                    attr.WorkflowType.Version = childWorkflowType.Version &&
                    attr.Cause = cause -> return "TEST PASS"
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
                              StartChildWorkflowExecutionFailedEventAttributes(Cause=StartChildWorkflowExecutionFailedCause.WORKFLOW_TYPE_DOES_NOT_EXIST, Control="1", DecisionTaskCompletedEventId=4L, WorkflowId=childWorkflowId, WorkflowType=childWorkflowType))
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
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.StartChildWorkflowExecution
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowId 
                                                        |> should equal childWorkflowId
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Name 
                                                        |> should equal childWorkflowType.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.WorkflowType.Version 
                                                        |> should equal childWorkflowType.Version
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.Input 
                                                        |> should equal childInput
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ChildPolicy  
                                                        |> should equal ChildPolicy.TERMINATE
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.LambdaRole  
                                                        |> should equal TestConfiguration.TestLambdaRole
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskList.Name  
                                                        |> should equal TestConfiguration.TestTaskList.Name
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.ExecutionStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                resp.Decisions.[0].StartChildWorkflowExecutionDecisionAttributes.TaskStartToCloseTimeout 
                                                        |> should equal (TestConfiguration.TwentyMinuteTimeout.ToString())
                
                TestHelper.RespondDecisionTaskCompleted resp

            | 2 ->
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Add("ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution", "WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)").Add("ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution","WorkflowExecution(RunId=\"Child RunId\", WorkflowId=childWorkflowId)"))

