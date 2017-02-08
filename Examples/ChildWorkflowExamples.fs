module FlowSharp.Examples.ChildWorkflowExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example c1 : Start and wait for a child workflow 
//      This example demonstrates starting a child workflow and waiting for it to complete
//      from within a parent workflow.
// To Run, start the project and type these commands into the command line interpreter.
//    sw c1             (Starts the workflow)
//    dt c1p            (Processes the initial decision task for the parent workflow, starts the child workflow and waits)
//    dt c1c            (Processes the decision task for the child workflow, completes workflow)
//    dt c1p            (Processes the final decision task for the parent workflow, completes workflow)
let private RegisterChildWorkflowExample() =
    let parentWorkflowId = "FlowSharp Child Workflow Example (parent)"
    let childWorkflowId = "FlowSharp Child Workflow Example (child)"

    let parentDecider(dt:DecisionTask) =
        FlowSharp(dt) {
            // Start a child workflow
            let! child = FlowSharpAction.StartChildWorkflowExecution(TestConfiguration.WorkflowType, childWorkflowId)

            match child with
            | StartChildWorkflowExecutionResult.Completed(_) ->
                // Complete the workflow execution with a result of "OK"
                return "OK"
            | _ -> 
                do! FlowSharpAction.Wait()
        }

    let childDecider(dt:DecisionTask) =
        FlowSharp(dt) {
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, parentWorkflowId, None, None)
    AddOperation (Command.StartWorkflow("c1")) start
    AddOperation (Command.DecisionTask("c1p")) (Operation.DecisionTask(parentDecider, false, None))
    AddOperation (Command.DecisionTask("c1c")) (Operation.DecisionTask(childDecider, false, None))

let Register() =
    RegisterChildWorkflowExample()
