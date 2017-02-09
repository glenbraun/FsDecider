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
    // The TestConfiguration type is used to set global configuration information. These
    // values can be changed to match your preferences.
    TestConfiguration.GetSwfClient  <- fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) :> IAmazonSimpleWorkflow
    TestConfiguration.Domain        <- "FlowSharp"
    TestConfiguration.WorkflowType  <- WorkflowType(Name="FlowSharp Test Workflow", Version="1")
    TestConfiguration.ActivityType  <- ActivityType(Name="FlowSharp Test Activity", Version="1")
    TestConfiguration.LambdaName    <- "SwfLambdaTest"
    TestConfiguration.LambdaRole    <- null  // Note: The Lambda unit tests only run in offline mode if LambdaRole is not specified.

    TestConfiguration.ReverseOrder              <- false
    TestConfiguration.GenerateOfflineHistory    <- false
    
    // The unit tests can run in an offline mode using a fake event history. This is faster and
    // doesn't use SWF resources.
    TestConfiguration.IsConnected               <- false

    // Register the domain, workfow and activity types if needed
    if TestConfiguration.IsConnected then
        TestConfiguration.Register()

    // Set up the trace log
    use log = System.IO.File.CreateText("..\\..\\log.txt")
    let listener = new System.Diagnostics.TextWriterTraceListener(log)
    TraceSource.Listeners.Clear()
    TraceSource.Listeners.Add(listener) |> ignore

    // Run the tests    
    run tests |> ignore
    0
