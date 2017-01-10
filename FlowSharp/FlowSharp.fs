namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

type FlowSharp = 
    /// <summary>Start an Activity Task and block further progress until the Activity Task has been Completed, Canceled, TimedOut, or Failed.</summary>
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
    /// <returns>A WaitForActivityTaskResult of Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member StartAndWaitForActivityTask(activityType:ActivityType, activityId:string, ?input:string, ?heartbeatTimeout:string, ?scheduleToCloseTimeout:string, ?scheduleToStartTimeout:string, ?startToCloseTimeout:string, ?taskList:TaskList, ?taskPriority:string) =
        let attr = new ScheduleActivityTaskDecisionAttributes()
        attr.ActivityType <- activityType
        attr.ActivityId <- activityId
        attr.Input <- if input.IsSome then input.Value else null
        attr.HeartbeatTimeout <- if heartbeatTimeout.IsSome then heartbeatTimeout.Value else null
        attr.ScheduleToCloseTimeout <- if scheduleToCloseTimeout.IsSome then scheduleToCloseTimeout.Value else null
        attr.ScheduleToStartTimeout <- if scheduleToStartTimeout.IsSome then scheduleToStartTimeout.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value else null

        StartAndWaitForActivityTaskAction.Attributes(attr)

    /// <summary>Start an Activity Task but do not block further progress.</summary>
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
    /// <returns>A StartActivityTaskResult of Scheduling, Scheduled, Started, or ScheduleFailed.</returns>
    static member StartActivityTask(activity:ActivityType, activityId:string, ?input:string, ?heartbeatTimeout:string, ?scheduleToCloseTimeout:string, ?scheduleToStartTimeout:string, ?startToCloseTimeout:string, ?taskList:TaskList, ?taskPriority:string) =
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

        StartActivityTaskAction.Attributes(attr)

    /// <summary>Wait for an Activity Task and block further progress until the Activity Task has been Completed, Canceled, TimedOut, or Failed.</summary>
    /// <param name="start">Required. The result of a previous StartActivityTask call.</param>
    /// <returns>A WaitForActivityTaskResult of Completed, Canceled, TimedOut, Failed, or ScheduleFailed.</returns>
    static member WaitForActivityTask(start:StartActivityTaskResult) =
        WaitForActivityTaskAction.StartResult(start)

    static member RequestCancelActivityTask(start:StartActivityTaskResult) =
        RequestCancelActivityTaskAction.StartResult(start)

    static member StartAndWaitForLambdaFunction(id:string, name:string, ?input:string, ?startToCloseTimeout:string) =
        let attr = new ScheduleLambdaFunctionDecisionAttributes()
        attr.Id <- id
        attr.Input <- if input.IsSome then input.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value else null
        attr.Name <- name

        StartAndWaitForLambdaFunctionAction.Attributes(attr)

    static member StartTimer(timerId:string, startToFireTimeout:string) =
        let attr = new StartTimerDecisionAttributes()
        attr.TimerId <- timerId
        attr.StartToFireTimeout <- startToFireTimeout.ToString()

        StartTimerAction.Attributes(attr)

    static member CancelTimer(start:StartTimerResult) =
        CancelTimerAction.StartResult(start)

    static member WaitForTimer(start:StartTimerResult) =
        WaitForTimerAction.StartResult(start)

    static member RecordMarker(markerName:string, ?details:string) =
        let attr = new RecordMarkerDecisionAttributes(MarkerName=markerName);
        attr.Details <- if details.IsSome then details.Value else null

        RecordMarkerAction.Attributes(attr)

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
        ?taskStartToCloseTimeout:string
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

        StartChildWorkflowExecutionAction.Attributes(attr)

    static member WaitForChildWorkflowExecution(start:StartChildWorkflowExecutionResult) =
        WaitForChildWorkflowExecutionAction.StartResult(start)

    static member SignalExternalWorkflowExecution(signalName:string, workflowId:string, ?input:string, ?runId:string) =
        let attr = new SignalExternalWorkflowExecutionDecisionAttributes(SignalName=signalName, WorkflowId=workflowId);
        attr.Input <- if input.IsSome then input.Value else null
        attr.RunId <- if runId.IsSome then runId.Value else null

        SignalExternalWorkflowExecutionAction.Attributes(attr)

    static member RequestCancelExternalWorkflowExecution (workflowId:string, ?runId:string) =
        let attr = new RequestCancelExternalWorkflowExecutionDecisionAttributes()
        attr.RunId <- if runId.IsSome then runId.Value else null
        attr.WorkflowId <- workflowId

        RequestCancelExternalWorkflowExecutionAction.Attributes(attr)

    static member WorkflowExecutionSignaled(signalName:string) =
        WorkflowExecutionSignaledAction.Attributes(SignalName=signalName)

    static member WaitForWorkflowExecutionSignaled(signalName:string) =
        WaitForWorkflowExecutionSignaledAction.Attributes(SignalName=signalName)

    static member WorkflowExecutionCancelRequested() =
        WorkflowExecutionCancelRequestedAction.Attributes()

    static member GetWorkflowExecutionInput() =
        GetWorkflowExecutionInputAction.Attributes()

