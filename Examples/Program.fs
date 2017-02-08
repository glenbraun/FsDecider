module FlowSharp.Examples.Program

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

(*
How to use the command iterpreter
    When you run this project a command window will appear
    Type a command and hit enter
    Most commands are two parts, the command name and an argument. For example, the command 'sw hello'
    will run the start workflow command (sw) for the example registered with the key of 'hello'.

    Each example has a comment directly above its registration function which gives the sequence of 
    commands to type to complete the example. Typing commands different from the ones provided will produce
    unspecified results. 

    The following commands are supported.
        sw <key>            Start workflow execution for the example registered with key.
        dt <key>            Poll for a Decision Task and use the decider function registered with key.
        at <key>            Poll for Activity Task and respond with based on the registered key.
        sg <key>            Signals a workflow execution based on the registered key.
        h                   List the current History events for the latest workflow execution.

*)


// Example 1 : Hello FlowSharp
//      This example demonstrates a simple FlowSharp decider for a workflow with one simple action, it 
//      completes with a result of "Hello FlowSharp"
// To Run, start the project and type these commands into the command line interpreter.
//    sw hello
//    dt hello
let RegisterHelloFlowSharp() =
    // This creates a decider function using the FlowSharp decider builder. This
    // decider expression simply completes the workflow with a result of "Hello FlowSharp"
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt, false) {
            return "Hello FlowSharp"            
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, "Hello FlowSharp example", None, None)
    AddOperation (Command.StartWorkflow("hello")) start
    AddOperation (Command.DecisionTask("hello")) (Operation.DecisionTask(decider, None))

[<EntryPoint>]
let main argv = 
    TestConfiguration.GetSwfClient  <- fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) :> IAmazonSimpleWorkflow
    TestConfiguration.Domain        <- "FlowSharp"
    TestConfiguration.WorkflowType  <- WorkflowType(Name="FlowSharp Unit Test Workflow", Version="1")
    TestConfiguration.ActivityType  <- ActivityType(Name="FlowSharp Unit Test Activity", Version="1")

    RegisterHelloFlowSharp()
    FlowSharp.Examples.ActivityExamples.Register()
    FlowSharp.Examples.SignalExamples.Register()
    FlowSharp.Examples.MarkerExamples.Register()
    FlowSharp.Examples.ChildWorkflowExamples.Register()
    FlowSharp.Examples.TimerExamples.Register()
    FlowSharp.Examples.InputAndReturnExamples.Register()

    System.Diagnostics.Trace.Listeners.Clear()
    System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(System.Console.Out)) |> ignore

    Loop()
    0 // return an integer exit code
