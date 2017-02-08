module FlowSharp.Examples.TimerExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example t1 : Start a timer and wait for it to fire
//      This example demonstracts starting a timer and then waiting for it to fire.
// To Run, start the project and type these commands into the command line interpreter.
//    sw t1             (Starts the workflow)
//    dt t1             (Processes the initial decision task, starts the timer and waits)
//    dt t1             (Processes the final decision task, after 15 seconds detects fired timer and completes workflow)
let private RegisterTimerExample() =
    let workflowId = "FlowSharp Timer Example"

    let decider(dt:DecisionTask) =
        FlowSharp(dt) {
            let! timer = FlowSharpAction.StartTimer("Some Timer", "15")

            do! FlowSharpAction.WaitForTimer(timer)

            // Complete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, None)
    AddOperation (Command.StartWorkflow("t1")) start
    AddOperation (Command.DecisionTask("t1")) (Operation.DecisionTask(decider, false, None))

// Example t2 : Restart timer example
//      This example demonstracts waiting for a timer three times.
// To Run, start the project and type these commands into the command line interpreter.
//    sw t2             (Starts the workflow)
//    dt t2             (Processes the initial decision task, starts the timer and waits)
//    dt t2             (Processes a decision task, waits for timer to time out and resets it)
//    dt t2             (Processes a decision task, waits for timer to time out and resets it)
//    dt t2             (Processes the final decision task, after 15 seconds detects fired timer and completes workflow)
let private RegisterRestartTimerExample() =
    let workflowId = "FlowSharp Restart Timer Example"

    let decider(dt:DecisionTask) =
        FlowSharp(dt) {
            for i = 1 to 3 do
                // Once a timer name has been used, it cannot be used again.
                // Append a unique value to the new timer name each time.
                let! timer = FlowSharpAction.StartTimer("Some Timer " + (i.ToString()), "15")

                // Wait for the timer to complete
                do! FlowSharpAction.WaitForTimer(timer)

                // Unit value required for 'for' loop
                ()

            // Complete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, None)
    AddOperation (Command.StartWorkflow("t2")) start
    AddOperation (Command.DecisionTask("t2")) (Operation.DecisionTask(decider, false, None))

let Register() =
    RegisterTimerExample()
    RegisterRestartTimerExample()
