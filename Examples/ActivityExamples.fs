module FlowSharp.Examples.ActivityExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example a1 : Activities in series
//      This example demonstrates a simple FlowSharp decider for a workflow with two 
//      activity tasks. Once the first activity task completes, the second is started. 
//      The workflow execution completes when after the second activity task completes.
// To Run, start the project and type these commands into the command line interpreter.
//    sw a1             (Starts the workflow)
//    dt a1             (Processes the initial decision task, schedules first activity task)
//    at a1             (Completes the first activity task)
//    dt a1             (Processes the second decision task, schedules second activity task)
//    at a1             (Completes the second activity task)
//    dt a1             (Processes the final decision task, completes the workflow)
let private RegisterActivitiesInSeries() =
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            // Schedule the first activity task
            let! first = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                "First Activity")
            
            // Wait for the first activity task to finish
            do! FlowSharp.WaitForActivityTask(first)

            // Schedule the second activity task
            let! second = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                "Second Activity")

            // Wait for the second activity task to finish
            do! FlowSharp.WaitForActivityTask(second)

            // FlowSharp.WaitForAllActivityTask can be used to wait for a list of activity tasks to finish.
            // FlowSharp.WaitForAnyActivityTask can be used to wait for any of a list of activity tasks to finish.

            // Complete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, "Activities in series example", None, None)
    AddOperation (Command.StartWorkflow("a1")) start
    AddOperation (Command.ActivityTask("a1")) (Operation.ActivityTask(TestConfiguration.ActivityType, None, None))
    AddOperation (Command.DecisionTask("a1")) (Operation.DecisionTask(decider, None))

// Example a2 : Activities in parallel
//      This example demonstrates a simple FlowSharp decider for a workflow with two 
//      activity tasks. The activity tasks run in parallel. The workflow completes when both
//      activity tasks have completed.
// To Run, start the project and type these commands into the command line interpreter.
//    sw a2             (Starts the workflow)
//    dt a2             (Processes the initial decision task, schedules two activity tasks)
//    at a2             (Completes one activity task)
//    at a2             (Completes the other activity task)
//    dt a2             (Processes the final decision task, completes the workflow)
let private RegisterActivitiesInParallel() =
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            // Schedule the first activity task
            let! first = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                "First Activity")
            
            // Schedule the second activity task
            let! second = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                "Second Activity")

            // Wait for both of the activity tasks to finish
            do! FlowSharp.WaitForAllActivityTask([first; second;])

            // Complete the workflow execution with a result of "OK"
            return "OK"
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, "Activities in parallel example", None, None)
    AddOperation (Command.StartWorkflow("a2")) start
    AddOperation (Command.ActivityTask("a2")) (Operation.ActivityTask(TestConfiguration.ActivityType, None, None))
    AddOperation (Command.DecisionTask("a2")) (Operation.DecisionTask(decider, None))

// Example a3 : Conditional logic based on the status of an activity task
//      This example demonstrates a simple FlowSharp decider for a workflow with one 
//      activity task. The activity task has a short timeout (10 seconds), once it times out the 
//      workflow execution completes.
// To Run, start the project and type these commands into the command line interpreter.
//    sw a3         (Starts the workflow)
//    dt a3         (Processes the initial decision task, schedules the activity task)
//    dt a3         (Processes the final decision task, at 10 seconds after previous step activity will timeout)
let private RegisterActivityStatus() =
    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            // Schedule the activity task
            let! activity = FlowSharp.ScheduleActivityTask (
                                TestConfiguration.ActivityType, 
                                "Activity Example",  
                                scheduleToStartTimeout="10")
            
            match activity with
            | ScheduleActivityTaskResult.TimedOut(attr) -> 
                // Complete the workflow execution with a result of "OK"
                return "OK"
            | _ -> 
                do! FlowSharp.Wait()
        }

    // The code below supports the example runner
    let start = Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, "Activities status example", None, None)
    AddOperation (Command.StartWorkflow("a3")) start
    AddOperation (Command.DecisionTask("a3")) (Operation.DecisionTask(decider, None))

let Register() =
    RegisterActivitiesInSeries()
    RegisterActivitiesInParallel()
    RegisterActivityStatus()
