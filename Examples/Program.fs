module FsDecider.Examples.Program

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FsDecider
open FsDecider.Examples.CommandInterpreter
open FsDecider.UnitTests

(*
How to use the command iterpreter
    When you run this project a command window will appear. Type a command and hit enter.
    Most commands have two parts, the command name and an argument. For example, the command 'sw hello'
    will run the start workflow command (sw) for the example loaded with the key of 'hello'.

    Each example has a comment directly above its registration function which gives the sequence of 
    commands to type to complete the example.

    The following commands are supported.
        sw <key>            Start workflow execution for the example registered with key.
        dt <key>            Poll for a Decision Task and use the decider function registered with key.
        at <key>            Poll for Activity Task and respond with based on the registered key.
        sg <key>            Signals a workflow execution based on the registered key.
        h                   List the current History events for the latest workflow execution.
*)


// Example 1 : Hello FsDecider
//      This example demonstrates a simple FsDecider for a workflow with one simple action, it 
//      completes with a result of "Hello FsDecider"
// To Run, start the project and type these commands into the command line interpreter.
//    sw hello
//    dt hello
let LoadHelloDecider() =
    // This creates a decider function using the FsDecider builder. This
    // decider expression simply completes the workflow with a result of "Hello FsDecider"
    let decider(dt:DecisionTask) =
        Decider(dt) {
            return "Hello FsDecider"            
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, "Hello FsDecider Example", None, None)
    AddOperation (Command.StartWorkflow("hello")) start
    AddOperation (Command.DecisionTask("hello")) (Operation.DecisionTask(decider, false, None))

[<EntryPoint>]
let main argv = 
    // The examples use the same configuration as the unit tests.
    TestConfiguration.GetSwfClient  <- fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) :> IAmazonSimpleWorkflow
    TestConfiguration.Domain        <- "FsDecider"
    TestConfiguration.WorkflowType  <- WorkflowType(Name="FsDecider Test Workflow", Version="1")
    TestConfiguration.ActivityType  <- ActivityType(Name="FsDecider Test Activity", Version="1")
    TestConfiguration.IsConnected   <- true

    if TestConfiguration.IsConnected then
        TestConfiguration.Register()
    else
        failwith "The examples require online access to SWF. Verify TestConfiguration and set the IsConnected property to true."    

    // Load the examples into the interpreter
    LoadHelloDecider()
    FsDecider.Examples.ActivityExamples.Load()
    FsDecider.Examples.SignalExamples.Load()
    FsDecider.Examples.MarkerExamples.Load()
    FsDecider.Examples.ChildWorkflowExamples.Load()
    FsDecider.Examples.TimerExamples.Load()
    FsDecider.Examples.InputAndReturnExamples.Load()
    FsDecider.Examples.ContextExamples.Load()

    // Set up the trace listener
    Trace.TraceSource.Listeners.Clear()
    Trace.TraceSource.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(System.Console.Out)) |> ignore

    // Run the command intperpeter loop
    Loop()

    0 // return an integer exit code
