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

module TestWaitForAnyLambdaFunction =
    let private OfflineHistorySubstitutions =  
        Map.empty<string, string>
        |> Map.add "WorkflowType" "TestConfiguration.WorkflowType"
        |> Map.add "RunId" "\"Offline RunId\""
        |> Map.add "WorkflowId" "workflowId"
        |> Map.add "LambdaRole" "TestConfiguration.LambdaRole"
        |> Map.add "TaskList" "TestConfiguration.TaskList"
        |> Map.add "Identity" "TestConfiguration.Identity"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Id" "lambdaId"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Name" "TestConfiguration.LambdaName"
        |> Map.add "LambdaFunctionScheduledEventAttributes.Input" "TestConfiguration.LambdaInput"
        |> Map.add "LambdaFunctionCompletedEventAttributes.Result" "TestConfiguration.LambdaResult"

    let ``Wait For Any Lambda Function with One Completed Lambda Function``() =
        let workflowId = "Wait For Any Lambda Function with One Completed Lambda Function"
        let lambdaId = "lambda1"

        let FiveSeconds = "5"


        let deciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! lambda1 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "1",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let! lambda2 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "2",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let lambdaList = [lambda1; lambda2;]

            do! FlowSharpAction.WaitForAnyLambdaFunction(lambdaList)

            let finishedList = 
                lambdaList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleLambdaFunctionResult.Completed(attr) ] when attr.Result = TestConfiguration.LambdaResult -> return "TEST PASS"
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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId + "1", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId + "2", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.LambdaResult, ScheduledEventId=5L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=10L, StartedEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=12L, Result="TEST PASS"))

        if TestConfiguration.IsConnected then
            // Only offline supported
            ()
        else
            // Start the workflow
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal (lambdaId + "1")
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.LambdaName
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.LambdaInput
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal (lambdaId + "2")
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.LambdaName
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.LambdaInput
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
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

    let ``Wait For Any Lambda Function with All Completed Lambda Functions``() =
        let workflowId = "Wait For Any Lambda Function with All Completed Lambda Functions"
        let lambdaId = "lambda1"

        let FiveSeconds = "5"


        let deciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! lambda1 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "1",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let! lambda2 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "2",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let lambdaList = [lambda1; lambda2;]

            do! FlowSharpAction.WaitForAnyLambdaFunction(lambdaList)

            let finishedList = 
                lambdaList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleLambdaFunctionResult.Completed(attr1); ScheduleLambdaFunctionResult.Completed(attr2); ] 
                when attr1.Result = TestConfiguration.LambdaResult && 
                     attr2.Result = TestConfiguration.LambdaResult -> return "TEST PASS"

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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId+"1", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId+"2", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.LambdaResult, ScheduledEventId=6L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 10
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 11
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.LambdaResult, ScheduledEventId=5L, StartedEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=9L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=9L, StartedEventId=12L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=13L, Result="TEST PASS"))

        // Start the workflow
        let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

        // Poll and make decisions
        for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 2 do
            match i with
            | 1 -> 
                resp.Decisions.Count                    |> should equal 2
                resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal (lambdaId + "1")
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.LambdaName
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.LambdaInput
                resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                        |> should equal (FiveSeconds.ToString())

                resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Id
                                                        |> should equal (lambdaId + "2")
                resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Name
                                                        |> should equal TestConfiguration.LambdaName
                resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Input
                                                        |> should equal TestConfiguration.LambdaInput
                resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
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

    let ``Wait For Any Lambda Function with No Completed Lambda Functions``() =
        let workflowId = "Wait For Any Lambda Function with No Completed Lambda Functions"
        let lambdaId = "lambda1"

        let FiveSeconds = "5"


        let deciderFunc(dt:DecisionTask) =
            FlowSharp(dt, TestConfiguration.ReverseOrder) {
            
            // Schedule a Lambda Function
            let! lambda1 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "1",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let! lambda2 = FlowSharpAction.ScheduleLambdaFunction (
                            id=lambdaId + "2",
                            name=TestConfiguration.LambdaName,
                            input=TestConfiguration.LambdaInput,
                            startToCloseTimeout=FiveSeconds
                          )

            let lambdaList = [lambda1; lambda2;]

            do! FlowSharpAction.WaitForAnyLambdaFunction(lambdaList)

            let finishedList = 
                lambdaList
                |> List.choose (fun r -> if r.IsFinished() then Some(r) else None)

            match finishedList with
            | [ ScheduleLambdaFunctionResult.Completed(attr1); ScheduleLambdaFunctionResult.Completed(attr2); ] 
                when attr1.Result = TestConfiguration.LambdaResult && 
                     attr2.Result = TestConfiguration.LambdaResult -> return "TEST PASS"

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
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId + "1", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 6
                              LambdaFunctionScheduledEventAttributes(DecisionTaskCompletedEventId=4L, Id=lambdaId + "2", Input=TestConfiguration.LambdaInput, Name=TestConfiguration.LambdaName, StartToCloseTimeout="5"))
                          |> OfflineHistoryEvent (        // EventId = 7
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=5L))
                          |> OfflineHistoryEvent (        // EventId = 8
                              LambdaFunctionStartedEventAttributes(ScheduledEventId=6L))
                          |> OfflineHistoryEvent (        // EventId = 9
                              WorkflowExecutionSignaledEventAttributes(Input="", SignalName="Test Signal"))
                          |> OfflineHistoryEvent (        // EventId = 10
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 11
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=10L))
                          |> OfflineHistoryEvent (        // EventId = 12
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=10L, StartedEventId=11L))
                          |> OfflineHistoryEvent (        // EventId = 13
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.LambdaResult, ScheduledEventId=5L, StartedEventId=7L))
                          |> OfflineHistoryEvent (        // EventId = 14
                              DecisionTaskScheduledEventAttributes(StartToCloseTimeout="1200", TaskList=TestConfiguration.TaskList))
                          |> OfflineHistoryEvent (        // EventId = 15
                              LambdaFunctionCompletedEventAttributes(Result=TestConfiguration.LambdaResult, ScheduledEventId=6L, StartedEventId=8L))
                          |> OfflineHistoryEvent (        // EventId = 16
                              DecisionTaskStartedEventAttributes(Identity=TestConfiguration.Identity, ScheduledEventId=14L))
                          |> OfflineHistoryEvent (        // EventId = 17
                              DecisionTaskCompletedEventAttributes(ScheduledEventId=14L, StartedEventId=16L))
                          |> OfflineHistoryEvent (        // EventId = 18
                              WorkflowExecutionCompletedEventAttributes(DecisionTaskCompletedEventId=17L, Result="TEST PASS"))

        if TestConfiguration.IsConnected then
            // Only offline supported
            ()
        else
            // Start the workflow
            let runId = TestHelper.StartWorkflowExecutionOnTaskList (TestConfiguration.WorkflowType) workflowId (TestConfiguration.TaskList) None None None

            // Poll and make decisions
            for (i, resp) in TestHelper.PollAndDecide (TestConfiguration.TaskList) deciderFunc offlineFunc false 3 do
                match i with
                | 1 -> 
                    resp.Decisions.Count                    |> should equal 2
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal (lambdaId + "1")
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.LambdaName
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.LambdaInput
                    resp.Decisions.[0].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    resp.Decisions.[1].DecisionType         |> should equal DecisionType.ScheduleLambdaFunction
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Id
                                                            |> should equal (lambdaId + "2")
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Name
                                                            |> should equal TestConfiguration.LambdaName
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.Input
                                                            |> should equal TestConfiguration.LambdaInput
                    resp.Decisions.[1].ScheduleLambdaFunctionDecisionAttributes.StartToCloseTimeout
                                                            |> should equal (FiveSeconds.ToString())

                    TestHelper.RespondDecisionTaskCompleted resp

                    // Send a signal to generate a decision task
                    TestHelper.SignalWorkflow runId workflowId "Test Signal" ""

                | 2 -> 
                    resp.Decisions.Count                    |> should equal 0

                    TestHelper.RespondDecisionTaskCompleted resp

                | 3 -> 
                    resp.Decisions.Count                    |> should equal 1
                    resp.Decisions.[0].DecisionType         |> should equal DecisionType.CompleteWorkflowExecution
                    resp.Decisions.[0].CompleteWorkflowExecutionDecisionAttributes.Result 
                                                            |> should equal "TEST PASS"

                    TestHelper.RespondDecisionTaskCompleted resp
                | _ -> ()

            // Generate Offline History
            TestHelper.GenerateOfflineDecisionTaskCodeSnippet runId workflowId OfflineHistorySubstitutions
