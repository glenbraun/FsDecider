module FlowSharp.Examples.Program

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Examples.CommandInterpreter

(*
sw a1  // start workflow for example as (activity in series) (grab runid and make sure same for all below)
dt a1  // do the decision task for a1  (this polls, gets the decision and responds)
at a   // do the activity for at (this polls, and completes activity)
dt a1  // do the decision task for a1
h      // gets the current history for the active workflow

*)


let RegisterExample1() =
    let start = Operation.StartWorkflowExecution(ExamplesConfiguration.WorkflowType, "FlowSharp Example 1", None, None)
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt, false) {
            return "OK"            
        }

    AddOperation (Command.StartWorkflow("e1")) start
    AddOperation (Command.DecisionTask("e1")) (Operation.DecisionTask(decider, None))
    // To Run: 
    //    sw e1
    //    dt e1


[<EntryPoint>]
let main argv = 
    RegisterExample1()

    System.Diagnostics.Trace.Listeners.Clear()
    System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(System.Console.Out)) |> ignore

    Loop()
    0 // return an integer exit code
