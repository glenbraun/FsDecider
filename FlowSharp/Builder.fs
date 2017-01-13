namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

type Builder (DecisionTask:DecisionTask) =
    let response = new RespondDecisionTaskCompletedRequest(Decisions = ResizeArray<Decision>(), TaskToken = DecisionTask.TaskToken)            
    let mutable bindingId = 0
    let mutable blockFlag = false
    let mutable exceptionEvents = List.empty<int64>
    let events = ResizeArray<HistoryEvent>(DecisionTask.Events)

    let NextBindingId() =
        bindingId <- bindingId + 1
        bindingId

    let AddExceptionEventId eventId = 
        exceptionEvents <- eventId :: exceptionEvents

    let HasExceptionBeenThrownForEvent eventId = 
        exceptionEvents
        |> List.exists ( (=) eventId )

    let (|EventOfType|_|) (etype:EventType) (hev:HistoryEvent) =
        if hev.EventType = etype then
            Some(hev)
        else
            None


    let FindSignalHistory (decisionTask:DecisionTask) (signalName:string) =
        let combinedHistory = new HistoryEvent()

        let findSignal =
            decisionTask.Events
            |> Seq.tryFindBack (fun hev -> hev.EventType = EventType.WorkflowExecutionSignaled && hev.WorkflowExecutionSignaledEventAttributes.SignalName = signalName)

        match findSignal with
        | Some(h) -> 
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp
            combinedHistory.WorkflowExecutionSignaledEventAttributes <- h.WorkflowExecutionSignaledEventAttributes
        | None -> ()
        
        // Return the combined history
        combinedHistory

    let FindMarkerHistory (decisionTask:DecisionTask) (markerName:string) =
        let combinedHistory = new HistoryEvent()

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if hev.EventType = EventType.MarkerRecorded && hev.MarkerRecordedEventAttributes.MarkerName = markerName then
                setCommonProperties(hev)
                combinedHistory.MarkerRecordedEventAttributes <- hev.MarkerRecordedEventAttributes

            elif hev.EventType = EventType.RecordMarkerFailed && hev.RecordMarkerFailedEventAttributes.MarkerName = markerName then
                setCommonProperties(hev)
                combinedHistory.RecordMarkerFailedEventAttributes <- hev.RecordMarkerFailedEventAttributes
        
        // Return the combined history
        combinedHistory

    let FindChildWorkflowExecutionHistory (decisionTask:DecisionTask) (bindingId:int) (workflowType:WorkflowType) (workflowId:string) =
        let combinedHistory = new HistoryEvent()
        let bindingIdString = bindingId.ToString()

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

            // StartChildWorkflowExecutionFailed
            elif hev.EventType = EventType.StartChildWorkflowExecutionFailed &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.StartChildWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.StartChildWorkflowExecutionFailedEventAttributes <- hev.StartChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionStarted
            elif hev.EventType = EventType.ChildWorkflowExecutionStarted &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionStartedEventAttributes <- hev.ChildWorkflowExecutionStartedEventAttributes

            // ChildWorkflowExecutionCompleted
            elif hev.EventType = EventType.ChildWorkflowExecutionCompleted &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCompletedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCompletedEventAttributes <- hev.ChildWorkflowExecutionCompletedEventAttributes

            // ChildWorkflowExecutionFailed
            elif hev.EventType = EventType.ChildWorkflowExecutionFailed &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionFailedEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionFailedEventAttributes <- hev.ChildWorkflowExecutionFailedEventAttributes

            // ChildWorkflowExecutionTimedOut
            elif hev.EventType = EventType.ChildWorkflowExecutionTimedOut &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionTimedOutEventAttributes <- hev.ChildWorkflowExecutionTimedOutEventAttributes

            // ChildWorkflowExecutionCanceled
            elif hev.EventType = EventType.ChildWorkflowExecutionCanceled &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Name = workflowType.Name &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowType.Version = workflowType.Version &&
                                 hev.ChildWorkflowExecutionCanceledEventAttributes.WorkflowExecution.WorkflowId = workflowId then
                setCommonProperties(hev)
                combinedHistory.ChildWorkflowExecutionCanceledEventAttributes <- hev.ChildWorkflowExecutionCanceledEventAttributes

            // ChildWorkflowExecutionTerminated
            elif hev.EventType = EventType.ChildWorkflowExecutionTerminated &&
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
        let mutable initiatedEventId = 0L

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
        let mutable initiatedEventId = 0L

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
                initiatedEventId <- hev.EventId

            elif hev.EventType = EventType.SignalExternalWorkflowExecutionFailed &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.Control = bindingIdString &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.InitiatedEventId = initiatedEventId &&
                 hev.SignalExternalWorkflowExecutionFailedEventAttributes.WorkflowId = workflowId then

                setCommonProperties(hev)
                combinedHistory.SignalExternalWorkflowExecutionFailedEventAttributes <- hev.SignalExternalWorkflowExecutionFailedEventAttributes

            elif hev.EventType = EventType.ExternalWorkflowExecutionSignaled &&
                 hev.ExternalWorkflowExecutionSignaledEventAttributes.InitiatedEventId = initiatedEventId && 
                 hev.ExternalWorkflowExecutionSignaledEventAttributes.WorkflowExecution.WorkflowId = workflowId then

                setCommonProperties(hev)
                combinedHistory.ExternalWorkflowExecutionSignaledEventAttributes <- hev.ExternalWorkflowExecutionSignaledEventAttributes
        
        // Return the combined history
        combinedHistory
        
    let FindReturnExceptionHistory (decisionTask:DecisionTask) =
        let combinedHistory = new HistoryEvent()

        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            if HasExceptionBeenThrownForEvent (hev.EventId) then
                // If an exception has been thrown once for this event, don't let it be flagged twice
                ()
            else
                if hev.EventType = EventType.CompleteWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.CompleteWorkflowExecutionFailedEventAttributes <- hev.CompleteWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.CancelWorkflowExecutionFailed  then
                    setCommonProperties(hev)
                    combinedHistory.CancelWorkflowExecutionFailedEventAttributes <- hev.CancelWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.FailWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.FailWorkflowExecutionFailedEventAttributes <- hev.FailWorkflowExecutionFailedEventAttributes

                elif hev.EventType = EventType.ContinueAsNewWorkflowExecutionFailed then
                    setCommonProperties(hev)
                    combinedHistory.ContinueAsNewWorkflowExecutionFailedEventAttributes <- hev.ContinueAsNewWorkflowExecutionFailedEventAttributes
                        
        // Return the combined history
        combinedHistory

    let FindScheduleActivityTaskResult (decision:ScheduleActivityTaskDecisionAttributes) : ScheduleActivityTaskResult =  

        let rec TryFindFinished (index:int) (activityTaskStarted:HistoryEvent) (activityTaskScheduled:HistoryEvent) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option = 
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                // Completed
                | EventOfType EventType.ActivityTaskCompleted _ when 
                        hev.ActivityTaskCompletedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCompletedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Completed(hev.ActivityTaskCompletedEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // Canceled
                | EventOfType EventType.ActivityTaskCanceled _ when 
                        hev.ActivityTaskCanceledEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskCanceledEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Canceled(hev.ActivityTaskCanceledEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // Failed
                | EventOfType EventType.ActivityTaskFailed _ when 
                        hev.ActivityTaskFailedEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskFailedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.Failed(hev.ActivityTaskFailedEventAttributes, 
                                                                  activityTaskStarted.ActivityTaskStartedEventAttributes, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut _ when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = activityTaskStarted.EventId &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes, 
                                                                  Some(activityTaskStarted.ActivityTaskStartedEventAttributes), 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindFinished (index+1) activityTaskStarted activityTaskScheduled hev

                | _ ->
                    TryFindFinished (index+1) activityTaskStarted activityTaskScheduled decisionTaskCompleted


        let rec TryFindStartedOrTimedOut (index:int) (activityTaskScheduled:HistoryEvent) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with

                // Started
                | EventOfType EventType.ActivityTaskStarted _ when 
                        hev.ActivityTaskStartedEventAttributes.ScheduledEventId = activityTaskScheduled.EventId ->
                
                    let tryFinished = TryFindFinished (index+1) hev activityTaskScheduled decisionTaskCompleted
                    match tryFinished with
                    | Some(_)  -> tryFinished
                    | None     -> Some(ScheduleActivityTaskResult.Started(hev.ActivityTaskStartedEventAttributes, 
                                                                          activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                          decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                // TimedOut
                | EventOfType EventType.ActivityTaskTimedOut _ when 
                        hev.ActivityTaskTimedOutEventAttributes.StartedEventId = 0L &&
                        hev.ActivityTaskTimedOutEventAttributes.ScheduledEventId = activityTaskScheduled.EventId -> 

                        Some(ScheduleActivityTaskResult.TimedOut(hev.ActivityTaskTimedOutEventAttributes, 
                                                                  None, 
                                                                  activityTaskScheduled.ActivityTaskScheduledEventAttributes, 
                                                                  decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindStartedOrTimedOut (index+1) activityTaskScheduled hev

                | _ ->
                    TryFindStartedOrTimedOut (index+1) activityTaskScheduled decisionTaskCompleted


        let rec TryFindScheduledOrScheduleFailed (index:int) (decisionTaskCompleted:HistoryEvent) : ScheduleActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                | EventOfType EventType.ActivityTaskScheduled _ when
                                hev.ActivityTaskScheduledEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityId = decision.ActivityId &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityType.Name = decision.ActivityType.Name &&
                                hev.ActivityTaskScheduledEventAttributes.ActivityType.Version = decision.ActivityType.Version ->

                    let tryStarted = TryFindStartedOrTimedOut (index+1) hev decisionTaskCompleted
                    match tryStarted with
                    | Some(_)   -> tryStarted
                    | None      -> Some(ScheduleActivityTaskResult.Scheduled(hev.ActivityTaskScheduledEventAttributes, 
                                                                             decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                
                | EventOfType EventType.ScheduleActivityTaskFailed _ when
                                hev.ScheduleActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityId = decision.ActivityId &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name = decision.ActivityType.Name &&
                                hev.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version = decision.ActivityType.Version ->

                    Some(ScheduleActivityTaskResult.ScheduleFailed(hev.ScheduleActivityTaskFailedEventAttributes, 
                                                                   decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindScheduledOrScheduleFailed (index+1) hev

                | _ ->
                    TryFindScheduledOrScheduleFailed (index+1) decisionTaskCompleted

                
        let rec TryFindDecisionTaskCompleted (index:int) : ScheduleActivityTaskResult option =
            if index >= events.Count then
                None
            else
                let hev = events.[index]
                
                if hev.EventType = EventType.DecisionTaskCompleted then
                    let tryScheduled = TryFindScheduledOrScheduleFailed (index+1) hev
                    match tryScheduled with
                    | Some(_)   -> tryScheduled
                    | None      -> TryFindDecisionTaskCompleted (index+1)
                else
                    TryFindDecisionTaskCompleted (index+1)

        let tryDecisionTaskCompleted = TryFindDecisionTaskCompleted 1
        match tryDecisionTaskCompleted with
        | Some(result)  -> result
        | None          -> ScheduleActivityTaskResult.Scheduling(decision)


    let FindRequestCancelActivityTaskResult (eventId:int) (activityId:string) : (RequestCancelActivityTaskResult option) = 

        let rec TryFindRequestedOrFailed (index:int) (activityId:string) (decisionTaskCompleted:HistoryEvent) : RequestCancelActivityTaskResult option =  
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                match hev with
                | EventOfType EventType.ActivityTaskCancelRequested _ when
                            hev.ActivityTaskCancelRequestedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                            hev.ActivityTaskCancelRequestedEventAttributes.ActivityId = activityId ->

                    Some(RequestCancelActivityTaskResult.CancelRequested(hev.ActivityTaskCancelRequestedEventAttributes, 
                                                                         decisionTaskCompleted.DecisionTaskCompletedEventAttributes))

                | EventOfType EventType.RequestCancelActivityTaskFailed _ when
                            hev.RequestCancelActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId = decisionTaskCompleted.EventId &&
                            hev.RequestCancelActivityTaskFailedEventAttributes.ActivityId = activityId ->

                    Some(RequestCancelActivityTaskResult.RequestCancelFailed(hev.RequestCancelActivityTaskFailedEventAttributes, 
                                                                             decisionTaskCompleted.DecisionTaskCompletedEventAttributes))
                | EventOfType EventType.DecisionTaskCompleted _ -> 
                    TryFindRequestedOrFailed (index+1) activityId hev

                | _ ->
                    TryFindRequestedOrFailed (index+1) activityId decisionTaskCompleted

        let rec TryFindDecisionTaskCompleted (index:int) (activityId:string) : RequestCancelActivityTaskResult option =
            if index >= events.Count then
                None
            else
                let hev = events.[index]

                if hev.EventType = EventType.DecisionTaskCompleted then
                    TryFindRequestedOrFailed (index+1) activityId hev                    
                else 
                    TryFindDecisionTaskCompleted (index+1) activityId

        TryFindDecisionTaskCompleted eventId activityId
                    
    
    let FindLambdaFunctionHistory (decisionTask:DecisionTask) (id:string) (name:string) =
        let combinedHistory = new HistoryEvent()
        let mutable scheduledEventId = 0L
        let mutable startedEventId = 0L
            
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

    let FindTimerHistory (decisionTask:DecisionTask) (control:string) =
        let combinedHistory = new HistoryEvent()
        let mutable startedEventId = -1L
        let mutable timerId : string = null
            
        let setCommonProperties (h:HistoryEvent) =
            combinedHistory.EventType <- h.EventType
            combinedHistory.EventId <- h.EventId
            combinedHistory.EventTimestamp <- h.EventTimestamp

        for hev in decisionTask.Events do
            // Skip these common DecisionTask events right away
            if hev.EventType = EventType.DecisionTaskScheduled || hev.EventType = EventType.DecisionTaskStarted || hev.EventType = EventType.DecisionTaskCompleted then ()
            
            // CancelTimerFailed
            elif hev.EventType = EventType.CancelTimerFailed && hev.CancelTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.CancelTimerFailedEventAttributes <- hev.CancelTimerFailedEventAttributes

            // StartTimerFailed
            elif hev.EventType = EventType.StartTimerFailed && hev.StartTimerFailedEventAttributes.DecisionTaskCompletedEventId > decisionTask.PreviousStartedEventId then
                combinedHistory.StartTimerFailedEventAttributes <- hev.StartTimerFailedEventAttributes

            // TimerStarted
            elif hev.EventType = EventType.TimerStarted && hev.TimerStartedEventAttributes.Control = control then
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

        // Return the combined history
        combinedHistory

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

    member this.Return(result:ReturnResult) =
        blockFlag <- true

        // Look for possible return failures
        let combinedHistory = FindReturnExceptionHistory DecisionTask

        match result with
        | ReturnResult.RespondDecisionTaskCompleted ->
            ()

        | ReturnResult.CompleteWorkflowExecution(r) -> 
            match (combinedHistory) with
            | h when h.CompleteWorkflowExecutionFailedEventAttributes <> null ->
                // A previous attempt was made to complete this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (h.EventId)
                raise (CompleteWorkflowExecutionFailedException(h.CompleteWorkflowExecutionFailedEventAttributes))

            | _ -> 
                let decision = new Decision();
                decision.DecisionType <- DecisionType.CompleteWorkflowExecution
                decision.CompleteWorkflowExecutionDecisionAttributes <- new CompleteWorkflowExecutionDecisionAttributes();
                decision.CompleteWorkflowExecutionDecisionAttributes.Result <- r
                response.Decisions.Add(decision)

        | ReturnResult.CancelWorkflowExecution(details) ->
            match (combinedHistory) with
            | h when h.CancelWorkflowExecutionFailedEventAttributes <> null ->
                // A previous attempt was made to cancel this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (h.EventId)
                raise (CancelWorkflowExecutionFailedException(h.CancelWorkflowExecutionFailedEventAttributes))

            | _ ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.CancelWorkflowExecution
                decision.CancelWorkflowExecutionDecisionAttributes <- new CancelWorkflowExecutionDecisionAttributes();
                decision.CancelWorkflowExecutionDecisionAttributes.Details <- details
                response.Decisions.Add(decision)

        | ReturnResult.FailWorkflowExecution(reason, details) ->
            match (combinedHistory) with
            | h when h.FailWorkflowExecutionFailedEventAttributes <> null ->
                // A previous attempt was made to fail this workflow, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (h.EventId)
                raise (FailWorkflowExecutionFailedException(h.FailWorkflowExecutionFailedEventAttributes))
            | _ ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.FailWorkflowExecution
                decision.FailWorkflowExecutionDecisionAttributes <- new FailWorkflowExecutionDecisionAttributes();
                decision.FailWorkflowExecutionDecisionAttributes.Reason <- reason
                decision.FailWorkflowExecutionDecisionAttributes.Details <- details
                response.Decisions.Add(decision)

        | ReturnResult.ContinueAsNewWorkflowExecution(attr) ->
            match (combinedHistory) with
            | h when h.ContinueAsNewWorkflowExecutionFailedEventAttributes <> null ->
                // A previous attempt was made to continue this workflow as new, but it failed
                // Raise an exception that the decider can process
                blockFlag <- false
                AddExceptionEventId (h.EventId)
                raise (ContinueAsNewWorkflowExecutionFailedException(h.ContinueAsNewWorkflowExecutionFailedEventAttributes))
            | _ ->
                let decision = new Decision();
                decision.DecisionType <- DecisionType.ContinueAsNewWorkflowExecution
                decision.ContinueAsNewWorkflowExecutionDecisionAttributes <- attr;
                response.Decisions.Add(decision)

        response

    member this.Return(result:string) = this.Return(ReturnResult.CompleteWorkflowExecution(result))
    member this.Return(result:unit) = this.Return(ReturnResult.RespondDecisionTaskCompleted)
            
    // Start and Wait for Activity Task
    member this.Bind(ScheduleAndWaitForActivityTaskAction.Attributes(attr), f:(ScheduleActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        // The idea is that with the same decider, the sequence of calls to Bind will be the same. The bindingId is used in the .Control 
        // properties and is used when matching the execution history to a DeciderAction
        let bindingId = NextBindingId()

        let result = FindScheduleActivityTaskResult attr

        match (result) with
        // Scheduling
        | ScheduleActivityTaskResult.Scheduling(_) ->
            blockFlag <- true

            let d = new Decision();
            d.DecisionType <- DecisionType.ScheduleActivityTask
            d.ScheduleActivityTaskDecisionAttributes <- attr
            response.Decisions.Add(d)
            response

        // Started
        | ScheduleActivityTaskResult.Started(_) ->
            blockFlag <- true
            response

        | _ -> 
            f(result)

    // Schedule Activity Task
    member this.Bind(ScheduleActivityTaskAction.Attributes(attr), f:(ScheduleActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 
        let bindingId = NextBindingId()

        let result = FindScheduleActivityTaskResult attr

        match (result) with
        // Scheduling
        | ScheduleActivityTaskResult.Scheduling(_) ->
            let d = new Decision();
            d.DecisionType <- DecisionType.ScheduleActivityTask
            d.ScheduleActivityTaskDecisionAttributes <- attr
            response.Decisions.Add(d)

        | _ -> ()

        f(result)
    
    // Wait For Activity Task
    member this.Bind(WaitForActivityTaskAction.ScheduleResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =

        match (result.IsFinished()) with 
        | true -> f()
        | false -> 
            blockFlag <- true
            response       

    // Wait For Any Activity Tasks
    member this.Bind(WaitForAnyActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        match anyFinished with
        | true -> f()
        | false -> 
            blockFlag <- true
            response       

    // Wait For All Activity Tasks
    member this.Bind(WaitForAllActivityTaskAction.ScheduleResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        match allFinished with
        | true -> f()
        | false -> 
            blockFlag <- true
            response 

    // Request Cancel Activity Task 
    member this.Bind(RequestCancelActivityTaskAction.ScheduleResult(schedule), f:(RequestCancelActivityTaskResult -> RespondDecisionTaskCompletedRequest)) = 

        match schedule with 
        // Scheduling
        | ScheduleActivityTaskResult.Scheduling(_) -> 
            blockFlag <- true
            response

        // ScheduleFailed
        | ScheduleActivityTaskResult.ScheduleFailed(_) -> f(RequestCancelActivityTaskResult.ActivityScheduleFailed)

        | _ -> 
            if (schedule.IsFinished()) then
                f(RequestCancelActivityTaskResult.ActivityFinished)
            else
               
                let scheduleAttr = 
                    match schedule with
                    | ScheduleActivityTaskResult.Scheduled(s, _)  -> s
                    | ScheduleActivityTaskResult.Started(_, s, _) -> s
                    | _ -> failwith "error"

                let eventId = 
                    events 
                    |> Seq.findIndex (fun hev -> hev.EventId = scheduleAttr.DecisionTaskCompletedEventId)

                let findResult = FindRequestCancelActivityTaskResult eventId (scheduleAttr.ActivityId)
                
                match findResult with
                | Some(result) -> f(result)
                | None -> 
                    blockFlag <- true
                    let d = new Decision()
                    d.DecisionType <- DecisionType.RequestCancelActivityTask
                    d.RequestCancelActivityTaskDecisionAttributes <- new RequestCancelActivityTaskDecisionAttributes()
                    d.RequestCancelActivityTaskDecisionAttributes.ActivityId <- (scheduleAttr.ActivityId)
                    response.Decisions.Add(d)
                    response

    // Start Child Workflow Execution
    member this.Bind(StartChildWorkflowExecutionAction.Attributes(attr), f:(StartChildWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let bindingId = NextBindingId()

        let combinedHistory = FindChildWorkflowExecutionHistory DecisionTask bindingId (attr.WorkflowType) (attr.WorkflowId)

        match (combinedHistory) with
        // Completed
        | h when h.ChildWorkflowExecutionCompletedEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.Completed(h.ChildWorkflowExecutionCompletedEventAttributes))
                 
        // Canceled
        | h when h.ChildWorkflowExecutionCanceledEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.Canceled(h.ChildWorkflowExecutionCanceledEventAttributes))

        // TimedOut
        | h when h.ChildWorkflowExecutionTimedOutEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.TimedOut(h.ChildWorkflowExecutionTimedOutEventAttributes))

        // Failed
        | h when h.ChildWorkflowExecutionFailedEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.Failed(h.ChildWorkflowExecutionFailedEventAttributes))

        // Terminated
        | h when h.ChildWorkflowExecutionTerminatedEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.Terminated(h.ChildWorkflowExecutionTerminatedEventAttributes))

        // StartChildWorkflowExecutionFailed
        | h when h.StartChildWorkflowExecutionFailedEventAttributes <> null && 
                 h.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name && 
                 h.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version ->
            f(StartChildWorkflowExecutionResult.StartFailed(h.StartChildWorkflowExecutionFailedEventAttributes))

        // Started
        | h when h.ChildWorkflowExecutionStartedEventAttributes <> null ->
            f(StartChildWorkflowExecutionResult.Started(h.ChildWorkflowExecutionStartedEventAttributes, h.StartChildWorkflowExecutionInitiatedEventAttributes))

        // Initiated
        | h when h.StartChildWorkflowExecutionInitiatedEventAttributes <> null &&
                    h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name = attr.WorkflowType.Name && 
                    h.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version = attr.WorkflowType.Version ->
            f(StartChildWorkflowExecutionResult.Initiated(h.StartChildWorkflowExecutionInitiatedEventAttributes))

        // Not Started
        | h when h.ActivityTaskScheduledEventAttributes = null ->
            attr.Control <- bindingId.ToString()

            let d = new Decision();
            d.DecisionType <- DecisionType.StartChildWorkflowExecution
            d.StartChildWorkflowExecutionDecisionAttributes <- attr
            response.Decisions.Add(d)
                
            f(StartChildWorkflowExecutionResult.Starting(d.StartChildWorkflowExecutionDecisionAttributes))

        | _ -> failwith "error"

    // Wait For Child Workflow Execution
    member this.Bind(WaitForChildWorkflowExecutionAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match (result.IsFinished()) with 
        | true -> f()
        | false -> 
            blockFlag <- true
            response 

    // Wait For Any Child Workflow Execution
    member this.Bind(WaitForAnyChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let anyFinished = 
            results
            |> List.exists (fun r -> r.IsFinished())

        match anyFinished with
        | true -> f()
        | false -> 
            blockFlag <- true
            response       

    // Wait For All Child Workflow Execution
    member this.Bind(WaitForAllChildWorkflowExecutionAction.StartResults(results), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        let allFinished = 
            results
            |> List.forall (fun r -> r.IsFinished())

        match allFinished with
        | true -> f()
        | false -> 
            blockFlag <- true
            response 

    // Request Cancel External Workflow Execution
    member this.Bind(RequestCancelExternalWorkflowExecutionAction.Attributes(attr), f:(RequestCancelExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let bindingId = NextBindingId()

        let combinedHistory = FindRequestCancelExternalWorkflowExecutionHistory DecisionTask bindingId attr.WorkflowId

        match (combinedHistory) with
        // Request Delivered
        | h when h.ExternalWorkflowExecutionCancelRequestedEventAttributes <> null ->
            f(RequestCancelExternalWorkflowExecutionResult.Delivered(h.ExternalWorkflowExecutionCancelRequestedEventAttributes))

        // Request Failed
        | h when h.RequestCancelExternalWorkflowExecutionFailedEventAttributes <> null ->
            f(RequestCancelExternalWorkflowExecutionResult.Failed(h.RequestCancelExternalWorkflowExecutionFailedEventAttributes))
 
        // Request Initiated
        | h when h.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <> null ->
            f(RequestCancelExternalWorkflowExecutionResult.Initiated(h.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes))

        // Request not initiated yet
        | _ ->
            attr.Control <- bindingId.ToString()

            let d = new Decision();
            d.DecisionType <- DecisionType.RequestCancelExternalWorkflowExecution
            d.RequestCancelExternalWorkflowExecutionDecisionAttributes <- attr
            response.Decisions.Add(d)
                
            f(RequestCancelExternalWorkflowExecutionResult.Requesting(attr))

    // Start and Wait for Lambda Function
    member this.Bind(ScheduleAndWaitForLambdaFunctionAction.Attributes(attr), f:(ScheduleAndWaitForLambdaFunctionResult -> RespondDecisionTaskCompletedRequest)) = 

        let combinedHistory = FindLambdaFunctionHistory DecisionTask (attr.Id) (attr.Name)

        match (combinedHistory) with
        // ScheduleLambdaFunctionFailed
        | h when h.ScheduleLambdaFunctionFailedEventAttributes <> null && 
                    h.ScheduleLambdaFunctionFailedEventAttributes.Id = attr.Id && 
                    h.ScheduleLambdaFunctionFailedEventAttributes.Name = attr.Name -> 
            f(ScheduleAndWaitForLambdaFunctionResult.ScheduleFailed(h.ScheduleLambdaFunctionFailedEventAttributes))

        // StartLambdaFunctionFailed
        | h when h.StartLambdaFunctionFailedEventAttributes <> null -> 
            f(ScheduleAndWaitForLambdaFunctionResult.StartFailed(h.StartLambdaFunctionFailedEventAttributes))

        // Lambda Function Completed
        | h when h.EventType = EventType.LambdaFunctionCompleted -> 
            f(ScheduleAndWaitForLambdaFunctionResult.Completed(h.LambdaFunctionCompletedEventAttributes))

        // Lambda Function TimedOut
        | EventOfType EventType.LambdaFunctionTimedOut h -> 
            f(ScheduleAndWaitForLambdaFunctionResult.TimedOut(h.LambdaFunctionTimedOutEventAttributes))

        // Lambda Function Failed
        | EventOfType EventType.LambdaFunctionFailed h -> 
            f(ScheduleAndWaitForLambdaFunctionResult.Failed(h.LambdaFunctionFailedEventAttributes))

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

        let combinedHistory = FindTimerHistory DecisionTask (bindingId.ToString())

        match (combinedHistory) with
        // Fired
        | h when h.TimerFiredEventAttributes <> null &&
                 h.TimerFiredEventAttributes.TimerId = attr.TimerId ->
            f(StartTimerResult.Fired(h.TimerFiredEventAttributes))

        // Fired
        | h when h.TimerCanceledEventAttributes <> null &&
                 h.TimerCanceledEventAttributes.TimerId = attr.TimerId ->
            f(StartTimerResult.Canceled(h.TimerCanceledEventAttributes))

        // StartTimerFailed
        | h when h.StartTimerFailedEventAttributes <> null && 
                 h.StartTimerFailedEventAttributes.TimerId = attr.TimerId ->
            f(StartTimerResult.StartTimerFailed(h.StartTimerFailedEventAttributes))

        // TimerStarted
        | h when h.TimerStartedEventAttributes <> null ->
            f(StartTimerResult.Started(h.TimerStartedEventAttributes))

        // Timer Not Started
        | h when h.TimerStartedEventAttributes = null ->
            attr.Control <- bindingId.ToString()

            let d = new Decision();
            d.DecisionType <- DecisionType.StartTimer
            d.StartTimerDecisionAttributes <- attr
            response.Decisions.Add(d)
                
            f(StartTimerResult.Starting(d.StartTimerDecisionAttributes))

        | _ -> failwith "error"

    // Wait For Timer
    member this.Bind(WaitForTimerAction.StartResult(result), f:(unit -> RespondDecisionTaskCompletedRequest)) =
        match result with 
        // Canceled
        | StartTimerResult.Canceled(_) -> f()

        // Fired
        | StartTimerResult.Fired(_) -> f()

        // StartTimerFailed
        | StartTimerResult.StartTimerFailed(_) -> f()

        // Starting
        | StartTimerResult.Starting(_) -> 
            blockFlag <- true
            response

        // Started
        | StartTimerResult.Started(attr) ->
            blockFlag <- true
            response

    // Cancel Timer
    member this.Bind(CancelTimerAction.StartResult(result), f:(CancelTimerResult -> RespondDecisionTaskCompletedRequest)) =

        match result with 
        // Starting
        | StartTimerResult.Starting(_) -> 
            blockFlag <- true
            response

        // Canceled
        | StartTimerResult.Canceled(attr) -> f(CancelTimerResult.Canceled(attr))

        // Fired
        | StartTimerResult.Fired(attr) -> f(CancelTimerResult.Fired(attr))

        // StartTimerFailed
        | StartTimerResult.StartTimerFailed(attr) -> f(CancelTimerResult.StartTimerFailed(attr))

        // Started
        | StartTimerResult.Started(attr) -> 
            let combinedHistory = FindTimerHistory DecisionTask (attr.Control)

            match (combinedHistory) with
            // CancelTimerFailed
            | h when h.CancelTimerFailedEventAttributes <> null && 
                     h.CancelTimerFailedEventAttributes.TimerId = attr.TimerId ->
                f(CancelTimerResult.CancelTimerFailed(h.CancelTimerFailedEventAttributes))

            | _ -> 
                // This timer has not been canceled yet, make cancel decision
                let d = new Decision();
                d.DecisionType <- DecisionType.CancelTimer
                d.CancelTimerDecisionAttributes <- new CancelTimerDecisionAttributes(TimerId=attr.TimerId)
                response.Decisions.Add(d)

                f(CancelTimerResult.Canceling)

    // Marker Recorded
    member this.Bind(MarkerRecordedAction.Attributes(markerName), f:(MarkerRecordedResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = FindMarkerHistory DecisionTask markerName

        match combinedHistory with
        // RecordMarkerFailed
        | h when h.RecordMarkerFailedEventAttributes <> null ->
            f(MarkerRecordedResult.RecordMarkerFailed(h.RecordMarkerFailedEventAttributes))

        // MarkerRecorded
        | h when h.MarkerRecordedEventAttributes <> null ->
            f(MarkerRecordedResult.MarkerRecorded(h.MarkerRecordedEventAttributes))

        // The marker was never recorded, record it now
        | _ ->
            f(MarkerRecordedResult.NotRecorded)

    // Record Marker
    member this.Bind(RecordMarkerAction.Attributes(attr), f:(RecordMarkerResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = FindMarkerHistory DecisionTask attr.MarkerName

        match combinedHistory with

        // RecordMarkerFailed
        | h when h.RecordMarkerFailedEventAttributes <> null ->
            f(RecordMarkerResult.RecordMarkerFailed(h.RecordMarkerFailedEventAttributes))

        // MarkerRecorded
        | h when h.MarkerRecordedEventAttributes <> null ->
            f(RecordMarkerResult.MarkerRecorded(h.MarkerRecordedEventAttributes))

        // The marker was never recorded, record it now
        | _ ->
            let d = new Decision();
            d.DecisionType <- DecisionType.RecordMarker
            d.RecordMarkerDecisionAttributes <- attr
            response.Decisions.Add(d)

            f(RecordMarkerResult.Recording)

    // Signal External Workflow Execution
    member this.Bind(SignalExternalWorkflowExecutionAction.Attributes(attr), f:(SignalExternalWorkflowExecutionResult -> RespondDecisionTaskCompletedRequest)) =
        let bindingId = NextBindingId()

        let combinedHistory = FindSignalExternalWorkflowExecutionHistory DecisionTask bindingId attr.SignalName attr.WorkflowId

        match combinedHistory with
        // Failed
        | h when h.SignalExternalWorkflowExecutionFailedEventAttributes <> null ->
            f(SignalExternalWorkflowExecutionResult.Failed(h.SignalExternalWorkflowExecutionFailedEventAttributes))
        
        // Signaled
        | h when h.ExternalWorkflowExecutionSignaledEventAttributes <> null ->
            f(SignalExternalWorkflowExecutionResult.Signaled(h.ExternalWorkflowExecutionSignaledEventAttributes))

        // Initiated
        | h when h.SignalExternalWorkflowExecutionInitiatedEventAttributes <> null ->
            f(SignalExternalWorkflowExecutionResult.Initiated(h.SignalExternalWorkflowExecutionInitiatedEventAttributes))

        // Signaling
        | _ -> 
            // The signal was never sent, send it now
            attr.Control <- bindingId.ToString()
                
            let d = new Decision();
            d.DecisionType <- DecisionType.SignalExternalWorkflowExecution
            d.SignalExternalWorkflowExecutionDecisionAttributes <- attr
            response.Decisions.Add(d)
            f(SignalExternalWorkflowExecutionResult.Signaling)

    // Workflow Execution Signaled
    member this.Bind(WorkflowExecutionSignaledAction.Attributes(signalName), f:(WorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = FindSignalHistory DecisionTask signalName

        match combinedHistory with
        // Signaled
        | h when h.WorkflowExecutionSignaledEventAttributes <> null ->
            f(WorkflowExecutionSignaledResult.Signaled(h.WorkflowExecutionSignaledEventAttributes))
        
        // Not Signaled
        | _ ->
            f(WorkflowExecutionSignaledResult.NotSignaled)

    // Wait For Workflow Execution Signaled
    member this.Bind(WaitForWorkflowExecutionSignaledAction.Attributes(signalName), f:(WaitForWorkflowExecutionSignaledResult -> RespondDecisionTaskCompletedRequest)) =
        let combinedHistory = FindSignalHistory DecisionTask signalName

        match combinedHistory with
        // Signaled
        | h when h.WorkflowExecutionSignaledEventAttributes <> null ->
            f(WaitForWorkflowExecutionSignaledResult.Signaled(h.WorkflowExecutionSignaledEventAttributes))
        
        // Not Signaled, keep waiting
        | _ ->
            blockFlag <- true
            response

    // Workflow Execution Cancel Requested
    member this.Bind(WorkflowExecutionCancelRequestedAction.Attributes(), f:(WorkflowExecutionCancelRequestedResult -> RespondDecisionTaskCompletedRequest)) =
        let cancelRequestedEvent = 
            DecisionTask.Events |>
            Seq.tryFindBack (fun hev -> hev.EventType = EventType.WorkflowExecutionCancelRequested)

        match cancelRequestedEvent with
        // Workflow Cancel Requsted
        | Some(event) ->
            f(WorkflowExecutionCancelRequestedResult.CancelRequested(event.WorkflowExecutionCancelRequestedEventAttributes))
            
        // NotRequested
        | None ->
            f(WorkflowExecutionCancelRequestedResult.NotRequested)            

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

