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

module TestWaitForAllLambdaFunction =
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
