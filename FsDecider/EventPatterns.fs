module FsDecider.EventPatterns

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model


let (|EventOfType|_|) (etype:EventType) (hev:HistoryEvent) =
    if hev.EventType = etype then
        Some(hev)
    else
        None

let (|SomeEventOfType|_|) (etype:EventType) (hev:HistoryEvent option) =
    match hev with
    | Some(h) when h.EventType = etype ->
        Some(h)
    | _ -> None

let (|SignalExternalWorkflowExecutionInitiated|_|) (attr:SignalExternalWorkflowExecutionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.SignalExternalWorkflowExecutionInitiated &&
        hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.SignalName = attr.SignalName &&
        hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = attr.WorkflowId &&
        hev.SignalExternalWorkflowExecutionInitiatedEventAttributes.RunId = attr.RunId then

        Some(hev)
    else
        None

let (|RequestCancelExternalWorkflowExecutionInitiated|_|) (attr:RequestCancelExternalWorkflowExecutionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.RequestCancelExternalWorkflowExecutionInitiated &&
        hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.WorkflowId = attr.WorkflowId &&
        hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes.RunId = attr.RunId then

        Some(hev)
    else
        None            

let (|StartChildWorkflowExecutionInitiated|_|) (attr:StartChildWorkflowExecutionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.StartChildWorkflowExecutionInitiated &&
        hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId = attr.WorkflowId &&
        hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name &&
        hev.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version then

        Some(hev)
    else
        None            

let (|StartChildWorkflowExecutionFailed|_|) (attr:StartChildWorkflowExecutionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.StartChildWorkflowExecutionFailed &&
        hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowId = attr.WorkflowId &&
        hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name &&
        hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version then

        Some(hev)
    else
        None            

let (|MarkerRecorded|_|) (markerName:string) (hev:HistoryEvent) =
    if hev.EventType = EventType.MarkerRecorded &&
        hev.MarkerRecordedEventAttributes.MarkerName = markerName then

        Some(hev)
    else
        None

let (|RecordMarkerFailed|_|) (markerName:string) (hev:HistoryEvent) =
    if hev.EventType = EventType.RecordMarkerFailed &&
        hev.RecordMarkerFailedEventAttributes.MarkerName = markerName  then

        Some(hev)
    else
        None

let (|WorkflowExecutionSignaled|_|) (signalName:string) (hev:HistoryEvent) =
    if hev.EventType = EventType.WorkflowExecutionSignaled &&
        hev.WorkflowExecutionSignaledEventAttributes.SignalName = signalName then

        Some(hev)
    else
        None

let (|WorkflowExecutionException|_|) (eventType:EventType) (exceptionEvents:int64 list) (hev:HistoryEvent) =
    if hev.EventType = eventType then
        let exists =
            exceptionEvents
            |> List.exists ( (=) (hev.EventId) )

        if exists then
            None
        else 
            Some(hev)
    else
        None

let (|TimerStarted|_|) (attr:StartTimerDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.TimerStarted &&
        hev.TimerStartedEventAttributes.TimerId = attr.TimerId then

        Some(hev)
    else
        None

let (|StartTimerFailed|_|) (attr:StartTimerDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.StartTimerFailed &&
        hev.StartTimerFailedEventAttributes.TimerId = attr.TimerId then

        Some(hev)
    else
        None

let (|CancelTimerFailed|_|) (timerId:string) (hev:HistoryEvent) =
    if hev.EventType = EventType.CancelTimerFailed &&
        hev.CancelTimerFailedEventAttributes.TimerId = timerId then

        Some(hev)
    else
        None

let (|LambdaFunctionScheduled|_|) (attr:ScheduleLambdaFunctionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.LambdaFunctionScheduled &&
        hev.LambdaFunctionScheduledEventAttributes.Id = attr.Id &&
        hev.LambdaFunctionScheduledEventAttributes.Name = attr.Name then

        Some(hev)
    else
        None

let (|ScheduleLambdaFunctionFailed|_|) (attr:ScheduleLambdaFunctionDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.ScheduleLambdaFunctionFailed &&
        hev.ScheduleLambdaFunctionFailedEventAttributes.Id = attr.Id &&
        hev.ScheduleLambdaFunctionFailedEventAttributes.Name = attr.Name then

        Some(hev)
    else
        None

let (|ActivityTaskCancelRequested|_|) (activityId:string) (hev:HistoryEvent) =
    if hev.EventType = EventType.ActivityTaskCancelRequested &&
        hev.ActivityTaskCancelRequestedEventAttributes.ActivityId = activityId then

        Some(hev)
    else
        None

let (|ActivityTaskScheduled|_|) (attr:ScheduleActivityTaskDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.ActivityTaskScheduled &&
        hev.ActivityTaskScheduledEventAttributes.ActivityId = attr.ActivityId &&
        hev.ActivityTaskScheduledEventAttributes.ActivityType.Name = attr.ActivityType.Name &&
        hev.ActivityTaskScheduledEventAttributes.ActivityType.Version = attr.ActivityType.Version then

        Some(hev)
    else
        None

let (|ScheduleActivityTaskFailed|_|) (attr:ScheduleActivityTaskDecisionAttributes) (hev:HistoryEvent) =
    if hev.EventType = EventType.ScheduleActivityTaskFailed &&
        hev.ScheduleActivityTaskFailedEventAttributes.ActivityId = attr.ActivityId &&
        hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = attr.ActivityType.Name &&
        hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = attr.ActivityType.Version then

        Some(hev)
    else
        None


