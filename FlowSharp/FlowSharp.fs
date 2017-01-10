namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

type FlowSharp = 
    static member StartAndWaitForActivityTask(activity:ActivityType, ?input:string, ?activityId:string, ?heartbeatTimeout:uint32, ?scheduleToCloseTimeout:uint32, ?scheduleToStartTimeout:uint32, ?startToCloseTimeout:uint32, ?taskList:TaskList, ?taskPriority:int) =
        let attr = new ScheduleActivityTaskDecisionAttributes()
        attr.ActivityId <- if activityId.IsSome then activityId.Value else null
        attr.ActivityType <- activity
        attr.HeartbeatTimeout <- if heartbeatTimeout.IsSome then heartbeatTimeout.Value.ToString() else null
        attr.Input <- if input.IsSome then input.Value else null
        attr.ScheduleToCloseTimeout <- if scheduleToCloseTimeout.IsSome then scheduleToCloseTimeout.Value.ToString() else null
        attr.ScheduleToStartTimeout <- if scheduleToStartTimeout.IsSome then scheduleToStartTimeout.Value.ToString() else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value.ToString() else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value.ToString() else null

        StartAndWaitForActivityTaskAction.Attributes(attr)

    static member StartActivityTask(activity:ActivityType, ?input:string, ?activityId:string, ?heartbeatTimeout:uint32, ?scheduleToCloseTimeout:uint32, ?scheduleToStartTimeout:uint32, ?startToCloseTimeout:uint32, ?taskList:TaskList, ?taskPriority:int) =
        let attr = new ScheduleActivityTaskDecisionAttributes()
        attr.ActivityId <- if activityId.IsSome then activityId.Value else null
        attr.ActivityType <- activity
        attr.HeartbeatTimeout <- if heartbeatTimeout.IsSome then heartbeatTimeout.Value.ToString() else null
        attr.Input <- if input.IsSome then input.Value else null
        attr.ScheduleToCloseTimeout <- if scheduleToCloseTimeout.IsSome then scheduleToCloseTimeout.Value.ToString() else null
        attr.ScheduleToStartTimeout <- if scheduleToStartTimeout.IsSome then scheduleToStartTimeout.Value.ToString() else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value.ToString() else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value.ToString() else null

        StartActivityTaskAction.Attributes(attr)

    static member WaitForActivityTask(start:StartActivityTaskResult) =
        WaitForActivityTaskAction.StartResult(start)

    static member RequestCancelActivityTask(start:StartActivityTaskResult) =
        RequestCancelActivityTaskAction.StartResult(start)

    static member StartAndWaitForLambdaFunction(id:string, name:string, ?input:string, ?startToCloseTimeout:uint32) =
        let attr = new ScheduleLambdaFunctionDecisionAttributes()
        attr.Id <- id
        attr.Input <- if input.IsSome then input.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value.ToString() else null
        attr.Name <- name

        StartAndWaitForLambdaFunctionAction.Attributes(attr)

    static member StartTimer(timerId:string, startToFireTimeout:uint32) =
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
        ?executionStartToCloseTimeout:uint32,
        ?taskStartToCloseTimeout:uint32
        ) =
        let attr = new StartChildWorkflowExecutionDecisionAttributes();
        attr.ChildPolicy <- if childPolicy.IsSome then childPolicy.Value else null
        attr.ExecutionStartToCloseTimeout <- if executionStartToCloseTimeout.IsSome then (if executionStartToCloseTimeout.Value = 0u then "NONE" else executionStartToCloseTimeout.Value.ToString()) else null
        attr.Input <- if input.IsSome then input.Value else null
        attr.LambdaRole <- if lambdaRole.IsSome then lambdaRole.Value else null
        attr.TagList <- if tagList.IsSome then tagList.Value else null
        attr.TaskList <- if taskList.IsSome then taskList.Value else null
        attr.TaskPriority <- if taskPriority.IsSome then taskPriority.Value.ToString() else null
        attr.TaskStartToCloseTimeout <- if taskStartToCloseTimeout.IsSome then (if taskStartToCloseTimeout.Value = 0u then "NONE" else taskStartToCloseTimeout.Value.ToString()) else null
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

