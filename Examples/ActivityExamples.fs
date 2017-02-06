module FlowSharp.Examples.ActivityExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter


// Example a1 : Activities in series
//      This example demonstrates a simple FlowSharp decider for a workflow with two 
//      activity tasks. Once the first activity task completes, the second is started. 
//      The workflow execution completes when after the second activity task completes.
// To Run, start the project and type these commands into the command line interpreter.
//    sw a1
//    dt a1
//    dt a1
//    dt a1
let RegisterActivitiesInSeries() =
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt, false) {
            // Schedule the first activity task
            let! first = FlowSharp.ScheduleActivityTask (
                                ExamplesConfiguration.ActivityType, 
                                "First Activity")
            
            // Wait for the first activity task to finish
            do! FlowSharp.WaitForActivityTask(first)

            // Schedule the second activity task
            let! second = FlowSharp.ScheduleActivityTask (
                                ExamplesConfiguration.ActivityType, 
                                "Second Activity")

            // Wait for the second activity task to finish
            do! FlowSharp.WaitForActivityTask(second)

            // FlowSharp.WaitForAllActivityTask can be used to wait for a list of activity tasks to finish.
            // FlowSharp.WaitForAnyActivityTask can be used to wait for any of a list of activity tasks to finish.

            // Comlete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(ExamplesConfiguration.WorkflowType, "Activities in series example", None, None)
    AddOperation (Command.StartWorkflow("a1")) start
    AddOperation (Command.DecisionTask("a1")) (Operation.DecisionTask(decider, None))


