namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

type FlowSharpAction = 
    /// <summary>Determines if a request to cancel this workflow execution was made.</summary>
    /// <returns>A WorkflowExecutionCancelRequestedResult of CancelRequested or NotRequested.</returns>
    static member WorkflowExecutionCancelRequested() =
        WorkflowExecutionCancelRequestedAction.Attributes()

    /// <summary>Determines the input provided to the workflow execution (if any).</summary>
    /// <returns>The workflow execution input or null.</returns>
    static member GetWorkflowExecutionInput() =
        GetWorkflowExecutionInputAction.Attributes()

    /// <summary>Returns the ExecutionContext from the most recent DecisionTaskCompleted event.</summary>
    /// <returns>The Execution Context or null.</returns>
    static member GetExecutionContext() =
        GetExecutionContextAction.Attributes()

    /// <summary>Sets the ExecutionContext for the next RespondDecisionTaskCompletedReqest.</summary>
    static member SetExecutionContext(executionContext:string) =
        SetExecutionContextAction.Attributes(executionContext)

    static member Wait() =
        WaitAction.Attributes()

    static member RemoveFromContext(action:ScheduleActivityTaskAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.ScheduleActivityTask(attr)

    static member RemoveFromContext(action:ScheduleLambdaFunctionAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.ScheduleLambdaFunction(attr)

    static member RemoveFromContext(action:StartChildWorkflowExecutionAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.StartChildWorkflowExecution(attr)

    static member RemoveFromContext(action:StartTimerAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.StartTimer(attr)

    static member RemoveFromContext(action:WorkflowExecutionSignaledAction) =
        let signalName = action.GetAttributes()
        RemoveFromContextAction.WorkflowExecutionSignaled(signalName)

    static member RemoveFromContext(action:SignalExternalWorkflowExecutionAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.SignalExternalWorkflowExecution(attr)
        
    static member RemoveFromContext(action:RecordMarkerAction) =
        let attr = action.GetAttributes()
        RemoveFromContextAction.RecordMarker(attr)

    static member RemoveFromContext(action:MarkerRecordedAction) =
        let markerName = action.GetAttributes()
        RemoveFromContextAction.MarkerRecorded(markerName)


    /// <summary>Schedules an Activity Task but does not block further progress.</summary>
    /// <param name="activityType">Required. The type of the activity task to schedule.</param>
    /// <param name="activityId">Required. The activityId of the activity task.
    ///                          The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="input">The input provided to the activity task.</param>
    /// <param name="heartbeatTimeout">If set, specifies the maximum time before which a worker processing a task of this type must report progress by calling RecordActivityTaskHeartbeat. If the timeout is exceeded, the activity task is automatically timed out. If the worker subsequently attempts to record a heartbeat or returns a result, it will be ignored. This overrides the default heartbeat timeout specified when registering the activity type using RegisterActivityType.
    ///                                The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.</param>
    /// <param name="scheduleToCloseTimeout">The maximum duration for this activity task.
    ///                                      The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.
    ///                                      A schedule-to-close timeout for this activity task must be specified either as a default for the activity type or through this field. If neither this field is set nor a default schedule-to-close timeout was specified at registration time then a fault will be returned.</param>
    /// <param name="scheduleToStartTimeout">Optional. If set, specifies the maximum duration the activity task can wait to be assigned to a worker. This overrides the default schedule-to-start timeout specified when registering the activity type using RegisterActivityType.
    ///                                      The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.
    ///                                      A schedule-to-start timeout for this activity task must be specified either as a default for the activity type or through this field. If neither this field is set nor a default schedule-to-start timeout was specified at registration time then a fault will be returned.</param>
    /// <param name="startToCloseTimeout">If set, specifies the maximum duration a worker may take to process this activity task. This overrides the default start-to-close timeout specified when registering the activity type using RegisterActivityType.
    ///                                   The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.
    ///                                   A start-to-close timeout for this activity task must be specified either as a default for the activity type or through this field. If neither this field is set nor a default start-to-close timeout was specified at registration time then a fault will be returned.</param>
    /// <param name="taskList">If set, specifies the name of the task list in which to schedule the activity task. If not specified, the defaultTaskList registered with the activity type will be used.
    ///                        A task list for this activity task must be specified either as a default for the activity type or through this field. If neither this field is set nor a default task list was specified at registration time then a fault will be returned.
    ///                        The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="taskPriority">Optional. If set, specifies the priority with which the activity task is to be assigned to a worker. This overrides the defaultTaskPriority specified when registering the activity type using RegisterActivityType. Valid values are integers that range from Java's Integer.MIN_VALUE (-2147483648) to Integer.MAX_VALUE (2147483647). Higher numbers indicate higher priority.</param>
    /// <returns>A ScheduleActivityTaskResult of Scheduling, Scheduled, Started, Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member ScheduleActivityTask(activity:ActivityType, activityId:string, ?input:string, ?heartbeatTimeout:string, ?scheduleToCloseTimeout:string, ?scheduleToStartTimeout:string, ?startToCloseTimeout:string, ?taskList:TaskList, ?taskPriority:string, ?pushToContext:bool) =
        let attr = new ScheduleActivityTaskDecisionAttributes()
        attr.ActivityType <- activity
        attr.ActivityId <- activityId
        attr.HeartbeatTimeout <- if heartbeatTimeout.IsSome then heartbeatTimeout.Value else null
        attr.Input <- if input.IsSome then input.Value else null
        attr.ScheduleToCloseTimeout <- if scheduleToCloseTimeout.IsSome then scheduleToCloseTimeout.Value else null
        attr.ScheduleToStartTimeout <- if scheduleToStartTimeout.IsSome then scheduleToStartTimeout.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value else null

        ScheduleActivityTaskAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Waits for an Activity Task and blocks further progress until the Activity Task has been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="schedule">Required. The result of a previous StartActivityTask call.</param>
    /// <returns>A ScheduleActivityTaskResult of Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member WaitForActivityTask(schedule:ScheduleActivityTaskResult) =
        WaitForActivityTaskAction.ScheduleResult(schedule)

    /// <summary>Waits for a list of Activity Tasks and blocks further progress until any Activity Task has been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="scheduledList">Required. A list of results from previous StartActivityTask calls.</param>
    /// <returns>A list of ScheduleActivityTaskResult with atleast one of Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member WaitForAnyActivityTask(scheduledList:ScheduleActivityTaskResult list) =
        WaitForAnyActivityTaskAction.ScheduleResults(scheduledList)

    /// <summary>Waits for a list of Activity Tasks and blocks further progress until all Activity Task have been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="scheduledList">Required. A list of results from previous StartActivityTask calls.</param>
    /// <returns>A list of ScheduleActivityTaskResult with all of Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member WaitForAllActivityTask(scheduledList:ScheduleActivityTaskResult list) =
        WaitForAllActivityTaskAction.ScheduleResults(scheduledList)

    /// <summary>Attempts to cancel a previously scheduled activity task. If the activity task was scheduled but has not been assigned to a worker, then it will be canceled. If the activity task was already assigned to a worker, then the worker will be informed that cancellation has been requested in the response to RecordActivityTaskHeartbeat.</summary>
    /// <param name="schedule">Required. The result of a previous StartActivityTask call.</param>
    /// <returns>A RequestCancelActivityTaskResult of CancelRequested, RequestCancelFailed, Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member RequestCancelActivityTask(schedule:ScheduleActivityTaskResult) =
        RequestCancelActivityTaskAction.ScheduleResult(schedule)

    /// <summary>Schedules an AWS Lambda Function.</summary>
    /// <param name="id">Required. The SWF id of the AWS Lambda task.
    ///                  The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="name">Required. The name of the AWS Lambda function to invoke.</param>
    /// <param name="input">The input provided to the AWS Lambda function.</param>
    /// <param name="startToCloseTimeout">If set, specifies the maximum duration the function may take to execute.</param>
    /// <returns>A ScheduleLambdaFunctionResult of Scheduling, Scheduled, Started, Completed, TimedOut, Failed, StartFailed, or ScheduleFailed.</returns>
    static member ScheduleLambdaFunction(id:string, name:string, ?input:string, ?startToCloseTimeout:string, ?pushToContext:bool) =
        let attr = new ScheduleLambdaFunctionDecisionAttributes()
        attr.Id <- id
        attr.Input <- if input.IsSome then input.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value else null
        attr.Name <- name

        ScheduleLambdaFunctionAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Waits for a Lambda Function and blocks further progress until the Lambda Function has been Completed, Canceled, TimedOut, Failed, StartFailed, or ScheduleFailed.</summary>
    /// <param name="schedule">Required. The result of a previous StartActivityTask call.</param>
    /// <returns>A ScheduleLambdaFunctionResult of Completed, Canceled, TimedOut, Failed, StartFailed, or ScheduleFailed.</returns>
    static member WaitForLambdaFunction(schedule:ScheduleLambdaFunctionResult) =
        WaitForLambdaFunctionAction.ScheduleResult(schedule)

    /// <summary>Waits for a list of Lambda Functions and blocks further progress until any Lambda Functions has been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="scheduledList">Required. A list of results from previous StartLambdaFunction calls.</param>
    /// <returns>A list of ScheduleLambdaFunctionResult with atleast one of Completed, Canceled, TimedOut, Failed, StartFailed, or ScheduleFailed.</returns>
    static member WaitForAnyLambdaFunction(scheduledList:ScheduleLambdaFunctionResult list) =
        WaitForAnyLambdaFunctionAction.ScheduleResults(scheduledList)

    /// <summary>Waits for a list of Lambda Functions and blocks further progress until all Lambda Functions have been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="scheduledList">Required. A list of results from previous StartLambdaFunction calls.</param>
    /// <returns>A list of ScheduleLambdaFunctionResult with all of Completed, Canceled, TimedOut, Failed, StartFailed, or ScheduleFailed.</returns>
    static member WaitForAllLambdaFunction(scheduledList:ScheduleLambdaFunctionResult list) =
        WaitForAllLambdaFunctionAction.ScheduleResults(scheduledList)

    /// <summary>Requests that a child workflow execution be started and records a StartChildWorkflowExecutionInitiated event in the history. The child workflow execution is a separate workflow execution with its own history.</summary>
    /// <param name="workflowType">Required. The type of the workflow execution to be started.</param>
    /// <param name="workflowId">Required. The workflowId of the workflow execution.
    ///                          The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="input">The input to be provided to the workflow execution.</param>
    /// <param name="childPolicy">Optional. If set, specifies the policy to use for the child workflow executions if the workflow execution being started is terminated by calling the TerminateWorkflowExecution action explicitly or due to an expired timeout. This policy overrides the default child policy specified when registering the workflow type using RegisterWorkflowType.</param>
    /// <param name="lambdaRole">The ARN of an IAM role that authorizes Amazon SWF to invoke AWS Lambda functions.
    ///                          In order for this workflow execution to invoke AWS Lambda functions, an appropriate IAM role must be specified either as a default for the workflow type or through this field.</param>
    /// <param name="tagList">The list of tags to associate with the child workflow execution. A maximum of 5 tags can be specified. You can list workflow executions with a specific tag by calling ListOpenWorkflowExecutions or ListClosedWorkflowExecutions and specifying a TagFilter.</param>
    /// <param name="taskList">The name of the task list to be used for decision tasks of the child workflow execution.
    ///                        A task list for this workflow execution must be specified either as a default for the workflow type or through this parameter. If neither this parameter is set nor a default task list was specified at registration time then a fault will be returned.
    ///                        The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="taskPriority">Optional. A task priority that, if set, specifies the priority for a decision task of this workflow execution. This overrides the defaultTaskPriority specified when registering the workflow type. Valid values are integers that range from Java's Integer.MIN_VALUE (-2147483648) to Integer.MAX_VALUE (2147483647). Higher numbers indicate higher priority.</param>
    /// <param name="executionStartToCloseTimeout">The total duration for this workflow execution. This overrides the defaultExecutionStartToCloseTimeout specified when registering the workflow type.
    ///                                            The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.
    ///                                            An execution start-to-close timeout for this workflow execution must be specified either as a default for the workflow type or through this parameter. If neither this parameter is set nor a default execution start-to-close timeout was specified at registration time then a fault will be returned.</param>
    /// <param name="taskStartToCloseTimeout">Specifies the maximum duration of decision tasks for this workflow execution. This parameter overrides the defaultTaskStartToCloseTimout specified when registering the workflow type using RegisterWorkflowType.
    ///                                       The duration is specified in seconds; an integer greater than or equal to 0. The value "NONE" can be used to specify unlimited duration.
    ///                                       A task start-to-close timeout for this workflow execution must be specified either as a default for the workflow type or through this parameter. If neither this parameter is set nor a default task start-to-close timeout was specified at registration time then a fault will be returned.</param>
    /// <returns>A StartChildWorkflowExecutionResult of Scheduling, Initiated, Started, or StartFailed.</returns>
    static member StartChildWorkflowExecution 
        (
        workflowType:WorkflowType,
        workflowId:string,
        ?input:string,
        ?childPolicy:ChildPolicy,
        ?lambdaRole:string,
        ?tagList:System.Collections.Generic.List<System.String>,
        ?taskList:TaskList,
        ?taskPriority:int,
        ?executionStartToCloseTimeout:string,
        ?taskStartToCloseTimeout:string,
        ?pushToContext:bool
        ) =
        let attr = new StartChildWorkflowExecutionDecisionAttributes();
        attr.ChildPolicy <- if childPolicy.IsSome then childPolicy.Value else null
        attr.ExecutionStartToCloseTimeout <- if executionStartToCloseTimeout.IsSome then executionStartToCloseTimeout.Value else null
        attr.Input <- if input.IsSome then input.Value else null
        attr.LambdaRole <- if lambdaRole.IsSome then lambdaRole.Value else null
        attr.TagList <- if tagList.IsSome then tagList.Value else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value.ToString() else null
        attr.TaskStartToCloseTimeout <- if taskStartToCloseTimeout.IsSome then taskStartToCloseTimeout.Value else null
        attr.WorkflowId <- workflowId
        attr.WorkflowType <- workflowType

        StartChildWorkflowExecutionAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Waits for a previously started child workflow execution and blocks further progress until the child workflow execution has been Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</summary>
    /// <param name="start">Required. The result of a previous StartChildWorkflowExecution call.</param>
    /// <returns>A WaitForChildWorkflowExecutionResult of Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</returns>
    static member WaitForChildWorkflowExecution(start:StartChildWorkflowExecutionResult) =
        WaitForChildWorkflowExecutionAction.StartResult(start)

    /// <summary>Waits for a list of previously started child workflow execution and blocks further progress until atleast one child workflow execution has been Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</summary>
    /// <param name="startList">Required. A list of results of a previous StartChildWorkflowExecution calls.</param>
    /// <returns>A StartChildWorkflowExecutionResult of Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</returns>
    static member WaitForAnyChildWorkflowExecution(startList:StartChildWorkflowExecutionResult list) =
        WaitForAnyChildWorkflowExecutionAction.StartResults(startList)

    /// <summary>Waits for a list of previously started child workflow execution and blocks further progress until atleast all child workflow executions have Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</summary>
    /// <param name="startList">Required. A list of results of a previous StartChildWorkflowExecution calls.</param>
    /// <returns>A StartChildWorkflowExecutionResult of Completed, Canceled, TimedOut, Failed, Terminated, or StartFailed.</returns>
    static member WaitForAllChildWorkflowExecution(startList:StartChildWorkflowExecutionResult list) =
        WaitForAllChildWorkflowExecutionAction.StartResults(startList)

    /// <summary>Requests that a request be made to cancel the specified external workflow execution and records a RequestCancelExternalWorkflowExecutionInitiated event in the history.</summary>
    /// <param name="workflowId">Required. The workflowId of the external workflow execution to cancel.</param>
    /// <param name="runId">Optional. The runId of the external workflow execution to cancel.</param>
    /// <returns>A RequestCancelExternalWorkflowExecutionResult of Requesting, Initiated, Delivered or Failed.</returns>
    static member RequestCancelExternalWorkflowExecution (workflowId:string, ?runId:string) =
        let attr = new RequestCancelExternalWorkflowExecutionDecisionAttributes()
        attr.RunId <- if runId.IsSome then runId.Value else null
        attr.WorkflowId <- workflowId

        RequestCancelExternalWorkflowExecutionAction.Attributes(attr)

    /// <summary>Starts a timer for this workflow execution and records a TimerStarted event in the history. This timer will fire after the specified delay and record a TimerFired event.</summary>
    /// <param name="timerId">Required. The unique ID of the timer.
    ///                       The specified string must not start or end with whitespace. It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). Also, it must not contain the literal string quotarnquot.</param>
    /// <param name="startToFireTimeout">Required. The duration to wait before firing the timer.
    ///                                  The duration is specified in seconds; an integer greater than or equal to 0.</param>
    /// <returns>A StartTimerResult of Starting, Started, or StartTimerFailed.</returns>
    static member StartTimer(timerId:string, startToFireTimeout:string, ?pushToContext:bool) =
        let attr = new StartTimerDecisionAttributes()
        attr.TimerId <- timerId        
        attr.StartToFireTimeout <- startToFireTimeout

        StartTimerAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Waits for a previously started timer and blocks further progress until the timer has been Canceled, Fired, or StartTimerFailed.</summary>
    /// <param name="start">Required. The result of a previous StartTimer call.</param>
    /// <returns>A WaitForTimerResult of Canceled, Fired, or StartTimerFailed.</returns>
    static member WaitForTimer(start:StartTimerResult) =
        WaitForTimerAction.StartResult(start)

    /// <summary>Cancels a previously started timer and records a TimerCanceled event in the history.</summary>
    /// <param name="start">Required. The result of a previous StartTimer call.</param>
    /// <returns>A CancelTimerResult of Canceling, Fired, Canceled, Fired, NotStarted, or CancelTimerFailed.</returns>
    static member CancelTimer(start:StartTimerResult) =
        CancelTimerAction.StartResult(start)

    /// <summary>Determines if an external signal was received for the workflow execution.</summary>
    /// <param name="signalName">The name of the signal received. The decider can use the signal name and inputs to determine how to the process the signal.</param>
    /// <returns>A WorkflowExecutionSignaledResult of Signaled, or NotSignaled.</returns>
    static member WorkflowExecutionSignaled(signalName:string, ?pushToContext:bool) =
        WorkflowExecutionSignaledAction.Attributes(signalName, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Blocks further processing until an external signal was received for the workflow execution.</summary>
    /// <param name="signalName">The name of the signal received. The decider can use the signal name and inputs to determine how to the process the signal.</param>
    /// <returns>A WaitForWorkflowExecutionSignaledResult of Signaled.</returns>
    static member WaitForWorkflowExecutionSignaled(signalName:string, ?pushToContext:bool) =
        WaitForWorkflowExecutionSignaledAction.Attributes(signalName, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Requests a signal to be delivered to the specified external workflow execution and records a SignalExternalWorkflowExecutionInitiated event in the history.</summary>
    /// <param name="signalName">Required. The name of the signal.The target workflow execution will use the signal name and input to process the signal.</param>
    /// <param name="workflowId">Required. The workflowId of the workflow execution to be signaled.</param>
    /// <param name="input">Optional. Input data to be provided with the signal. The target workflow execution will use the signal name and input data to process the signal.</param>
    /// <param name="runId">Optional. The runId of the workflow execution to be signaled.</param>
    /// <returns>A WorkflowExecutionSignaledResult of Signaled or NotSignaled.</returns>
    static member SignalExternalWorkflowExecution(signalName:string, workflowId:string, ?input:string, ?runId:string, ?pushToContext:bool) =
        let attr = new SignalExternalWorkflowExecutionDecisionAttributes(SignalName=signalName, WorkflowId=workflowId);
        attr.Input <- if input.IsSome then input.Value else null
        attr.RunId <- if runId.IsSome then runId.Value else null

        SignalExternalWorkflowExecutionAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Determines if a marker was recorded in the workflow history as the result of a RecordMarker decision.</summary>
    /// <param name="markerName">Required. The name of the marker.</param>
    /// <returns>A MarkerRecordedResult of MarkerRecorded, NotRecorded, or RecordMarkerFailed.</returns>
    static member MarkerRecorded(markerName:string, ?pushToContext:bool) =
        MarkerRecordedAction.Attributes(markerName, (pushToContext.IsSome && pushToContext.Value))

    /// <summary>Records a MarkerRecorded event in the history. Markers can be used for adding custom information in the history for instance to let deciders know that they do not need to look at the history beyond the marker event.</summary>
    /// <param name="markerName">Required. The name of the marker.</param>
    /// <param name="details">Optional. details of the marker.</param>
    /// <returns>A RecordMarkerResult of Recording, MarkerRecorded, or RecordMarkerFailed.</returns>
    static member RecordMarker(markerName:string, ?details:string, ?pushToContext:bool) =
        let attr = new RecordMarkerDecisionAttributes(MarkerName=markerName);
        attr.Details <- if details.IsSome then details.Value else null

        RecordMarkerAction.Attributes(attr, (pushToContext.IsSome && pushToContext.Value))

