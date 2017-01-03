namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type SignalReceivedAction =
    | Attributes of SignalName:string * Input:string option * Wait:bool option * MarkerName:string option * MarkerDetails:string option

type SignalReceivedResult =
    | NotRecieved
    | Received of SignalName:string * Input:string * ExternalWorkflowExecution:WorkflowExecution * ExternalInitiatedEventId:Int64

type RecordMarkerAction = 
    | Attributes of RecordMarkerDecisionAttributes

type SignalExternalWorkflowExecutionAction = 
    | Attributes of SignalExternalWorkflowExecutionDecisionAttributes

type RequestCancelExternalWorkflowExecutionAction =
    | Attributes of RequestCancelExternalWorkflowExecutionDecisionAttributes

type RequestCancelExternalWorkflowExecutionResult =
    | Requesting
    | RequestFailed of RequestCancelExternalWorkflowExecutionFailedEventAttributes
    | RequestInitiated of RequestCancelExternalWorkflowExecutionInitiatedEventAttributes
    | RequestDelivered of ExternalWorkflowExecutionCancelRequestedEventAttributes

type StartActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type StartActivityTaskResult =
    | ScheduleFailed of Cause:ScheduleActivityTaskFailedCause
    | Scheduling of Activity:ActivityType * ActivityId:string
    | Scheduled of Activity:ActivityType * Control:string * ActivityId:string
    | Started of Activity:ActivityType * Control:string * ActivityId:string

type CompleteActivityTaskAction =
    | StartResult of StartActivityTaskResult

type CompleteActivityTaskResult =
    | ScheduleFailed of Cause:ScheduleActivityTaskFailedCause
    | Completed of Result:string
    | Canceled of Details:string
    | TimedOut of TimeoutType:ActivityTaskTimeoutType * Details:string
    | Failed of Reason:string * Details:string

type RequestCancelActivityTaskAction =
    | StartResult of StartActivityTaskResult

type RequestCancelActivityTaskResult =
    | ScheduleFailed of Cause:ScheduleActivityTaskFailedCause
    | RequestCancelFailed of ActivityId:string * Cause:RequestCancelActivityTaskFailedCause
    | CancelRequested
    | Completed of Result:string
    | Canceled of Details:string
    | TimedOut of TimeoutType:ActivityTaskTimeoutType * Details:string
    | Failed of Reason:string * Details:string

type ExecuteActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type ExecuteActivityTaskResult =
    | ScheduleFailed of Cause:ScheduleActivityTaskFailedCause
    | Completed of Result:string
    | Canceled of Details:string
    | TimedOut of TimeoutType:ActivityTaskTimeoutType * Details:string
    | Failed of Reason:string * Details:string

type StartChildWorkflowExecutionAction =
    | Attributes of StartChildWorkflowExecutionDecisionAttributes

type StartChildWorkflowExecutionResult =
    | Starting 
    | StartFailed of Cause:StartChildWorkflowExecutionFailedCause 
    | Initiated of WorkflowType:WorkflowType * Control:string * WorkflowId:string
    | Started of WorkflowType:WorkflowType * Control:string * WorkflowExecution:WorkflowExecution

type CompleteChildWorkflowExecutionAction =
    | StartResult of StartChildWorkflowExecutionResult

type CompleteChildWorkflowExecutionResult =
    | StartFailed of Cause:StartChildWorkflowExecutionFailedCause
    | Completed of Result:string
    | Canceled of Details:string
    | TimedOut of TimeoutType:WorkflowExecutionTimeoutType
    | Failed of Reason:string * Details:string
    | Terminated

type ExecuteLambdaFunctionAction =
    | Attributes of ScheduleLambdaFunctionDecisionAttributes

type ExecuteLambdaFunctionResult =
    | ScheduleFailed of Cause:ScheduleLambdaFunctionFailedCause
    | StartFailed of Cause:StartLambdaFunctionFailedCause * Message:string
    | Completed of Result:string
    | TimedOut of TimeoutType:LambdaFunctionTimeoutType
    | Failed of Reason:string * Details:string

type StartTimerAction =
    | Attributes of StartTimerDecisionAttributes

type StartTimerResult =
    | StartTimerFailed of Cause:StartTimerFailedCause
    | Starting
    | Started of TimerId:string * Control:string

type WaitForTimerAction =
    | StartResult of StartTimerResult

type WaitForTimerResult =
    | StartTimerFailed of Cause:StartTimerFailedCause
    | Fired
    | Canceled

type CancelTimerAction =
    | StartResult of StartTimerResult

type CancelTimerResult =
    | NotStarted
    | CancelTimerFailed of Cause:CancelTimerFailedCause
    | Canceling
    | Fired
    | Canceled

type CheckForWorkflowExecutionCancelRequestedAction =
    | Attributes of unit

type CheckForWorkflowExecutionCancelRequestedResult =
    | NotRequested
    | Requested of WorkflowExecutionCancelRequestedEventAttributes

type WorkflowResult = 
    | Complete of Result:string
    | Cancel of Details:string
    | ContinueAsNew of ContinueAsNewWorkflowExecutionDecisionAttributes
    | Fail of Reason:string * Details:string

type GetWorkflowExecutionInputAction =
    | Attributes of unit

type DeciderX = 
    static member ExecuteActivityTask(activity:ActivityType, ?input:string, ?activityId:string, ?heartbeatTimeout:uint32, ?scheduleToCloseTimeout:uint32, ?scheduleToStartTimeout:uint32, ?startToCloseTimeout:uint32, ?taskList:TaskList, ?taskPriority:int) =
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

        ExecuteActivityTaskAction.Attributes(attr)

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

    static member CompleteActivityTask(start:StartActivityTaskResult) =
        CompleteActivityTaskAction.StartResult(start)

    static member RequestCancelActivityTask(start:StartActivityTaskResult) =
        RequestCancelActivityTaskAction.StartResult(start)

    static member ExecuteLambdaFunction(id:string, name:string, ?input:string, ?startToCloseTimeout:uint32) =
        let attr = new ScheduleLambdaFunctionDecisionAttributes()
        attr.Id <- id
        attr.Input <- if input.IsSome then input.Value else null
        attr.StartToCloseTimeout <- if startToCloseTimeout.IsSome then startToCloseTimeout.Value.ToString() else null
        attr.Name <- name

        ExecuteLambdaFunctionAction.Attributes(attr)

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

    static member CompleteChildWorkflowExecution(start:StartChildWorkflowExecutionResult) =
        CompleteChildWorkflowExecutionAction.StartResult(start)

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

    static member SignalReceivedSinceMarker(signalName:string, markerName:string, ?input:string, ?wait:bool, ?markerDetails:string) =
        SignalReceivedAction.Attributes(SignalName=signalName, Input=input, Wait=wait, MarkerName=Some(markerName), MarkerDetails=markerDetails)

    static member SignalReceived(signalName:string, ?input:string, ?wait:bool) =
        SignalReceivedAction.Attributes(SignalName=signalName, Input=input, Wait=wait, MarkerName=None, MarkerDetails=None)

    static member CheckForWorkflowExecutionCancelRequested() =
        CheckForWorkflowExecutionCancelRequestedAction.Attributes()

    static member GetWorkflowExecutionInput() =
        GetWorkflowExecutionInputAction.Attributes()

