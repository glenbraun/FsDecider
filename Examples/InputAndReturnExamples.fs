module FlowSharp.Examples.InputAndReturnExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example ir1 : Input and Return Example
//      This example demonstracts getting the input to a workflow and return a result based on it.
// To Run, start the project and type these commands into the command line interpreter.
//    sw ir1c           (Starts the workflow with input of "Completed")
//    dt ir1            (Processes the one decision task, gets input and completes workflow with "OK")
//    sw ir1f           (Starts the workflow with input of "Failed")
//    dt ir1            (Processes the one decision task, gets input and fails workflow with reason and details)
//    sw ir1n           (Starts the workflow with input of "Canceled")
//    dt ir1            (Processes the one decision task, gets input and cancels workflow with details)
let private RegisterInputAndReturnExample() =
    let workflowId = "FlowSharp Input and Return Example"

    let decider(dt:DecisionTask) =
        FlowSharp(dt) {
            // Get the input to the workflow, set when starting the workflow
            let! input = FlowSharpAction.GetWorkflowExecutionInput()

            // Based on the input value, return as Completed, Failed, or Canceled
            match input with
            | "Completed"   -> return "OK"   // Short for: ReturnResult.CompleteWorkflowExecution("OK")
            | "Failed"      -> return ReturnResult.FailWorkflowExecution("Some Reason", "Some Details")
            | "Canceled"    -> return ReturnResult.CancelWorkflowExecution("Some Details")
            | _             -> failwith "Unhandled input value."
        }

    // The code below supports the example runner
    AddOperation (Command.StartWorkflow("ir1c")) (Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, Some("Completed")))
    AddOperation (Command.StartWorkflow("ir1f")) (Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, Some("Failed")))
    AddOperation (Command.StartWorkflow("ir1n")) (Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, Some("Canceled")))
    AddOperation (Command.DecisionTask("ir1")) (Operation.DecisionTask(decider, false, None))

let Register() =
    RegisterInputAndReturnExample()

