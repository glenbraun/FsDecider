module FlowSharp.Examples.ContextExamples

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp
open FlowSharp.Actions
open FlowSharp.ExecutionContext
open FlowSharp.Examples.CommandInterpreter
open FlowSharp.UnitTests

// Example x1 : Context example
//      This example demonstrates the use of the FlowSharp ExecutionContextManager. Without a context 
//      manager, FlowSharp uses the workflow execution history to determine what decisions to make.
//      However, the execution history can be very long if you consider a workflow which might run thousands of
//      activity tasks, for example. Determining the state of each FlowSharp action might not be necessary
//      to support the logic of the workflow. In these cases, an IContextManager can be used to retrieve the
//      state from another source. FlowSharp contains an implementation of the IContextManager interface that
//      uses the ExecutionContext property provided by the RespondDecisionTaskCompletedRequest
//      and the DecisionTaskCompletedEventAttributes types. Other possible implementations could use files
//      stored in S3, a DynamoDB table, etc.
// To Run, start the project and type these commands into the command line interpreter.
//    sw x1             (Starts the workflow)
//    dt x1             (Processes the first decision task, schedules the activity tasks)
//    at x1             (Completes the first activity task)
//    at x1             (Completes the second activity task)
//    dt x1             (Processes a decision task, gets activity task state from history this time but stores it in context, then records marker)
//    sg x1             (Sends a signal to the workflow to trigger a decision task)
//    dt x1             (Processes the final decistion task, activity results retrieved from context, not history.)
let private RegisterContextExample() =
    let workflowId = "FlowSharp Context Example"

    let decider(dt:DecisionTask) =
        // The FlowSharp ExecutionContextManager stores context in the ExecutionContext property 
        // of the DecisionTaskCompletedEventAttributes type.
        let context = ExecutionContextManager()

        // Construct the FlowSharp computation expression with ReverseOrder=true, and passing in the context manager.
        // Using ReverseOrder, the decider could retrieve only the latest history events since the last decision
        // task.
        FlowSharp.Builder(dt, true, Some(context :> IContextManager) ) {
            let! marker = FlowSharp.MarkerRecorded("Marker 1", pushToContext=true)

            // Notice "let" not "let!" here. Binding contextActivityAction so both branches of the match
            // statement below can access it. 
            let contextActivityAction = FlowSharp.ScheduleActivityTask(TestConfiguration.ActivityType, "Context Activity", pushToContext=true)

            match marker with
            | MarkerRecordedResult.NotRecorded ->
                // Notice "let!" used here. This will schedule the activity and store the state using the context manager.
                let! contextActivity = contextActivityAction

                // This activity is not stored in context. It's state cannot be determined without all the
                // execution history events.
                let! noContextActivity = FlowSharp.ScheduleActivityTask(TestConfiguration.ActivityType, "No Context Activity")

                // Wait for both activity tasks to complete.
                do! FlowSharp.WaitForAllActivityTask([contextActivity; noContextActivity;])

                // Record a marker and store in context.
                do! FlowSharp.RecordMarker("Marker 1", pushToContext=true)

                // Keep the workflow active using Wait.
                do! FlowSharp.Wait()                

            | MarkerRecordedResult.Recorded(_) ->
                // Notice "let!" used here. This will retrieve the activity task result from the context manager
                // rather than the execution history.
                let! contextActivity = contextActivityAction

                match contextActivity with
                | ScheduleActivityTaskResult.Completed(attr) ->
                    return ("Activity task returned: " + attr.Result)
                | _ ->
                    failwith "Unexpected state of activity task."

                ()
            | _ ->
                failwith "Unexpected state of marker."

        }

    // The code below supports the example runner
    AddOperation (Command.StartWorkflow("x1")) (Operation.StartWorkflowExecution(TestConfiguration.WorkflowType, workflowId, None, None))
    AddOperation (Command.ActivityTask("x1")) (Operation.ActivityTask(TestConfiguration.ActivityType, Some(fun at -> "'Hello from activity task'"), None))
    AddOperation (Command.SignalWorkflow("x1")) (Operation.SignalWorkflow(workflowId, "Some Signal"))
    AddOperation (Command.DecisionTask("x1")) (Operation.DecisionTask(decider, true, None))

let Register() =
    RegisterContextExample()

