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

module TestScheduleLambdaFunction =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.TestWorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.TestLambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TestTaskList"
        |> Map.add "Identity" "TestConfiguration.TestIdentity"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Id" "lambdaId"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Name" "TestConfiguration.TestLambdaName"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Input" "TestConfiguration.TestLambdaInput"
        |> Map.add "LambdaFunctionCompletedEventAttributes.Result" "TestConfiguration.TestLambdaResult"

    let ``Schedule Lambda Function with result of Scheduling``() =
        let workflowId = "Schedule Lambda Function with result of Scheduling"
        let lambdaId = "lambda1"
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(attr) when attr.Id = lambdaId -> return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.TestLambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Lambda Function with result of Scheduled``() =
        let workflowId = "Schedule Lambda Function with result of Scheduled"
        let lambdaId = "lambda1"
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(attr) when attr.Id = lambdaId -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.Scheduled(attr) when
                attr.Id = lambdaId &&
                attr.Name = TestConfiguration.TestLambdaName &&
                attr.Input = TestConfiguration.TestLambdaInput ->

                return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        if TestConfiguration.IsConnected then
            // Only supports offline
            ()
        else 
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal lambdaId
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.TestLambdaName
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.TestLambdaInput
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    TestHelper.RespondDecisionTaskCompleted resp

                | 2 ->
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp

                | _ -> ()

            // Generate Offline History
            TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Lambda Function with result of Started``() =
        let workflowId = "Schedule Lambda Function with result of Started"
        let lambdaId = "lambda1"
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(attr) when attr.Id = lambdaId -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.Started(_, scheduled) when
                scheduled.Id = lambdaId &&
                scheduled.Name = TestConfiguration.TestLambdaName &&
                scheduled.Input = TestConfiguration.TestLambdaInput ->

                return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        if TestConfiguration.IsConnected then
            // Only supports offline
            ()
        else 
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal lambdaId
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.TestLambdaName
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.TestLambdaInput
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    TestHelper.RespondDecisionTaskCompleted resp

                | 2 ->
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp

                | _ -> ()

            // Generate Offline History
            TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions

    let ``Schedule Lambda Function with result of Completed``() =
        let workflowId = "Schedule Lambda Function with result of Completed"
        let lambdaId = "lambda1"
        let FiveSeconds = "5"


        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(_) -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.Completed(attr) when attr.Result = TestConfiguration.TestLambdaResult -> return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="30"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.TestLambdaResult, ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.TestLambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleeping for 5 seconds to give lambda funtion time to complete.")
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

    let ``Schedule Lambda Function with result of TimedOut``() =
        let workflowId = "Schedule Lambda Function with result of TimedOut"
        let lambdaId = "lambda1"
        let lambdaInput = "\"timeout\""
        let timeoutType = LambdaFunctionTimeoutType.START_TO_CLOSE
        let FiveSeconds = "5"   // Note: Lambda function must run for more than 5 seconds

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=lambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(_) -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.TimedOut(attr) when attr.TimeoutType = timeoutType -> return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=lambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionTimedOutEventAttributes(ScheduledEventId=5L, StartedEventId=6L, TimeoutType=LambdaFunctionTimeoutType.START_TO_CLOSE))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal lambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleeping for 5 seconds to give lambda funtion time to complete.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0))

                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Remove("LambdaFunctionScheduledEventAttributes.Input").Add("LambdaFunctionScheduledEventAttributes.Input", "lambdaInput"))

    let ``Schedule Lambda Function with result of Failed``() =
        let workflowId = "Schedule Lambda Function with result of Failed"
        let lambdaId = "lambda1"
        let lambdaInput = "\"fail\""
        let FiveSeconds = "5"   // Note: Lambda function must run for more than 5 seconds

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=lambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            match result with
            | ScheduleLambdaFunctionResult.Scheduling(_) -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.Failed(attr) -> return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=lambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionFailedEventAttributes(Details="{\"stackTrace\": [[\"/var/task/lambda_function.py\", 10, \"lambda_handler\", \"raise Exception('Lambda failed')\"]], \"errorType\": \"Exception\", \"errorMessage\": \"Lambda failed\"}", Reason="UnhandledError", ScheduledEventId=5L, StartedEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=8L, StartedEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=10L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal lambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                TestHelper.RespondDecisionTaskCompleted resp

                if TestConfiguration.IsConnected then
                    System.Diagnostics.Debug.WriteLine("Sleeping for 5 seconds to give lambda funtion time to complete.")
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0))

                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Remove("LambdaFunctionScheduledEventAttributes.Input").Add("LambdaFunctionScheduledEventAttributes.Input", "lambdaInput"))

    let ``Schedule Lambda Function with result of ScheduleFailed``() =
        let workflowId = "Schedule Lambda Function with result of ScheduleFailed"
        let lambdaId = "lambda1"
        let cause = ScheduleLambdaFunctionFailedCause.ID_ALREADY_IN_USE
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            // Note: This test relies on intionally duplicating the schedule lambda decision to force the error
            match result with
            | ScheduleLambdaFunctionResult.Scheduling(_) -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.ScheduleFailed(attr) 
                when attr.Id = lambdaId &&
                     attr.Name = TestConfiguration.TestLambdaName &&
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
                              ScheduleLambdaFunctionFailedEventAttributes(Cause=ScheduleLambdaFunctionFailedCause.ID_ALREADY_IN_USE, DecisionTaskCompletedEventId=4L, Id="lambda1", Name="SwfLambdaTest"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=8L, Result="TEST PASS"))

        // Start the workflow
        if TestConfiguration.IsConnected then
            // Only offline supported for this test
            ()
        else 
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal lambdaId
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.TestLambdaName
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.TestLambdaInput
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    // Make a duplicate of the ScheduleLambdaFunction decision to force a scheduling error
                    resp.Decisions.Add(resp.Decisions.[0])
                
                    TestHelper.RespondDecisionTaskCompleted resp
                
                | 2 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp
                | _ -> ()

    let ``Schedule Lambda Function with result of StartFailed``() =
        let workflowId = "Schedule Lambda Function with result of StartFailed"
        let lambdaId = "lambda1"
        let lambdaRole = null
        let cause = StartLambdaFunctionFailedCause.ASSUME_ROLE_FAILED
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! result = FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            // Note: This test relies on intionally duplicating the schedule lambda decision to force the error
            match result with
            | ScheduleLambdaFunctionResult.Scheduling(_) -> do! FlowSharp.Wait()
            | ScheduleLambdaFunctionResult.StartFailed(attr) 
                when attr.Cause = cause -> return "TEST PASS"
            | _ -> return "TEST FAIL"                        
        }

        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))
                          |> OfflineHistoryEvent (        // EventId = 1
                              WorkflowExecutionStartedEventAttributes(ChildPolicy=ChildPolicy.TERMINATE, ExecutionStartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList, TaskStartToCloseTimeout="1200", WorkflowType=TestConfiguration.TestWorkflowType))
                          |> OfflineHistoryEvent (        // EventId = 2
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 3
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=2L))
                          |> OfflineHistoryEvent (        // EventId = 4
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=2L, StartedEventId=3L))
                          |> OfflineHistoryEvent (        // EventId = 5
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              StartLambdaFunctionFailedEventAttributes(Cause=StartLambdaFunctionFailedCause.ASSUME_ROLE_FAILED, Message="No IAM role is attached to the current workflow execution.", ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 7
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TestTaskList))
                          |> OfflineHistoryEvent (        // EventId = 8
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.TestIdentity, ScheduledEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=7L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=9L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None (Some(lambdaRole)) None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.TestLambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                TestHelper.RespondDecisionTaskCompleted resp
                
            | 2 -> 
                resp.Decisions.Count                    |> should equal 1
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId (OfflineHistorySubstitutions.Remove("LambdaRole").Add("LambdaRole", "lambdaRole"))

    let ``Schedule Lambda Function using do!``() =
        let workflowId = "Schedule Lambda Function using do!"
        let lambdaId = "lambda1"
        let FiveSeconds = "5"

        let deciderFunc(dt:DecisionTask) =
            FlowSharp.Builder(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            do! FlowSharp.ScheduleLambdaFunction (
                            id=lambdaId,
                            name=TestConfiguration.TestLambdaName,
                            input=TestConfiguration.TestLambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId, Input=TestConfiguration.TestLambdaInput, Name=TestConfiguration.TestLambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=4L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.TestWorkflowType) workflowId (TestConfiguration.TestTaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TestTaskList) deciderFunc offlineFunc false 1 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal lambdaId
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.TestLambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.TestLambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                resp.Decisions.[1].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                        |> should equal "TEST PASS"

                TestHelper.RespondDecisionTaskCompleted resp
            | _ -> ()

        // Generate Offline History
        TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
