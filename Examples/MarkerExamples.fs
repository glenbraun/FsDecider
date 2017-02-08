module FlowSharp.Examples.MarkerExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example m1 : Recording markers
//      This example demonstrates recording and detecting a marker.
// To Run, start the project and type these commands into the command line interpreter.
//    sw m1             (Starts the workflow)
//    dt m1             (Processes the initial decision task, records marker and waits)
//    sg m1             (Sends a signal to the workflow to force a decision task)
//    dt m1             (Processes the final decision task, detects marker and completes workflow)
let private RegisterRecordAndDetectMarker() =
    let workflowId = "FlowSharp Markers Example"

    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            do! FlowSharp.RecordMarker("Some Marker")

            let! marker = FlowSharp.MarkerRecorded("Some Marker")

            match marker with
            | MarkerRecordedResult.Recorded(attr) ->
                // Complete the workflow execution with a result of "OK"
                return "OK"
            | _ -> 
                do! FlowSharp.Wait()
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, None)
    AddOperation (Command.StartWorkflow("m1")) start
    AddOperation (Command.SignalWorkflow("m1")) (Operation.SignalWorkflow(workflowId, "Some Signal"))
    AddOperation (Command.DecisionTask("m1")) (Operation.DecisionTask(decider, false, None))

let Register() =
    RegisterRecordAndDetectMarker()
