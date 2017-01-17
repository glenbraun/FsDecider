namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

module HistoryWalker =

    let (|SomeEventOfType|_|) (etype:EventType) (hev:HistoryEvent option) =
        match hev with
        | Some(h) when h.EventType = etype ->
            Some(h)
        | _ -> None

    let (|EventOfType|_|) (etype:EventType) (hev:HistoryEvent) =
        if hev.EventType = etype then
            Some(hev)
        else
            None

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

    type WalkerResult =
        | Found
        | NotFound

    type HistoryWalker(HistoryEvents:System.Collections.Generic.IEnumerable<HistoryEvent>, ReverseOrder:bool) =
        let events = ResizeArray<HistoryEvent>(HistoryEvents)
        let step = if ReverseOrder then -1 else 1

        let SetCommonProperties (combinedHistory:HistoryEvent) (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        let InBounds (index:int) = events.Count > index && index >= 0

        let Start () = 
            if ReverseOrder then
                events.Count - 1
            else
                0
            
        let rec WalkToActivityTaskFinished (activityTaskStarted:HistoryEvent) (activityTaskScheduled:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) = 
            if InBounds(index) then
                match events.[index] with
                // Completed
                | EventOfType EventType.ActivityTaskCompleted hev when 
                        hev.ActivityTaskCompletedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCompletedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        combinedHistory.ActivityTaskCompletedEventAttributes <- hev.ActivityTaskCompletedEventAttributes
                        SetCommonProperties combinedHistory hev
                        WalkerResult.Found

                // Canceled
                | EventOfType EventType.ActivityTaskCanceled hev when 
                        hev.ActivityTaskCanceledEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCanceledEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        combinedHistory.ActivityTaskCanceledEventAttributes <- hev.ActivityTaskCanceledEventAttributes
                        SetCommonProperties combinedHistory hev
                        WalkerResult.Found

                // Failed
                | EventOfType EventType.ActivityTaskFailed hev when 
                        hev.ActivityTaskFailedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskFailedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        combinedHistory.ActivityTaskFailedEventAttributes <- hev.ActivityTaskFailedEventAttributes
                        SetCommonProperties combinedHistory hev
                        WalkerResult.Found

                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut hev when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        combinedHistory.ActivityTaskTimedOutEventAttributes <- hev.ActivityTaskTimedOutEventAttributes
                        SetCommonProperties combinedHistory hev
                        WalkerResult.Found

                | _ ->
                    WalkToActivityTaskFinished activityTaskStarted activityTaskScheduled (index+step) combinedHistory
            else
                WalkerResult.NotFound


        let rec WalkToActivityTaskStartedOrTimedOut (activityTaskScheduled:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =  
            if InBounds(index) then
                match events.[index] with
                // Started
                | EventOfType EventType.ActivityTaskStarted hev when 
                        hev.ActivityTaskStartedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId ->

                    combinedHistory.ActivityTaskStartedEventAttributes <- hev.ActivityTaskStartedEventAttributes
                    SetCommonProperties combinedHistory hev
                                    
                    WalkToActivityTaskFinished hev activityTaskScheduled (index+step) combinedHistory |> ignore
                    WalkerResult.Found

                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut hev when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = 0L &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                    combinedHistory.ActivityTaskTimedOutEventAttributes <- hev.ActivityTaskTimedOutEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToActivityTaskStartedOrTimedOut activityTaskScheduled (index+step) combinedHistory
            else
                WalkerResult.NotFound
    
        let rec WalkToActivityTaskScheduledOrScheduleFailed (attr:ScheduleActivityTaskDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | ActivityTaskScheduled(attr) hev ->

                    combinedHistory.ActivityTaskScheduledEventAttributes <- hev.ActivityTaskScheduledEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToActivityTaskStartedOrTimedOut hev (index+step) combinedHistory |> ignore
                    WalkerResult.Found
                
                | ScheduleActivityTaskFailed(attr) hev ->

                    combinedHistory.ScheduleActivityTaskFailedEventAttributes <- hev.ScheduleActivityTaskFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToActivityTaskScheduledOrScheduleFailed attr (index+step) combinedHistory
            else
                WalkerResult.NotFound


        let rec WalkToActivityTaskCancelRequestedOrRequestFailed (activityId:string) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =  
            if InBounds(index) then
                match events.[index] with
                | ActivityTaskCancelRequested(activityId) hev ->

                    combinedHistory.ActivityTaskCancelRequestedEventAttributes <- hev.ActivityTaskCancelRequestedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.RequestCancelActivityTaskFailed hev when
                            hev.RequestCancelActivityTaskFailedEventAttributes.ActivityId = activityId ->

                    combinedHistory.RequestCancelActivityTaskFailedEventAttributes <- hev.RequestCancelActivityTaskFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToActivityTaskCancelRequestedOrRequestFailed activityId (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToLambdaFunctionFinished (lambdaFunctionStarted:HistoryEvent) (lambdaFunctionScheduled:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                // Completed
                | EventOfType EventType.LambdaFunctionCompleted hev when 
                        hev.LambdaFunctionCompletedEventAttributes.ScheduledEventId = lambdaFunctionScheduled.EventId &&
                        hev.LambdaFunctionCompletedEventAttributes.StartedEventId = lambdaFunctionStarted.EventId ->

                    combinedHistory.LambdaFunctionCompletedEventAttributes <- hev.LambdaFunctionCompletedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                // Failed
                | EventOfType EventType.LambdaFunctionFailed hev when 
                        hev.LambdaFunctionFailedEventAttributes.ScheduledEventId = lambdaFunctionScheduled.EventId &&
                        hev.LambdaFunctionFailedEventAttributes.StartedEventId = lambdaFunctionStarted.EventId ->

                    combinedHistory.LambdaFunctionFailedEventAttributes <- hev.LambdaFunctionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                // TimedOut
                | EventOfType EventType.LambdaFunctionTimedOut hev when 
                        hev.LambdaFunctionTimedOutEventAttributes.ScheduledEventId = lambdaFunctionScheduled.EventId &&
                        hev.LambdaFunctionTimedOutEventAttributes.StartedEventId = lambdaFunctionStarted.EventId ->

                    combinedHistory.LambdaFunctionTimedOutEventAttributes <- hev.LambdaFunctionTimedOutEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToLambdaFunctionFinished lambdaFunctionStarted lambdaFunctionScheduled (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToLambdaFunctionStartedOrStartFailed (lambdaFunctionScheduled:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                // LambdaFunctionStarted
                | EventOfType EventType.LambdaFunctionStarted hev when 
                        hev.LambdaFunctionStartedEventAttributes.ScheduledEventId = lambdaFunctionScheduled.EventId ->

                    combinedHistory.LambdaFunctionStartedEventAttributes <- hev.LambdaFunctionStartedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToLambdaFunctionFinished hev lambdaFunctionScheduled (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                // StartFailed
                | EventOfType EventType.StartLambdaFunctionFailed hev when 
                        hev.StartLambdaFunctionFailedEventAttributes.ScheduledEventId = lambdaFunctionScheduled.EventId -> 

                    combinedHistory.StartLambdaFunctionFailedEventAttributes <- hev.StartLambdaFunctionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToLambdaFunctionStartedOrStartFailed lambdaFunctionScheduled (index+step) combinedHistory
            else
                WalkerResult.NotFound
            
        let rec WalkToLambdaFunctionScheduledOrScheduleFailed (attr:ScheduleLambdaFunctionDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | LambdaFunctionScheduled(attr) hev ->

                    combinedHistory.LambdaFunctionScheduledEventAttributes <- hev.LambdaFunctionScheduledEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToLambdaFunctionStartedOrStartFailed (hev) (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                
                | ScheduleLambdaFunctionFailed(attr) hev ->

                    combinedHistory.ScheduleLambdaFunctionFailedEventAttributes <- hev.ScheduleLambdaFunctionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToLambdaFunctionScheduledOrScheduleFailed attr (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToTimerFinished (timerStarted:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType EventType.TimerFired hev when
                    hev.TimerFiredEventAttributes.StartedEventId = timerStarted.EventId ->

                    combinedHistory.TimerFiredEventAttributes <- hev.TimerFiredEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.TimerCanceled hev when
                    hev.TimerCanceledEventAttributes.StartedEventId = timerStarted.EventId ->

                    combinedHistory.TimerCanceledEventAttributes <- hev.TimerCanceledEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToTimerFinished (timerStarted) (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToTimerStartedOrFailed (attr:StartTimerDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | TimerStarted(attr) hev ->

                    combinedHistory.TimerStartedEventAttributes <- hev.TimerStartedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToTimerFinished hev (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                | StartTimerFailed(attr) hev -> 

                    combinedHistory.StartTimerFailedEventAttributes <- hev.StartTimerFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToTimerStartedOrFailed attr (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToCancelTimerOrFailed (timerId:string) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | CancelTimerFailed(timerId) hev -> 

                    combinedHistory.CancelTimerFailedEventAttributes <- hev.CancelTimerFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToCancelTimerOrFailed timerId (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToWorkflowExecutionException (eventType:EventType) (exceptionEvents:int64 list) (index:int) : HistoryEvent option =
            if InBounds(index) then
                match events.[index] with
                | WorkflowExecutionException eventType exceptionEvents hev ->
                    Some(hev)

                | _ ->
                    WalkToWorkflowExecutionException eventType exceptionEvents (index+step)
            else
                None

        let rec WalkToWorkflowExecutionSignaled (signalName:string) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | WorkflowExecutionSignaled(signalName) hev->

                    combinedHistory.WorkflowExecutionSignaledEventAttributes <- hev.WorkflowExecutionSignaledEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToWorkflowExecutionSignaled signalName (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToMarkerRecordedOrFailed (markerName:string) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | MarkerRecorded(markerName) hev ->

                    combinedHistory.MarkerRecordedEventAttributes <- hev.MarkerRecordedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | RecordMarkerFailed(markerName) hev ->

                    combinedHistory.RecordMarkerFailedEventAttributes <- hev.RecordMarkerFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToMarkerRecordedOrFailed markerName (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToChildWorkflowExecutionFinished (childWorkflowExecutionStarted:HistoryEvent) (startChildWorkflowExecutionInitiated:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType EventType.ChildWorkflowExecutionCompleted hev when
                    hev.ChildWorkflowExecutionCompletedEventAttributes.StartedEventId = childWorkflowExecutionStarted.EventId &&
                    hev.ChildWorkflowExecutionCompletedEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionCompletedEventAttributes <- hev.ChildWorkflowExecutionCompletedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.ChildWorkflowExecutionCanceled hev when
                    hev.ChildWorkflowExecutionCanceledEventAttributes.StartedEventId = childWorkflowExecutionStarted.EventId &&
                    hev.ChildWorkflowExecutionCanceledEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionCanceledEventAttributes <- hev.ChildWorkflowExecutionCanceledEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.ChildWorkflowExecutionFailed hev when
                    hev.ChildWorkflowExecutionFailedEventAttributes.StartedEventId = childWorkflowExecutionStarted.EventId &&
                    hev.ChildWorkflowExecutionFailedEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionFailedEventAttributes <- hev.ChildWorkflowExecutionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found
            
                | EventOfType EventType.ChildWorkflowExecutionTerminated hev when
                    hev.ChildWorkflowExecutionTerminatedEventAttributes.StartedEventId = childWorkflowExecutionStarted.EventId &&
                    hev.ChildWorkflowExecutionTerminatedEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionTerminatedEventAttributes <- hev.ChildWorkflowExecutionTerminatedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.ChildWorkflowExecutionTimedOut hev when
                    hev.ChildWorkflowExecutionTimedOutEventAttributes.StartedEventId = childWorkflowExecutionStarted.EventId &&
                    hev.ChildWorkflowExecutionTimedOutEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionTimedOutEventAttributes <- hev.ChildWorkflowExecutionTimedOutEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToChildWorkflowExecutionFinished childWorkflowExecutionStarted startChildWorkflowExecutionInitiated (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToChildWorkflowExecutionStarted (startChildWorkflowExecutionInitiated:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType EventType.ChildWorkflowExecutionStarted hev when
                    hev.ChildWorkflowExecutionStartedEventAttributes.InitiatedEventId = startChildWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ChildWorkflowExecutionStartedEventAttributes <- hev.ChildWorkflowExecutionStartedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToChildWorkflowExecutionFinished hev startChildWorkflowExecutionInitiated (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                | _ ->
                    WalkToChildWorkflowExecutionStarted startChildWorkflowExecutionInitiated (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToStartChildWorkflowExecutionOrFailed (attr:StartChildWorkflowExecutionDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | StartChildWorkflowExecutionInitiated(attr) hev ->

                    combinedHistory.StartChildWorkflowExecutionInitiatedEventAttributes <- hev.StartChildWorkflowExecutionInitiatedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToChildWorkflowExecutionStarted hev (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                | StartChildWorkflowExecutionFailed(attr) hev ->

                    combinedHistory.StartChildWorkflowExecutionFailedEventAttributes <- hev.StartChildWorkflowExecutionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToStartChildWorkflowExecutionOrFailed attr (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToExternalWorkflowExecutionCancelRequestedOrFailed (requestCancelExternalWorkflowExecutionInitiated:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult = 
            if InBounds(index) then
                match events.[index] with
                | EventOfType EventType.ExternalWorkflowExecutionCancelRequested hev when
                    hev.ExternalWorkflowExecutionCancelRequestedEventAttributes.InitiatedEventId = requestCancelExternalWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ExternalWorkflowExecutionCancelRequestedEventAttributes <- hev.ExternalWorkflowExecutionCancelRequestedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.RequestCancelExternalWorkflowExecutionFailed hev when
                    hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = requestCancelExternalWorkflowExecutionInitiated.EventId ->

                    combinedHistory.RequestCancelExternalWorkflowExecutionFailedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToExternalWorkflowExecutionCancelRequestedOrFailed requestCancelExternalWorkflowExecutionInitiated (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToRequestCancelExternalWorkflowExecutionInitiated (attr:RequestCancelExternalWorkflowExecutionDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult = 
            if InBounds(index) then
                match events.[index] with
                | RequestCancelExternalWorkflowExecutionInitiated(attr) hev ->

                    combinedHistory.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <- hev.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToExternalWorkflowExecutionCancelRequestedOrFailed hev (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                | _ ->
                    WalkToRequestCancelExternalWorkflowExecutionInitiated attr (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToExternalWorkflowExecutionSignaledOrFailed (signalExternalWorkflowExecutionInitiated:HistoryEvent) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType EventType.ExternalWorkflowExecutionSignaled hev when
                    hev.ExternalWorkflowExecutionSignaledEventAttributes.InitiatedEventId = signalExternalWorkflowExecutionInitiated.EventId ->

                    combinedHistory.ExternalWorkflowExecutionSignaledEventAttributes <- hev.ExternalWorkflowExecutionSignaledEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | EventOfType EventType.SignalExternalWorkflowExecutionFailed hev when
                    hev.SignalExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = signalExternalWorkflowExecutionInitiated.EventId ->

                    combinedHistory.SignalExternalWorkflowExecutionFailedEventAttributes <- hev.SignalExternalWorkflowExecutionFailedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToExternalWorkflowExecutionSignaledOrFailed signalExternalWorkflowExecutionInitiated (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToSignalExternalWorkflowExecutionInitiated (attr:SignalExternalWorkflowExecutionDecisionAttributes) (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | SignalExternalWorkflowExecutionInitiated(attr) hev ->
                    combinedHistory.SignalExternalWorkflowExecutionInitiatedEventAttributes <- hev.SignalExternalWorkflowExecutionInitiatedEventAttributes
                    SetCommonProperties combinedHistory hev

                    WalkToExternalWorkflowExecutionSignaledOrFailed hev (index+step) combinedHistory |> ignore

                    WalkerResult.Found

                | _ ->
                    WalkToSignalExternalWorkflowExecutionInitiated attr (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToWorkflowExecutionCancelRequested (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType(EventType.WorkflowExecutionCancelRequested) hev ->
                    combinedHistory.WorkflowExecutionCancelRequestedEventAttributes <- hev.WorkflowExecutionCancelRequestedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToWorkflowExecutionCancelRequested (index+step) combinedHistory
            else
                WalkerResult.NotFound
            
        let rec WalkToWorkflowExecutionStarted (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType(EventType.WorkflowExecutionStarted) hev ->
                    combinedHistory.WorkflowExecutionStartedEventAttributes <- hev.WorkflowExecutionStartedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToWorkflowExecutionStarted (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let rec WalkToDecisionTaskCompleted (index:int) (combinedHistory:HistoryEvent) : WalkerResult =
            if InBounds(index) then
                match events.[index] with
                | EventOfType(EventType.DecisionTaskCompleted) hev ->
                    combinedHistory.DecisionTaskCompletedEventAttributes <- hev.DecisionTaskCompletedEventAttributes
                    SetCommonProperties combinedHistory hev
                    WalkerResult.Found

                | _ ->
                    WalkToDecisionTaskCompleted (index+step) combinedHistory
            else
                WalkerResult.NotFound

        let HistoryOrNone (walker:(HistoryEvent -> WalkerResult)) : HistoryEvent option =
            let combinedHistory = new HistoryEvent()
            let result = walker combinedHistory
            match result with
            | WalkerResult.Found    -> Some(combinedHistory)
            | WalkerResult.NotFound -> None
            
        member this.FindActivityTask (attr:ScheduleActivityTaskDecisionAttributes) : HistoryEvent option = 
            HistoryOrNone (WalkToActivityTaskScheduledOrScheduleFailed attr (Start()))
            
        member this.FindRequestCancelActivityTask (activityId:string) : HistoryEvent option =
            HistoryOrNone (WalkToActivityTaskCancelRequestedOrRequestFailed activityId (Start()))

        member this.FindLambdaFunction (attr:ScheduleLambdaFunctionDecisionAttributes) : HistoryEvent option =
            HistoryOrNone (WalkToLambdaFunctionScheduledOrScheduleFailed attr (Start()))

        member this.FindTimer (attr:StartTimerDecisionAttributes) : HistoryEvent option =
            HistoryOrNone (WalkToTimerStartedOrFailed attr (Start()))

        member this.FindCancelTimer (timerId:string) : HistoryEvent option =
            HistoryOrNone (WalkToCancelTimerOrFailed timerId (Start()))

        member this.FindWorkflowException (eventType:EventType, exceptionEvents:int64 list) : (HistoryEvent option) =
            WalkToWorkflowExecutionException eventType exceptionEvents (Start())

        member this.FindSignaled (signalName:string) : (HistoryEvent option) =
           HistoryOrNone (WalkToWorkflowExecutionSignaled signalName (Start()))

        member this.FindMarker (markerName:string) : (HistoryEvent option) =
           HistoryOrNone (WalkToMarkerRecordedOrFailed markerName (Start()))

        member this.FindChildWorkflowExecution (attr:StartChildWorkflowExecutionDecisionAttributes) : (HistoryEvent option) =
           HistoryOrNone (WalkToStartChildWorkflowExecutionOrFailed attr (Start()))

        member this.FindRequestCancelExternalWorkflowExecution (attr:RequestCancelExternalWorkflowExecutionDecisionAttributes) : (HistoryEvent option) =
           HistoryOrNone (WalkToRequestCancelExternalWorkflowExecutionInitiated attr (Start()))

        member this.FindSignalExternalWorkflow (attr:SignalExternalWorkflowExecutionDecisionAttributes) : (HistoryEvent option) =
           HistoryOrNone (WalkToSignalExternalWorkflowExecutionInitiated attr (Start()))

        member this.FindWorkflowExecutionCancelRequested () =
            HistoryOrNone (WalkToWorkflowExecutionCancelRequested (Start()))

        member this.FindWorkflowExecutionStarted () =
            HistoryOrNone (WalkToWorkflowExecutionStarted (Start()))

        member this.FindDecisionTaskCompleted () =
            HistoryOrNone (WalkToDecisionTaskCompleted (Start()))