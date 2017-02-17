module FsDecider.Examples.TimerExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FsDecider
open FsDecider.Actions
open FsDecider.Examples.CommandInterpreter
open FsDecider.UnitTests

// Example t1 : Start a timer and wait for it to fire
//      This example demonstracts starting a timer and then waiting for it to fire.
// To Run, start the project and type these commands into the command line interpreter.
//    sw t1             (Starts the workflow)
//    dt t1             (Processes the initial decision task, starts the timer and waits)
//    dt t1             (Processes the final decision task, after 15 seconds detects fired timer and completes workflow)
let private LoadTimerExample() =
    let workflowId = "FsDecider Timer Example"

    let decider(dt:DecisionTask) =
        Decider(dt) {
            let! timer = FsDeciderAction.StartTimer("Some Timer", "15")

            do! FsDeciderAction.WaitForTimer(timer)

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
let private LoadRestartTimerExample() =
    let workflowId = "FsDecider Restart Timer Example"

    let decider(dt:DecisionTask) =
        Decider(dt) {
            for i = 1 to 3 do
                // Once a timer name has been used, it cannot be used again.
                // Append a unique value to the new timer name each time.
                let! timer = FsDeciderAction.StartTimer("Some Timer " + (i.ToString()), "15")

                // Wait for the timer to complete
                do! FsDeciderAction.WaitForTimer(timer)

                // Unit value required for 'for' loop
                ()

            // Complete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, None)
    AddOperation (Command.StartWorkflow("t2")) start
    AddOperation (Command.DecisionTask("t2")) (Operation.DecisionTask(decider, false, None))

let Load() =
    LoadTimerExample()
    LoadRestartTimerExample()
