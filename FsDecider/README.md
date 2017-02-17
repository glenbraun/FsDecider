# FlowSharp
F# computation expression builder for AWS SimpleWorkflow.

Use the FlowSharp library for AWS SimpleWorkflow to write deciders using F# computation expressions.

    let decider(dt:DecisionTask) =
        FlowSharp.Builder(dt) {
            // Schedule activity tasks
            let! activity = FlowSharp.ScheduleActivityTask("ActivityType", "id1")

            // Wait for activities to complete
            do! FlowSharp.WaitForActivityTask(activity)

            // Support for: child workflows, lambda functions, timers, signals, markers, and more.
        }

    let poll = swf.PollForDecisionTask(request)
    let respond = decider(poll.DecisionTask)
    swf.RespondDecisionTaskCompleted(respond)

# Design goals
## Focus on decider logic. 
A SWF solution is composed of code which registers workflow metadata, starts workflows, processes activities (workers), and makes control flow logic decisions (deciders). Of these, writing the decider logic is by far the most complicated.

The FlowSharp decider builder uses F# computation expressions to capture the sequence of steps which define the logic of a workflow. FlowSharp deciders look like a single unit of code but they are executed over a sequence of independent decision loops, perhaps spread across many machines. Each loop iteration is picked up from where the last left off by searching the workflow history provided by SWF.
		
## Expose the AWS .NET SDK
The FlowSharp uses the classes of the AWS .NET SDK for SimpleWorkflow.
		
# Features
* Schedule and wait for activity tasks. Provides full for support for input, timeouts, task lists, and task priority and all activity task results of completed, canceled, failed, and timed out. Activity tasks can be executed in series and in parallel and supports logic to wait for some or all of them to complete. Cancel activity tasks with a request to cancel.
* Start child workflows and wait for them to complete. Full support for SWF features of child policy, timeouts, input, task lists, and task priority. Child workflows can return as completed, canceled, failed, timed out, or terminated. Child workflows can be canceled with a request to cancel. Execute multiple child workflows in series or parallel and wait for some or all of them to complete.
* Run Lambda functions from workflows with support for normal completion with return values, as well as failed or timed out Lambda functions.
* Start timers and wait for them to fire. Also cancel timers.
* Send signals to external workflow executions and handle signals being sent into a workflow execution.
* Record markers at significant spots in a workflow execution and detect which markers have been recorded.
* Support for workflow execution context to reduce the size of the list of history events needed to make decisions. Decider logic can be written which only requires the history events since the previous decision task.
