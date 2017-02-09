module FlowSharp.UnitTests.Main

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model
open Fuchu
open FlowSharp.UnitTests.Tests
open FlowSharp.Trace

[<EntryPoint>]
let main argv = 
    TestConfiguration.GetSwfClient  <- fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) :> IAmazonSimpleWorkflow
    TestConfiguration.Domain        <- "FlowSharp"
    TestConfiguration.WorkflowType  <- WorkflowType(Name="FlowSharp Unit Test Workflow", Version="1")
    TestConfiguration.ActivityType  <- ActivityType(Name="FlowSharp Unit Test Activity", Version="1")
    TestConfiguration.LambdaName    <- "SwfLambdaTest"
    TestConfiguration.LambdaRole    <- ""  

    TestConfiguration.ReverseOrder              <- false
    TestConfiguration.GenerateOfflineHistory    <- false
    TestConfiguration.IsConnected               <- false

    if TestConfiguration.IsConnected then
        TestConfiguration.Register()

    use log = System.IO.File.CreateText("..\\..\\log.txt")
    let listener = new System.Diagnostics.TextWriterTraceListener(log)
    
    TraceSource.Listeners.Clear()
    TraceSource.Listeners.Add(listener) |> ignore
    
    run tests |> ignore
    0