module WFModule =
    

    let FindSignalHistory (decisionTask:DecisionTask) (signalName:string) (input:string option) (markerName:string option) (markerDetails:string option) =
        let combinedHistory = new HistoryEvent()
        let mutable markerEvent : HistoryEvent = null

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Capture the Marker event, if markerName is supplied. Also test marker details if that is supplied.
            if hev.EventType = EventType.MarkerRecorded && 
               markerName.IsSome &&
               hev.MarkerRecordedEventAttributes.MarkerName = markerName.Value && 
               ( (markerDetails.IsNone) || (hev.MarkerRecordedEventAttributes.Details = markerDetails.Value) ) then

                setCommonProperties(hev)
                combinedHistory.MarkerRecordedEventAttributes <- hev.MarkerRecordedEventAttributes
                markerEvent <- hev

            // Capture the Signal event, if the signal name matches, optionally if the signal input matches, and optionally if the specified marker was found
            elif hev.EventType = EventType.WorkflowExecutionSignaled && 
                 hev.WorkflowExecutionSignaledEventAttributes.SignalName = signalName &&
                 ( (input.IsNone) || (hev.WorkflowExecutionSignaledEventAttributes.Input = input.Value) ) &&
                 ( (markerName.IsNone) || (markerEvent <> null) ) then

                setCommonProperties(hev)
                combinedHistory.WorkflowExecutionSignaledEventAttributes <- hev.WorkflowExecutionSignaledEventAttributes
                combinedHistory.EventType <- EventType.WorkflowExecutionSignaled
        
        // Return the combined history
        combinedHistory

    let FindMarkerHistory (decisionTask:DecisionTask) (markerName:string) (details:string) =
        let combinedHistory = new HistoryEvent()
        let mutable decisionTaskCompletedEventId = -1L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.MarkerRecorded && hev.MarkerRecordedEventAttributes.MarkerName = markerName && hev.MarkerRecordedEventAttributes.Details = details then
                setCommonProperties(hev)
                combinedHistory.MarkerRecordedEventAttributes <- hev.MarkerRecordedEventAttributes
                decisionTaskCompletedEventId <- hev.MarkerRecordedEventAttributes.DecisionTaskCompletedEventId

            elif hev.EventType = EventType.RecordMarkerFailed && hev.RecordMarkerFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompletedEventId && hev.RecordMarkerFailedEventAttributes.MarkerName = markerName then
                setCommonProperties(hev)
                combinedHistory.RecordMarkerFailedEventAttributes <- hev.RecordMarkerFailedEventAttributes
        
        // Return the combined history
        combinedHistory

    let FindChildWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (workflowType:WorkflowType) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable decisionTaskCompletedEventId = -1L
        let mutable initiatedEventId = -1L
        let mutable startedEventId = -1L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()

            // StartChildWorkflowExecutionInitiated
            elif hev.EventType = EventType.StartChildWorkflowExecutionInitiated &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionInitiatedEventAttributes <- hev.StartChildWorkflowExecutionInitiatedEventAttributes
                decisionTaskCompletedEventId <- hev.StartChildWorkflowExecutionInitiatedEventAttributes.DecisionTaskCompletedEventId
                initiatedEventId <- hev.EventId

            // StartChildWorkflowExecutionFailed
            elif hev.EventType = EventType.StartChildWorkflowExecutionFailed &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionFailedEventAttributes <- hev.StartChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionStarted
            elif hev.EventType = EventType.ChildWorkflowExecutionStarted &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionFailedEventAttributes <- hev.StartChildWorkflowExecutionFailedEventAttributes
                startedEventId <- hev.EventId

            // ChildWorkflowExecutionCompleted
            elif hev.EventType = EventType.ChildWorkflowExecutionCompleted &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.StartedEventId = startedEventId &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCompletedEventAttributes <- hev.ChildWorkflowExecutionCompletedEventAttributes

            // ChildWorkflowExecutionFailed
            elif hev.EventType = EventType.ChildWorkflowExecutionFailed &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.StartedEventId = startedEventId &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionFailedEventAttributes <- hev.ChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionTimedOut
            elif hev.EventType = EventType.ChildWorkflowExecutionTimedOut &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.StartedEventId = startedEventId &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionTimedOutEventAttributes <- hev.ChildWorkflowExecutionTimedOutEventAttributes

            // ChildWorkflowExecutionCanceled
            elif hev.EventType = EventType.ChildWorkflowExecutionCanceled &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.StartedEventId = startedEventId &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCanceledEventAttributes <- hev.ChildWorkflowExecutionCanceledEventAttributes

            // ChildWorkflowExecutionTerminated
            elif hev.EventType = EventType.ChildWorkflowExecutionTerminated &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.StartedEventId = startedEventId &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionTerminatedEventAttributes <- hev.ChildWorkflowExecutionTerminatedEventAttributes

        // Return the combined history
        combinedHistory

    let FindRequestCancelExternalWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable initiatedEventId = -1L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.RequestCancelExternalWorkflowExecutionInitiated && 
                               hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
                               hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes 
                initiatedEventId <- hev.EventId

            elif hev.EventType = EventType.RequestCancelExternalWorkflowExecutionFailed &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                                 hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.RequestCancelExternalWorkflowExecutionFailedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes 

            elif hev.EventType = EventType.ExternalWorkflowExecutionCancelRequested && 
                               hev.ExternalWorkflowExecutionCancelRequestedEventAttributes.InitiatedEventId = initiatedEventId &&
                               hev.ExternalWorkflowExecutionCancelRequestedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ExternalWorkflowExecutionCancelRequestedEventAttributes <- hev.ExternalWorkflowExecutionCancelRequestedEventAttributes

        // Return the combined history
        combinedHistory

    let FindSignalExternalWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (signalName:string) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable decisionTaskCompletedEventId = -1L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.SignalExternalWorkflowExecutionInitiated && 
                               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.Control = bindingIdString &&
                               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.SignalName = signalName && 
                               hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.SignalExternalWorkflowExecutionInitiatedEventAttributes <- hev.SignalExternalWorkflowExecutionInitiatedEventAttributes
                decisionTaskCompletedEventId <- hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.DecisionTaskCompletedEventId

            elif hev.EventType = EventType.SignalExternalWorkflowExecutionFailed && 
                                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompletedEventId && 
                                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.SignalExternalWorkflowExecutionFailedEventAttributes <- hev.SignalExternalWorkflowExecutionFailedEventAttributes
        
        // Return the combined history
        combinedHistory
        
    let FindContinueAsNewWorkflowExecutionHistory (decisionTask:DecisionTask) =
        let combinedHistory = new HistoryEvent()
        let mutable decisionTaskCompletedEventId = -1L

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.WorkflowExecutionContinuedAsNew then                
                setCommonProperties(hev)
                combinedHistory.WorkflowExecutionContinuedAsNewEventAttributes <- hev.WorkflowExecutionContinuedAsNewEventAttributes
                decisionTaskCompletedEventId <- hev.WorkflowExecutionContinuedAsNewEventAttributes.DecisionTaskCompletedEventId

            elif hev.EventType = EventType.ContinueAsNewWorkflowExecutionFailed && 
                                 hev.ContinueAsNewWorkflowExecutionFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompletedEventId then
                setCommonProperties(hev)
                combinedHistory.ContinueAsNewWorkflowExecutionFailedEventAttributes <- hev.ContinueAsNewWorkflowExecutionFailedEventAttributes
                        
        // Return the combined history
        combinedHistory

    let FindActivityTaskHistory (decisionTask:DecisionTask) (bindingId:int) (activityId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable scheduledEventId = -1L
        let mutable startedEventId = -1L
        let mutable activityId : string = null
        let mutable latestCancelRequestedEventId = -1L
            
        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()
            
            // ActivityTaskScheduled
            elif hev.EventType = EventType.ActivityTaskScheduled && hev.ActivityTaskScheduledEventAttributes.Control = bindingIdString then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskScheduledEventAttributes <- hev.ActivityTaskScheduledEventAttributes
                scheduledEventId <- hev.EventId
                activityId <- hev.ActivityTaskScheduledEventAttributes.ActivityId

            // ActivityTaskStarted
            elif hev.EventType = EventType.ActivityTaskStarted && hev.ActivityTaskStartedEventAttributes.ScheduledEventId = scheduledEventId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskStartedEventAttributes <- hev.ActivityTaskStartedEventAttributes
                startedEventId <- hev.EventId

            // ActivityTaskCompleted
            elif hev.EventType = EventType.ActivityTaskCompleted && hev.ActivityTaskCompletedEventAttributes.ScheduledEventId = scheduledEventId && hev.ActivityTaskCompletedEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskCompletedEventAttributes <- hev.ActivityTaskCompletedEventAttributes

            // ActivityTaskCanceled
            elif hev.EventType = EventType.ActivityTaskCanceled && hev.ActivityTaskCanceledEventAttributes.ScheduledEventId = scheduledEventId && hev.ActivityTaskCanceledEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskCanceledEventAttributes <- hev.ActivityTaskCanceledEventAttributes

            // ActivityTaskTimedOut
            elif hev.EventType = EventType.ActivityTaskTimedOut && hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = scheduledEventId && hev.ActivityTaskTimedOutEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskTimedOutEventAttributes <- hev.ActivityTaskTimedOutEventAttributes

            // ActivityTaskFailed
            elif hev.EventType = EventType.ActivityTaskFailed && hev.ActivityTaskFailedEventAttributes.ScheduledEventId = scheduledEventId && hev.ActivityTaskFailedEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskFailedEventAttributes <- hev.ActivityTaskFailedEventAttributes

            // ActivityTaskCancelRequested
            elif hev.EventType = EventType.ActivityTaskCancelRequested && hev.ActivityTaskCancelRequestedEventAttributes.ActivityId = activityId then
                setCommonProperties(hev)
                combinedHistory.ActivityTaskCancelRequestedEventAttributes <- hev.ActivityTaskCancelRequestedEventAttributes

            // RequestCancelActivityTaskFailed
            elif hev.EventType = EventType.RequestCancelActivityTaskFailed && hev.RequestCancelActivityTaskFailedEventAttributes.ActivityId = activityId
                                                                           && hev.RequestCancelActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                setCommonProperties(hev)
                combinedHistory.RequestCancelActivityTaskFailedEventAttributes <- hev.RequestCancelActivityTaskFailedEventAttributes

            // ScheduleActivityTaskFailed
            elif hev.EventType = EventType.ScheduleActivityTaskFailed && hev.ScheduleActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.ScheduleActivityTaskFailedEventAttributes <- hev.ScheduleActivityTaskFailedEventAttributes

        // Return the combined history
        combinedHistory
    
    let FindLambdaFunctionHistory (decisionTask:DecisionTask) (id:string) (name:string) =
        let combinedHistory = new HistoryEvent()
        let mutable scheduledEventId = -1L
        let mutable startedEventId = -1L
            
        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()
            
            // LambdaFunctionScheduled
            elif hev.EventType = EventType.LambdaFunctionScheduled && hev.LambdaFunctionScheduledEventAttributes.Id = id && hev.LambdaFunctionScheduledEventAttributes.Name = name then
                setCommonProperties(hev)
                combinedHistory.LambdaFunctionScheduledEventAttributes <- hev.LambdaFunctionScheduledEventAttributes
                scheduledEventId <- hev.EventId

            // LambdaFunctionStarted
            elif hev.EventType = EventType.LambdaFunctionStarted && hev.LambdaFunctionStartedEventAttributes.ScheduledEventId = scheduledEventId then
                setCommonProperties(hev)
                combinedHistory.LambdaFunctionStartedEventAttributes <- hev.LambdaFunctionStartedEventAttributes
                startedEventId <- hev.EventId

            // LambdaFunctionCompleted
            elif hev.EventType = EventType.LambdaFunctionCompleted && hev.LambdaFunctionCompletedEventAttributes.ScheduledEventId = scheduledEventId && hev.LambdaFunctionCompletedEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.LambdaFunctionCompletedEventAttributes <- hev.LambdaFunctionCompletedEventAttributes

            // LambdaFunctionTimedOut
            elif hev.EventType = EventType.LambdaFunctionTimedOut && hev.LambdaFunctionTimedOutEventAttributes.ScheduledEventId = scheduledEventId && hev.LambdaFunctionTimedOutEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.LambdaFunctionTimedOutEventAttributes <- hev.LambdaFunctionTimedOutEventAttributes

            // LambdaFunctionFailed
            elif hev.EventType = EventType.LambdaFunctionFailed && hev.LambdaFunctionFailedEventAttributes.ScheduledEventId = scheduledEventId && hev.LambdaFunctionFailedEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.LambdaFunctionFailedEventAttributes <- hev.LambdaFunctionFailedEventAttributes

            // Schedule Lambda Function Failed
            elif hev.EventType = EventType.ScheduleLambdaFunctionFailed && hev.ScheduleLambdaFunctionFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.ScheduleLambdaFunctionFailedEventAttributes <- hev.ScheduleLambdaFunctionFailedEventAttributes

            // Start Lambda Function Failed
            elif hev.EventType = EventType.StartLambdaFunctionFailed && hev.StartLambdaFunctionFailedEventAttributes.ScheduledEventId = scheduledEventId then
                combinedHistory.StartLambdaFunctionFailedEventAttributes <- hev.StartLambdaFunctionFailedEventAttributes

        // Return the combined history
        combinedHistory

    let FindTimerHistory (decisionTask:DecisionTask) (bindingId:int) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()
        let mutable startedEventId = -1L
        let mutable timerId : string = null
            
        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()
            
            // TimerStarted
            elif hev.EventType = EventType.TimerStarted && hev.TimerStartedEventAttributes.Control = bindingIdString then
                setCommonProperties(hev)
                combinedHistory.TimerStartedEventAttributes <- hev.TimerStartedEventAttributes
                startedEventId <- hev.EventId
                timerId <- hev.TimerStartedEventAttributes.TimerId

            // TimerFired
            elif hev.EventType = EventType.TimerFired && hev.TimerFiredEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.TimerFiredEventAttributes <- hev.TimerFiredEventAttributes

            // TimerCanceled
            elif hev.EventType = EventType.TimerCanceled && hev.TimerCanceledEventAttributes.StartedEventId = startedEventId then
                setCommonProperties(hev)
                combinedHistory.TimerCanceledEventAttributes <- hev.TimerCanceledEventAttributes

            // StartTimerFailed
            elif hev.EventType = EventType.StartTimerFailed && hev.StartTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.StartTimerFailedEventAttributes <- hev.StartTimerFailedEventAttributes

            // CancelTimerFailed
            elif hev.EventType = EventType.CancelTimerFailed && hev.CancelTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.CancelTimerFailedEventAttributes <- hev.CancelTimerFailedEventAttributes

        // Return the combined history
        combinedHistory


    type DeciderBuilder (DecisionTask:DecisionTask) =
        let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
        let mutable bindingId = 0
        let mutable blockFlag = false

        let NextBindingId() =
            bindingId <- bindingId + 1
            bindingId

        member this.Delay(f) =
            if DecisionTask.TaskToken = null then 
                // When PollForDecisionTask times out, the TaskToken is null. There's nothing to decide in this case so null is returned.
                (fun () -> null)
            else
                // There are decisions to be made, so call the decider.
                (fun () -> f())

        member this.Run(f) : RespondDecisionTaskCompletedRequest = 
            // Run is used to call the Delay function, making execution immediate which is what we want.
            f()

        member this.Zero() = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)

        member this.Return(result:WorkflowResult) =
            blockFlag <- true
            let decision = new Decision();

            match result with
            | Complete(r) -> 
                decision.DecisionType <- DecisionType.CompleteWorkflowExecution
                decision.CompleteWorkflowExecutionDecisionAttributes <- new CompleteWorkflowExecutionDecisionAttributes();
                decision.CompleteWorkflowExecutionDecisionAttributes.Result <- r

            | Cancel(details) ->
                decision.DecisionType <- DecisionType.CancelWorkflowExecution
                decision.CancelWorkflowExecutionDecisionAttributes <- new CancelWorkflowExecutionDecisionAttributes();
                decision.CancelWorkflowExecutionDecisionAttributes.Details <- details

            | ContinueAsNew(attr) ->
                // Look for possible failure of ContinueAsNewWorkflowExecution
                let combinedHistory = FindContinueAsNewWorkflowExecutionHistory DecisionTask

                match (combinedHistory) with
                | h when h.EventType = EventType.ContinueAsNewWorkflowExecutionFailed ->
                    // A previous attempt was made to continue this workflow as new, but it failed
                    // Raise an exception that the decider can process
                    failwith (sprintf "ContinueAsNewWorkflowExecutionFailed, %s" (h.ContinueAsNewWorkflowExecutionFailedEventAttributes.Cause.ToString()))
                | _ ->
                    decision.DecisionType <- DecisionType.ContinueAsNewWorkflowExecution
                    decision.ContinueAsNewWorkflowExecutionDecisionAttributes <- new ContinueAsNewWorkflowExecutionDecisionAttributes();
                 
            | Fail(details, reason) ->
                decision.DecisionType <- DecisionType.FailWorkflowExecution
                decision.FailWorkflowExecutionDecisionAttributes <- new FailWorkflowExecutionDecisionAttributes();
                decision.FailWorkflowExecutionDecisionAttributes.Details <- details
                decision.FailWorkflowExecutionDecisionAttributes.Reason <- reason

            response.Decisions.Add(decision)
            response

        member this.Return(result:string) = this.Return(Complete(result))
        member this.Return(result:unit) = this.Return(Complete(null))
            
        // Execute Activity
        member this.Bind(ExecuteActivityTaskAction.Attributes(attr), f:(ExecuteActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
            // The idea is that with the same decider, the sequence of calls to Bind will be the same. The bindingId is used in the .Control 
            // properties and is used when matching the execution history to a DeciderAction
            let bindingId = NextBindingId()

            let combinedHistory = FindActivityTaskHistory DecisionTask bindingId (attr.ActivityId)

            match (combinedHistory) with
            // Completed
            | h when h.EventType = EventType.ActivityTaskCompleted -> 
                f(ExecuteActivityTaskResult.Completed(h.ActivityTaskCompletedEventAttributes.Result))

            // TimedOut
            | h when h.EventType = EventType.ActivityTaskTimedOut ->
                f(ExecuteActivityTaskResult.TimedOut(TimeoutType=h.ActivityTaskTimedOutEventAttributes.TimeoutType, Details=h.ActivityTaskTimedOutEventAttributes.Details))

            // Canceled
            | h when h.EventType = EventType.ActivityTaskCanceled ->
                f(ExecuteActivityTaskResult.Canceled(h.ActivityTaskCanceledEventAttributes.Details))

            // Failed
            | h when h.EventType = EventType.ActivityTaskFailed ->
                f(ExecuteActivityTaskResult.Failed(Reason=h.ActivityTaskFailedEventAttributes.Reason, Details=h.ActivityTaskFailedEventAttributes.Details))

            // ScheduleActivityTaskFailed
            | h when h.ScheduleActivityTaskFailedEventAttributes <> null && 
                        h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = attr.ActivityType.Name && 
                        h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = attr.ActivityType.Version ->
                f(ExecuteActivityTaskResult.ScheduleFailed(h.ScheduleActivityTaskFailedEventAttributes.Cause))

            // Not Scheduled
            | h when h.ActivityTaskScheduledEventAttributes = null ->
                blockFlag <- true
                attr.Control <- bindingId.ToString()

                let d = new Decision();
                d.DecisionType <- DecisionType.ScheduleActivityTask
                d.ScheduleActivityTaskDecisionAttributes <- attr
                response.Decisions.Add(d)
                response
            | _ -> 
                // This activity is still running, continue blocking
                blockFlag <- true
                response

        // Start Activity Task
        member this.Bind(StartActivityTaskAction.Attributes(attr), f:(StartActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
            let bindingId = NextBindingId()

            let combinedHistory = FindActivityTaskHistory DecisionTask bindingId (attr.ActivityId)

            match (combinedHistory) with
            // ScheduleActivityTaskFailed
            | h when h.ScheduleActivityTaskFailedEventAttributes <> null && 
                        h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = attr.ActivityType.Name && 
                        h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = attr.ActivityType.Version ->
                f(StartActivityTaskResult.ScheduleFailed(h.ScheduleActivityTaskFailedEventAttributes.Cause))

            // Started
            | h when h.ActivityTaskStartedEventAttributes <> null && h.ActivityTaskScheduledEventAttributes <> null->
                f(StartActivityTaskResult.Started(Activity=attr.ActivityType, Control=h.ActivityTaskScheduledEventAttributes.Control, ActivityId=h.ActivityTaskScheduledEventAttributes.ActivityId))

            // Scheduled
            | h when h.ActivityTaskScheduledEventAttributes <> null ->
                f(StartActivityTaskResult.Scheduled(Activity=attr.ActivityType, Control=h.ActivityTaskScheduledEventAttributes.Control, ActivityId=h.ActivityTaskScheduledEventAttributes.ActivityId))

            // Not Scheduled
            | h when h.ActivityTaskScheduledEventAttributes = null ->
                attr.Control <- bindingId.ToString()

                let d = new Decision();
                d.DecisionType <- DecisionType.ScheduleActivityTask
                d.ScheduleActivityTaskDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(StartActivityTaskResult.Scheduling(Activity=attr.ActivityType, ActivityId=attr.ActivityId))

            | _ -> failwith "error"

        // Complete Activity
        member this.Bind(CompleteActivityTaskAction.StartResult(result), f:(CompleteActivityTaskResult -> RespondDecisionTaskCompletedRequest)) =

            let bindWithHistory (activity:ActivityType) (control:string) (activityId:string) =
                let combinedHistory = FindActivityTaskHistory DecisionTask (Convert.ToInt32(control)) activityId

                match (combinedHistory) with
                // Completed
                | h when h.EventType = EventType.ActivityTaskCompleted -> 
                    f(CompleteActivityTaskResult.Completed(h.ActivityTaskCompletedEventAttributes.Result))

                // TimedOut
                | h when h.EventType = EventType.ActivityTaskTimedOut ->
                    f(CompleteActivityTaskResult.TimedOut(TimeoutType=h.ActivityTaskTimedOutEventAttributes.TimeoutType, Details=h.ActivityTaskTimedOutEventAttributes.Details))

                // Canceled
                | h when h.EventType = EventType.ActivityTaskCanceled ->
                    f(CompleteActivityTaskResult.Canceled(h.ActivityTaskCanceledEventAttributes.Details))

                // Failed
                | h when h.EventType = EventType.ActivityTaskFailed ->
                    f(CompleteActivityTaskResult.Failed(Reason=h.ActivityTaskFailedEventAttributes.Reason, Details=h.ActivityTaskFailedEventAttributes.Details))

                // ScheduleActivityTaskFailed
                | h when h.ScheduleActivityTaskFailedEventAttributes <> null && 
                            h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = activity.Name && 
                            h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = activity.Version ->
                    f(CompleteActivityTaskResult.ScheduleFailed(h.ScheduleActivityTaskFailedEventAttributes.Cause))

                | _ -> 
                    // This activity is still running, continue blocking
                    blockFlag <- true
                    response

            match result with 
            // If this activity is being scheduled then block. Return the decision to schedule the activity and pick up here next decistion task
            | Scheduling(_,_) -> 
                blockFlag <- true
                response

            // The StartActivityResult checks for scheduling failure so no need to check history again.
            | StartActivityTaskResult.ScheduleFailed(cause) -> f(CompleteActivityTaskResult.ScheduleFailed(cause))

            | StartActivityTaskResult.Scheduled(activity:ActivityType, control:string, (activityId:string)) ->
                bindWithHistory activity control activityId
            | StartActivityTaskResult.Started(activity:ActivityType, control:string, (activityId:string)) ->
                bindWithHistory activity control activityId

        // Request Cancel Activity Task 
        member this.Bind(RequestCancelActivityTaskAction.StartResult(result), f:(RequestCancelActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
            let bindWithHistory (activity:ActivityType) (control:string) (activityId:string) =
                let combinedHistory = FindActivityTaskHistory DecisionTask (Convert.ToInt32(control)) activityId

                match (combinedHistory) with
                // ScheduleActivityTaskFailed
                | h when h.ScheduleActivityTaskFailedEventAttributes <> null && 
                            h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = activity.Name && 
                            h.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = activity.Version ->
                    f(RequestCancelActivityTaskResult.ScheduleFailed(h.ScheduleActivityTaskFailedEventAttributes.Cause))

                // Canceled
                | h when h.ActivityTaskCanceledEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.Canceled(Details=h.ActivityTaskCanceledEventAttributes.Details))

                // RequestCancelFailed
                | h when h.RequestCancelActivityTaskFailedEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.RequestCancelFailed(ActivityId=activityId, Cause=h.RequestCancelActivityTaskFailedEventAttributes.Cause))

                // Completed
                | h when h.ActivityTaskCompletedEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.Completed(Result=h.ActivityTaskCompletedEventAttributes.Result))

                // Failed
                | h when h.ActivityTaskFailedEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.Failed(Reason=h.ActivityTaskFailedEventAttributes.Reason, Details=h.ActivityTaskFailedEventAttributes.Details))

                // TimedOut
                | h when h.ActivityTaskTimedOutEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.TimedOut(TimeoutType=h.ActivityTaskTimedOutEventAttributes.TimeoutType, Details=h.ActivityTaskTimedOutEventAttributes.Details))

                // CancelRequested
                | h when h.ActivityTaskCancelRequestedEventAttributes <> null ->
                    f(RequestCancelActivityTaskResult.CancelRequested)

                // Task is scheduled
                | h when h.ActivityTaskScheduledEventAttributes <> null && 
                         h.ActivityTaskScheduledEventAttributes.ActivityType.Name = activity.Name && 
                         h.ActivityTaskScheduledEventAttributes.ActivityType.Version = activity.Version ->
                    
                    blockFlag <- true

                    // If started, it can be canceled
                    if h.ActivityTaskStartedEventAttributes <> null then
                        let d = new Decision()
                        d.DecisionType <- DecisionType.RequestCancelActivityTask
                        d.RequestCancelActivityTaskDecisionAttributes <- new RequestCancelActivityTaskDecisionAttributes()
                        d.RequestCancelActivityTaskDecisionAttributes.ActivityId <- h.ActivityTaskScheduledEventAttributes.ActivityId
                        response.Decisions.Add(d)

                    response

                | _ -> failwith "error"

            match result with 
            // If this activity is being scheduled then block. Return the decision to schedule the activity and pick up here next decistion task
            | Scheduling(_,_) -> 
                blockFlag <- true
                response

            // The StartActivityResult checks for scheduling failure so no need to check history again.
            | StartActivityTaskResult.ScheduleFailed(cause) -> f(RequestCancelActivityTaskResult.ScheduleFailed(cause))

            | StartActivityTaskResult.Scheduled(activity:ActivityType, control:string, activityId:string) ->
                bindWithHistory activity control activityId
            | StartActivityTaskResult.Started(activity:ActivityType, control:string, activityId:string) ->
                bindWithHistory activity control activityId

        // Execute Lambda Function
        member this.Bind(ExecuteLambdaFunctionAction.Attributes(attr), f:(ExecuteLambdaFunctionResult -> RespondDecisionTaskCompletedRequest)) = 

            let combinedHistory = FindLambdaFunctionHistory DecisionTask (attr.Id) (attr.Name)

            match (combinedHistory) with
            // Lambda Function Completed
            | h when h.EventType = EventType.LambdaFunctionCompleted -> 
                f(ExecuteLambdaFunctionResult.Completed(h.LambdaFunctionCompletedEventAttributes.Result))

            // Lambda Function TimedOut
            | h when h.EventType = EventType.LambdaFunctionTimedOut ->
                f(ExecuteLambdaFunctionResult.TimedOut(TimeoutType=h.LambdaFunctionTimedOutEventAttributes.TimeoutType))

            // Lambda Function Failed
            | h when h.EventType = EventType.LambdaFunctionFailed ->
                f(ExecuteLambdaFunctionResult.Failed(Reason=h.LambdaFunctionFailedEventAttributes.Reason, Details=h.LambdaFunctionFailedEventAttributes.Details))

            // ScheduleLambdaFunctionFailed
            | h when h.ScheduleLambdaFunctionFailedEventAttributes <> null && 
                        h.ScheduleLambdaFunctionFailedEventAttributes.Id = attr.Id && 
                        h.ScheduleLambdaFunctionFailedEventAttributes.Name = attr.Name -> 
                f(ExecuteLambdaFunctionResult.ScheduleFailed(h.ScheduleLambdaFunctionFailedEventAttributes.Cause))

            // StartLambdaFunctionFailed
            | h when h.StartLambdaFunctionFailedEventAttributes <> null -> 
                f(ExecuteLambdaFunctionResult.StartFailed(Cause=h.StartLambdaFunctionFailedEventAttributes.Cause, Message=h.StartLambdaFunctionFailedEventAttributes.Message))

            // Not Scheduled
            | h when h.ScheduleLambdaFunctionFailedEventAttributes = null ->
                blockFlag <- true

                let d = new Decision();
                d.DecisionType <- DecisionType.ScheduleLambdaFunction
                d.ScheduleLambdaFunctionDecisionAttributes <- attr
                response.Decisions.Add(d)
                response
            | _ -> 
                // This lambda function is still running, continue blocking
                blockFlag <- true
                response

        // Start Timer
        member this.Bind(StartTimerAction.Attributes(attr), f:(StartTimerResult -> RespondDecisionTaskCompletedRequest)) = 
            let bindingId = NextBindingId()

            let combinedHistory = FindTimerHistory DecisionTask bindingId

            match (combinedHistory) with
            // StartTimerFailed
            | h when h.StartTimerFailedEventAttributes <> null && 
                     h.StartTimerFailedEventAttributes.TimerId = attr.TimerId ->
                f(StartTimerResult.StartTimerFailed(h.StartTimerFailedEventAttributes.Cause))

            // TimerStarted
            | h when h.TimerStartedEventAttributes <> null ->
                f(StartTimerResult.Started(TimerId=attr.TimerId, Control=h.TimerStartedEventAttributes.Control))

            // Timer Not Started
            | h when h.TimerStartedEventAttributes = null ->
                attr.Control <- bindingId.ToString()

                let d = new Decision();
                d.DecisionType <- DecisionType.StartTimer
                d.StartTimerDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(StartTimerResult.Starting)

            | _ -> failwith "error"

        // Cancel Timer
        member this.Bind(CancelTimerAction.StartResult(result), f:(CancelTimerResult -> RespondDecisionTaskCompletedRequest)) =

            let bindWithHistory (timerId:string) (control:string) =
                let combinedHistory = FindTimerHistory DecisionTask (Convert.ToInt32(control))

                match (combinedHistory) with
                // TimerCanceled
                | h when h.EventType = EventType.TimerCanceled ->
                    f(CancelTimerResult.Canceled)

                // TimerFired, could have fired before canceled
                | h when h.EventType = EventType.TimerFired ->
                    f(CancelTimerResult.Fired)

                // CancelTimerFailed
                | h when h.CancelTimerFailedEventAttributes <> null && 
                         h.CancelTimerFailedEventAttributes.TimerId = timerId ->
                    f(CancelTimerResult.CancelTimerFailed(h.CancelTimerFailedEventAttributes.Cause))

                | _ -> 
                    // This timer has not been canceled yet, make cancel decision
                    let d = new Decision();
                    d.DecisionType <- DecisionType.CancelTimer
                    d.CancelTimerDecisionAttributes <- new CancelTimerDecisionAttributes(TimerId=timerId)
                    response.Decisions.Add(d)

                    f(CancelTimerResult.Canceling)

            match result with 
            // If this timer is being started then block. Return the current decisions.
            | StartTimerResult.Starting -> 
                blockFlag <- true
                response

            // The StartTimerResult checks for starting failure so no need to check history again.
            | StartTimerResult.StartTimerFailed(cause) -> f(CancelTimerResult.NotStarted)

            | StartTimerResult.Started(timerId, control) ->
                bindWithHistory timerId control

        // Wait For Timer
        member this.Bind(WaitForTimerAction.StartResult(result), f:(WaitForTimerResult -> RespondDecisionTaskCompletedRequest)) =

            let bindWithHistory (timerId:string) (control:string) =
                let combinedHistory = FindTimerHistory DecisionTask (Convert.ToInt32(control))

                match (combinedHistory) with
                // TimerFired
                | h when h.EventType = EventType.TimerFired -> 
                    f(WaitForTimerResult.Fired)

                // TimerCanceled
                | h when h.EventType = EventType.TimerCanceled ->
                    f(WaitForTimerResult.Canceled)

                // StartTimerFailed
                | h when h.StartTimerFailedEventAttributes <> null && 
                            h.StartTimerFailedEventAttributes.TimerId = timerId ->
                    f(WaitForTimerResult.StartTimerFailed(h.StartTimerFailedEventAttributes.Cause))

                | _ -> 
                    // This timer is still running, continue blocking
                    blockFlag <- true
                    response

            match result with 
            // If this timer is being started then block. Return the decision to start the timer and pick up here next decistion task
            | StartTimerResult.Starting -> 
                blockFlag <- true
                response

            // The StartTimerResult checks for starting failure so no need to check history again.
            | StartTimerResult.StartTimerFailed(cause) -> f(WaitForTimerResult.StartTimerFailed(cause))

            | StartTimerResult.Started(timerId, control) ->
                bindWithHistory timerId control

        // Record Marker
        member this.Bind(RecordMarkerAction.Attributes(attr), f:(unit -> RespondDecisionTaskCompletedRequest)) =
            let combinedHistory = FindMarkerHistory DecisionTask attr.MarkerName attr.Details

            if combinedHistory.MarkerRecordedEventAttributes = null && combinedHistory.RecordMarkerFailedEventAttributes = null then
                // The marker was never recorded, record it now
                let d = new Decision();
                d.DecisionType <- DecisionType.RecordMarker
                d.RecordMarkerDecisionAttributes <- attr
                response.Decisions.Add(d)

            // RecordMarker action does not block and returns nothing
            f()

        // Start Child Workflow Execution
        member this.Bind(StartChildWorkflowExecutionAction.Attributes(attr), f:(StartChildWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
            let bindingId = NextBindingId()

            let combinedHistory = FindChildWorkflowExecutionHistory DecisionTask bindingId (attr.WorkflowType) (attr.WorkflowId)

            match (combinedHistory) with
            // StartChildWorkflowExecutionFailed
            | h when h.StartChildWorkflowExecutionFailedEventAttributes <> null && 
                     h.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name && 
                     h.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version ->
                f(StartChildWorkflowExecutionResult.StartFailed(h.StartChildWorkflowExecutionFailedEventAttributes.Cause))

            // Initiated
            | h when h.StartChildWorkflowExecutionInitiatedEventAttributes <> null &&
                     h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name && 
                     h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version ->
                f(StartChildWorkflowExecutionResult.Initiated(WorkflowType=h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType,
                                                              Control=h.StartChildWorkflowExecutionInitiatedEventAttributes.Control,
                                                              WorkflowId=h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId))

            // Started
            | h when h.ChildWorkflowExecutionStartedEventAttributes <> null ->
                f(StartChildWorkflowExecutionResult.Started(WorkflowType=h.ChildWorkflowExecutionStartedEventAttributes.WorkflowType, 
                                                            Control=(bindingId.ToString()),
                                                            WorkflowExecution=h.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution))

            // Not Started
            | h when h.ActivityTaskScheduledEventAttributes = null ->
                attr.Control <- bindingId.ToString()

                let d = new Decision();
                d.DecisionType <- DecisionType.StartChildWorkflowExecution
                d.StartChildWorkflowExecutionDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(StartChildWorkflowExecutionResult.Starting)

            | _ -> failwith "error"

        // Complete Child Workflow Execution
        member this.Bind(CompleteChildWorkflowExecutionAction.StartResult(result), f:(CompleteChildWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =

            let bindWithHistory (workflowType:WorkflowType) (control:string) (workflowId:string) =
                let combinedHistory = FindChildWorkflowExecutionHistory DecisionTask (Convert.ToInt32(control)) workflowType workflowId

                match (combinedHistory) with
                // Completed
                | h when h.EventType = EventType.ChildWorkflowExecutionCompleted -> 
                    f(CompleteChildWorkflowExecutionResult.Completed(h.ChildWorkflowExecutionCompletedEventAttributes.Result))

                // TimedOut
                | h when h.EventType = EventType.ChildWorkflowExecutionTimedOut ->
                    f(CompleteChildWorkflowExecutionResult.TimedOut(TimeoutType=h.ChildWorkflowExecutionTimedOutEventAttributes.TimeoutType))

                // Canceled
                | h when h.EventType = EventType.ChildWorkflowExecutionCanceled ->
                    f(CompleteChildWorkflowExecutionResult.Canceled(h.ChildWorkflowExecutionCanceledEventAttributes.Details))

                // Failed
                | h when h.EventType = EventType.ChildWorkflowExecutionFailed ->
                    f(CompleteChildWorkflowExecutionResult.Failed(Reason=h.ChildWorkflowExecutionFailedEventAttributes.Reason, Details=h.ChildWorkflowExecutionFailedEventAttributes.Details))

                // Terminated
                | h when h.EventType = EventType.ChildWorkflowExecutionTerminated ->
                    f(CompleteChildWorkflowExecutionResult.Terminated)

                | _ -> 
                    // This child workflow execution is still running, continue blocking
                    blockFlag <- true
                    response

            match result with 
            // If this child workflow execution is being started then block. Return the decision to start the child workflow and pick up here next decision task
            | StartChildWorkflowExecutionResult.Starting -> 
                blockFlag <- true
                response

            // The StartChildWorkflowExecutionResult checks for starting failure so no need to check history again.
            | StartChildWorkflowExecutionResult.StartFailed(cause) -> 
                f(CompleteChildWorkflowExecutionResult.StartFailed(cause))
            | StartChildWorkflowExecutionResult.Initiated(workflowType:WorkflowType, control:string, workflowId:string) ->
                bindWithHistory workflowType control workflowId
            | StartChildWorkflowExecutionResult.Started(workflowType:WorkflowType, control:string, workflowExecution:WorkflowExecution) ->
                bindWithHistory workflowType control (workflowExecution.WorkflowId)

        // Request Cancel External Workflow Execution
        member this.Bind(RequestCancelExternalWorkflowExecutionAction.Attributes(attr), f:(RequestCancelExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
            let bindingId = NextBindingId()

            let combinedHistory = FindRequestCancelExternalWorkflowExecutionHistory DecisionTask bindingId attr.WorkflowId

            match (combinedHistory) with
            // Request Delivered
            | h when h.ExternalWorkflowExecutionCancelRequestedEventAttributes <> null ->
                f(RequestCancelExternalWorkflowExecutionResult.RequestDelivered(h.ExternalWorkflowExecutionCancelRequestedEventAttributes))

            // Request Failed
            | h when h.RequestCancelExternalWorkflowExecutionFailedEventAttributes <> null ->
                f(RequestCancelExternalWorkflowExecutionResult.RequestFailed(h.RequestCancelExternalWorkflowExecutionFailedEventAttributes))
 
            // Request Initiated
            | h when h.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <> null ->
                f(RequestCancelExternalWorkflowExecutionResult.RequestInitiated(h.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes))

            // Request not initiated yet
            | _ ->
                attr.Control <- bindingId.ToString()

                let d = new Decision();
                d.DecisionType <- DecisionType.RequestCancelExternalWorkflowExecution
                d.RequestCancelExternalWorkflowExecutionDecisionAttributes <- attr
                response.Decisions.Add(d)
                
                f(RequestCancelExternalWorkflowExecutionResult.Requesting)

        // Signal External Workflow Execution
        member this.Bind(SignalExternalWorkflowExecutionAction.Attributes(attr), f:(unit -> RespondDecisionTaskCompletedRequest)) =
            let bindingId = NextBindingId()

            let combinedHistory = FindSignalExternalWorkflowExecutionHistory DecisionTask bindingId attr.SignalName attr.WorkflowId

            if combinedHistory.SignalExternalWorkflowExecutionInitiatedEventAttributes = null && 
               combinedHistory.SignalExternalWorkflowExecutionFailedEventAttributes = null then
                // The signal was never sent, send it now
                attr.Control <- bindingId.ToString()
                
                let d = new Decision();
                d.DecisionType <- DecisionType.SignalExternalWorkflowExecution
                d.SignalExternalWorkflowExecutionDecisionAttributes <- attr
                response.Decisions.Add(d)

            // SignalExternalWorkflowExecution action does not block and returns nothing
            f()

        // Signal Received
        member this.Bind(SignalReceivedAction.Attributes(signalName, input, wait, markerName, markerDetails), f:(SignalReceivedResult -> RespondDecisionTaskCompletedRequest)) =
            let combinedHistory = FindSignalHistory DecisionTask signalName input markerName markerDetails

            match combinedHistory with
            // Signal Received
            | h when h.WorkflowExecutionSignaledEventAttributes <> null ->
                f(SignalReceivedResult.Received(SignalName=h.WorkflowExecutionSignaledEventAttributes.SignalName, Input=h.WorkflowExecutionSignaledEventAttributes.Input, ExternalWorkflowExecution=h.WorkflowExecutionSignaledEventAttributes.ExternalWorkflowExecution, ExternalInitiatedEventId=h.WorkflowExecutionSignaledEventAttributes.ExternalInitiatedEventId))
            // Signal not received
            | _ ->
                match wait with
                | Some(true) ->
                    blockFlag <- true
                    response
                | Some(false) | None ->
                    f(SignalReceivedResult.NotRecieved)

        // Check For Workflow Execution Cancel Requested
        member this.Bind(CheckForWorkflowExecutionCancelRequestedAction.Attributes(), f:(CheckForWorkflowExecutionCancelRequestedResult -> RespondDecisionTaskCompletedRequest)) =
            let cancelRequestedEvent = 
                DecisionTask.Events |>
                Seq.tryFindBack (fun hev -> hev.EventType = EventType.WorkflowExecutionCancelRequested)

            match cancelRequestedEvent with
            // Workflow Cancel Requsted
            | Some(event) ->
                f(CheckForWorkflowExecutionCancelRequestedResult.Requested(event.WorkflowExecutionCancelRequestedEventAttributes))
            
            // NotRequested
            | None ->
                f(CheckForWorkflowExecutionCancelRequestedResult.NotRequested)            

        // Get Workflow Execution Input
        member this.Bind(GetWorkflowExecutionInputAction.Attributes(), f:(string -> RespondDecisionTaskCompletedRequest)) =
            if DecisionTask.Events.Count >= 1 then
                let firstEvent = DecisionTask.Events.[0]
                if firstEvent.EventType = EventType.WorkflowExecutionStarted then
                    f(firstEvent.WorkflowExecutionStartedEventAttributes.Input)
                elif firstEvent.EventType = EventType.WorkflowExecutionContinuedAsNew then
                    f(firstEvent.WorkflowExecutionContinuedAsNewEventAttributes.Input)
                else
                    f(null)
            else 
                f(null)

        member this.For(enumeration:seq<'T>, f:(_ -> RespondDecisionTaskCompletedRequest)) =

            let processForBlock x = 
                if not blockFlag then f(x) |> ignore
                (not blockFlag)

            enumeration |>
            Seq.takeWhile processForBlock |>
            Seq.iter (fun x -> ())

        member this.While(condition:(unit -> bool), f:(unit -> RespondDecisionTaskCompletedRequest)) =
            while (not blockFlag) && condition() do
                f() |> ignore

        member this.Combine(exprBefore, fAfter) =
            // We assume the exprBefore decisions have been added to the response already
            // Just need to run the expression after this, which will add their decisions while executing
            if blockFlag then response else fAfter()

        member this.TryFinally(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprFinally:(unit -> unit)) =
            try 
                exprInside()
            finally
                if not blockFlag then exprFinally()

        member this.TryWith(exprInside:(unit -> RespondDecisionTaskCompletedRequest), exprWith:(Exception -> RespondDecisionTaskCompletedRequest)) =
            try
                exprInside()
            with
            | e ->
                exprWith e

    let public decider(dt: DecisionTask) = new DeciderBuilder(dt);    

