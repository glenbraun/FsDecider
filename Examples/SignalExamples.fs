module FlowSharp.Examples.SignalExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example s1 : Sending and receiving signals
//      This example has two workflows. One which sends a signal and the other which receives it.
// To Run, start the project and type these commands into the command line interpreter.
//    sw s1r            (Starts the receiving workflow)
//    dt s1r            (Processes the receiving workflow initial decision task, nothing to do but wait)
//    sw s1s            (Starts the sending workflow)
//    dt s1s            (Processes the sending workflow initial decision task, sends signal and waits)
//    dt s1r            (Processes the receiving workflow, gets signal and completes)
//    dt s1s            (Processes the sending workflow initial decision task, sends signal and completes)
let private RegisterSendingAndReceivingSignals() =
    let receivingWorkflowId = "FlowSharp Signals Example (receiver)"
    let sendingWorkflowId = "FlowSharp Signals Example (sender)"

    let sendSignalDecider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            // Send a signal to an external workflow
            let! signal = FlowSharpAction.SignalExternalWorkflowExecution("Some Signal", receivingWorkflowId)

            match signal with
            | SignalExternalWorkflowExecutionResult.Signaled(_) ->
                // Complete the workflow execution with a result of "OK"
                return "OK"
            | _ -> 
                do! FlowSharpAction.Wait()
        }

    let receiveSignalDecider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            do! FlowSharpAction.WaitForWorkflowExecutionSignaled("Some Signal")

            return "OK"
        }

    // The code below supports the example runner
    let startReceiver = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, receivingWorkflowId, None, None)
    let startSender = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, sendingWorkflowId, None, None)
    AddOperation (Command.StartWorkflow("s1r")) startReceiver
    AddOperation (Command.StartWorkflow("s1s")) startSender
    AddOperation (Command.DecisionTask("s1r")) (Operation.DecisionTask(receiveSignalDecider, false, None))
    AddOperation (Command.DecisionTask("s1s")) (Operation.DecisionTask(sendSignalDecider, false, None))

let Register() =
    RegisterSendingAndReceivingSignals()
