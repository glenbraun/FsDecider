namespace FlowSharp.UnitTests

open System
open System.IO
open System.Reflection
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

module OfflineHistory =

    let private AssignAttributes (event:HistoryEvent) (args:System.Object) = 
        match args with
        // WorkflowExecutionStarted: The workflow execution was started.
        | :? WorkflowExecutionStartedEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionStarted
            event.WorkflowExecutionStartedEventAttributes <- args :?> WorkflowExecutionStartedEventAttributes

        // WorkflowExecutionCompleted: The workflow execution was closed due to successful completion.
        | :? WorkflowExecutionCompletedEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionCompleted
            event.WorkflowExecutionCompletedEventAttributes <- args :?> WorkflowExecutionCompletedEventAttributes

        // WorkflowExecutionFailed: The workflow execution closed due to a failure.
        | :? WorkflowExecutionFailedEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionFailed
            event.WorkflowExecutionFailedEventAttributes <- args :?> WorkflowExecutionFailedEventAttributes

        // WorkflowExecutionTimedOut: The workflow execution was closed because a time out was exceeded.
        | :? WorkflowExecutionTimedOutEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionTimedOut
            event.WorkflowExecutionTimedOutEventAttributes <- args :?> WorkflowExecutionTimedOutEventAttributes

        // WorkflowExecutionCanceled: The workflow execution was successfully canceled and closed.
        | :? WorkflowExecutionCanceledEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionCanceled
            event.WorkflowExecutionCanceledEventAttributes <- args :?> WorkflowExecutionCanceledEventAttributes

        // WorkflowExecutionTerminated: The workflow execution was terminated.
        | :? WorkflowExecutionTerminatedEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionTerminated
            event.WorkflowExecutionTerminatedEventAttributes <- args :?> WorkflowExecutionTerminatedEventAttributes

        // WorkflowExecutionContinuedAsNew: The workflow execution was closed and a new execution of the same type was created with the same workflowId.
        | :? WorkflowExecutionContinuedAsNewEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionContinuedAsNew
            event.WorkflowExecutionContinuedAsNewEventAttributes <- args :?> WorkflowExecutionContinuedAsNewEventAttributes

        // WorkflowExecutionCancelRequested: A request to cancel this workflow execution was made.
        | :? WorkflowExecutionCancelRequestedEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionCancelRequested
            event.WorkflowExecutionCancelRequestedEventAttributes <- args :?> WorkflowExecutionCancelRequestedEventAttributes

        // DecisionTaskScheduled: A decision task was scheduled for the workflow execution.
        | :? DecisionTaskScheduledEventAttributes as attr ->
            event.EventType <- EventType.DecisionTaskScheduled
            event.DecisionTaskScheduledEventAttributes <- args :?> DecisionTaskScheduledEventAttributes

        // DecisionTaskStarted: The decision task was dispatched to a decider.
        | :? DecisionTaskStartedEventAttributes as attr ->
            event.EventType <- EventType.DecisionTaskStarted
            event.DecisionTaskStartedEventAttributes <- args :?> DecisionTaskStartedEventAttributes

        // DecisionTaskCompleted: The decider successfully completed a decision task by calling RespondDecisionTaskCompleted.
        | :? DecisionTaskCompletedEventAttributes as attr ->
            event.EventType <- EventType.DecisionTaskCompleted
            event.DecisionTaskCompletedEventAttributes <- args :?> DecisionTaskCompletedEventAttributes

        // DecisionTaskTimedOut: The decision task timed out.
        | :? DecisionTaskTimedOutEventAttributes as attr ->
            event.EventType <- EventType.DecisionTaskTimedOut
            event.DecisionTaskTimedOutEventAttributes <- args :?> DecisionTaskTimedOutEventAttributes

        // ActivityTaskScheduled: An activity task was scheduled for execution.
        | :? ActivityTaskScheduledEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskScheduled
            event.ActivityTaskScheduledEventAttributes <- args :?> ActivityTaskScheduledEventAttributes

        // ScheduleActivityTaskFailed: Failed to process ScheduleActivityTask decision. This happens when the decision is not configured properly, for example the activity type specified is not registered.
        | :? ScheduleActivityTaskFailedEventAttributes as attr ->
            event.EventType <- EventType.ScheduleActivityTaskFailed
            event.ScheduleActivityTaskFailedEventAttributes <- args :?> ScheduleActivityTaskFailedEventAttributes

        // ActivityTaskStarted: The scheduled activity task was dispatched to a worker.
        | :? ActivityTaskStartedEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskStarted
            event.ActivityTaskStartedEventAttributes <- args :?> ActivityTaskStartedEventAttributes

        // ActivityTaskCompleted: An activity worker successfully completed an activity task by calling RespondActivityTaskCompleted.
        | :? ActivityTaskCompletedEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskCompleted
            event.ActivityTaskCompletedEventAttributes <- args :?> ActivityTaskCompletedEventAttributes

        // ActivityTaskFailed: An activity worker failed an activity task by calling RespondActivityTaskFailed.
        | :? ActivityTaskFailedEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskFailed
            event.ActivityTaskFailedEventAttributes <- args :?> ActivityTaskFailedEventAttributes

        // ActivityTaskTimedOut: The activity task timed out.
        | :? ActivityTaskTimedOutEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskTimedOut
            event.ActivityTaskTimedOutEventAttributes <- args :?> ActivityTaskTimedOutEventAttributes

        // ActivityTaskCanceled: The activity task was successfully canceled.
        | :? ActivityTaskCanceledEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskCanceled
            event.ActivityTaskCanceledEventAttributes <- args :?> ActivityTaskCanceledEventAttributes

        // ActivityTaskCancelRequested: A RequestCancelActivityTask decision was received by the system.
        | :? ActivityTaskCancelRequestedEventAttributes as attr ->
            event.EventType <- EventType.ActivityTaskCancelRequested
            event.ActivityTaskCancelRequestedEventAttributes <- args :?> ActivityTaskCancelRequestedEventAttributes

        // RequestCancelActivityTaskFailed: Failed to process RequestCancelActivityTask decision. This happens when the decision is not configured properly.
        | :? RequestCancelActivityTaskFailedEventAttributes as attr ->
            event.EventType <- EventType.RequestCancelActivityTaskFailed
            event.RequestCancelActivityTaskFailedEventAttributes <- args :?> RequestCancelActivityTaskFailedEventAttributes

        // WorkflowExecutionSignaled: An external signal was received for the workflow execution.
        | :? WorkflowExecutionSignaledEventAttributes as attr ->
            event.EventType <- EventType.WorkflowExecutionSignaled
            event.WorkflowExecutionSignaledEventAttributes <- args :?> WorkflowExecutionSignaledEventAttributes

        // MarkerRecorded: A marker was recorded in the workflow history as the result of a RecordMarker decision.
        | :? MarkerRecordedEventAttributes as attr ->
            event.EventType <- EventType.MarkerRecorded
            event.MarkerRecordedEventAttributes <- args :?> MarkerRecordedEventAttributes

        // RecordMarkerFailedEventAttributes
        | :? RecordMarkerFailedEventAttributes as attr ->
            event.EventType <- EventType.RecordMarkerFailed
            event.RecordMarkerFailedEventAttributes <- args :?> RecordMarkerFailedEventAttributes

        // TimerStarted: A timer was started for the workflow execution due to a StartTimer decision.
        | :? TimerStartedEventAttributes as attr ->
            event.EventType <- EventType.TimerStarted
            event.TimerStartedEventAttributes <- args :?> TimerStartedEventAttributes

        // StartTimerFailed: Failed to process StartTimer decision. This happens when the decision is not configured properly, for example a timer already exists with the specified timer ID.
        | :? StartTimerFailedEventAttributes as attr ->
            event.EventType <- EventType.StartTimerFailed
            event.StartTimerFailedEventAttributes <- args :?> StartTimerFailedEventAttributes

        // TimerFired: A timer, previously started for this workflow execution, fired.
        | :? TimerFiredEventAttributes as attr ->
            event.EventType <- EventType.TimerFired
            event.TimerFiredEventAttributes <- args :?> TimerFiredEventAttributes

        // TimerCanceled: A timer, previously started for this workflow execution, was successfully canceled.
        | :? TimerCanceledEventAttributes as attr ->
            event.EventType <- EventType.TimerCanceled
            event.TimerCanceledEventAttributes <- args :?> TimerCanceledEventAttributes

        // CancelTimerFailed: Failed to process CancelTimer decision. This happens when the decision is not configured properly, for example no timer exists with the specified timer ID.
        | :? CancelTimerFailedEventAttributes as attr ->
            event.EventType <- EventType.CancelTimerFailed
            event.CancelTimerFailedEventAttributes <- args :?> CancelTimerFailedEventAttributes

        // StartChildWorkflowExecutionInitiated: A request was made to start a child workflow execution.
        | :? StartChildWorkflowExecutionInitiatedEventAttributes as attr ->
            event.EventType <- EventType.StartChildWorkflowExecutionInitiated
            event.StartChildWorkflowExecutionInitiatedEventAttributes <- args :?> StartChildWorkflowExecutionInitiatedEventAttributes

        // StartChildWorkflowExecutionFailed: Failed to process StartChildWorkflowExecution decision. This happens when the decision is not configured properly, for example the workflow type specified is not registered.
        | :? StartChildWorkflowExecutionFailedEventAttributes as attr ->
            event.EventType <- EventType.StartChildWorkflowExecutionFailed
            event.StartChildWorkflowExecutionFailedEventAttributes <- args :?> StartChildWorkflowExecutionFailedEventAttributes

        // ChildWorkflowExecutionStarted: A child workflow execution was successfully started.
        | :? ChildWorkflowExecutionStartedEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionStarted
            event.ChildWorkflowExecutionStartedEventAttributes <- args :?> ChildWorkflowExecutionStartedEventAttributes

        // ChildWorkflowExecutionCompleted: A child workflow execution, started by this workflow execution, completed successfully and was closed.
        | :? ChildWorkflowExecutionCompletedEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionCompleted
            event.ChildWorkflowExecutionCompletedEventAttributes <- args :?> ChildWorkflowExecutionCompletedEventAttributes

        // ChildWorkflowExecutionFailed: A child workflow execution, started by this workflow execution, failed to complete successfully and was closed.
        | :? ChildWorkflowExecutionFailedEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionFailed
            event.ChildWorkflowExecutionFailedEventAttributes <- args :?> ChildWorkflowExecutionFailedEventAttributes

        // ChildWorkflowExecutionTimedOut: A child workflow execution, started by this workflow execution, timed out and was closed.
        | :? ChildWorkflowExecutionTimedOutEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionTimedOut
            event.ChildWorkflowExecutionTimedOutEventAttributes <- args :?> ChildWorkflowExecutionTimedOutEventAttributes

        // ChildWorkflowExecutionCanceled: A child workflow execution, started by this workflow execution, was canceled and closed.
        | :? ChildWorkflowExecutionCanceledEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionCanceled
            event.ChildWorkflowExecutionCanceledEventAttributes <- args :?> ChildWorkflowExecutionCanceledEventAttributes

        // ChildWorkflowExecutionTerminated: A child workflow execution, started by this workflow execution, was terminated.
        | :? ChildWorkflowExecutionTerminatedEventAttributes as attr ->
            event.EventType <- EventType.ChildWorkflowExecutionTerminated
            event.ChildWorkflowExecutionTerminatedEventAttributes <- args :?> ChildWorkflowExecutionTerminatedEventAttributes

        // SignalExternalWorkflowExecutionInitiated: A request to signal an external workflow was made.
        | :? SignalExternalWorkflowExecutionInitiatedEventAttributes as attr ->
            event.EventType <- EventType.SignalExternalWorkflowExecutionInitiated
            event.SignalExternalWorkflowExecutionInitiatedEventAttributes <- args :?> SignalExternalWorkflowExecutionInitiatedEventAttributes

        // ExternalWorkflowExecutionSignaled: A signal, requested by this workflow execution, was successfully delivered to the target external workflow execution.
        | :? ExternalWorkflowExecutionSignaledEventAttributes as attr ->
            event.EventType <- EventType.ExternalWorkflowExecutionSignaled
            event.ExternalWorkflowExecutionSignaledEventAttributes <- args :?> ExternalWorkflowExecutionSignaledEventAttributes

        // SignalExternalWorkflowExecutionInitiated: A request to signal an external workflow was made.
        | :? SignalExternalWorkflowExecutionFailedEventAttributes as attr ->
            event.EventType <- EventType.SignalExternalWorkflowExecutionFailed
            event.SignalExternalWorkflowExecutionFailedEventAttributes <- args :?> SignalExternalWorkflowExecutionFailedEventAttributes

        // RequestCancelExternalWorkflowExecutionInitiated: A request was made to request the cancellation of an external workflow execution.
        | :? RequestCancelExternalWorkflowExecutionInitiatedEventAttributes as attr ->
            event.EventType <- EventType.RequestCancelExternalWorkflowExecutionInitiated
            event.RequestCancelExternalWorkflowExecutionInitiatedEventAttributes <- args :?> RequestCancelExternalWorkflowExecutionInitiatedEventAttributes

        // ExternalWorkflowExecutionCancelRequested: Request to cancel an external workflow execution was successfully delivered to the target execution.
        | :? ExternalWorkflowExecutionCancelRequestedEventAttributes as attr ->
            event.EventType <- EventType.ExternalWorkflowExecutionCancelRequested
            event.ExternalWorkflowExecutionCancelRequestedEventAttributes <- args :?> ExternalWorkflowExecutionCancelRequestedEventAttributes

        // RequestCancelExternalWorkflowExecutionFailed: Request to cancel an external workflow execution failed.
        | :? RequestCancelExternalWorkflowExecutionFailedEventAttributes as attr ->
            event.EventType <- EventType.RequestCancelExternalWorkflowExecutionFailed
            event.RequestCancelExternalWorkflowExecutionFailedEventAttributes <- args :?> RequestCancelExternalWorkflowExecutionFailedEventAttributes

        // LambdaFunctionScheduled: An AWS Lambda function was scheduled for execution.
        | :? LambdaFunctionScheduledEventAttributes as attr ->
            event.EventType <- EventType.LambdaFunctionScheduled
            event.LambdaFunctionScheduledEventAttributes <- args :?> LambdaFunctionScheduledEventAttributes

        // LambdaFunctionStarted: The scheduled function was invoked in the AWS Lambda service.
        | :? LambdaFunctionStartedEventAttributes as attr ->
            event.EventType <- EventType.LambdaFunctionStarted
            event.LambdaFunctionStartedEventAttributes <- args :?> LambdaFunctionStartedEventAttributes

        // LambdaFunctionCompleted: The AWS Lambda function successfully completed.
        | :? LambdaFunctionCompletedEventAttributes as attr ->
            event.EventType <- EventType.LambdaFunctionCompleted
            event.LambdaFunctionCompletedEventAttributes <- args :?> LambdaFunctionCompletedEventAttributes

        // LambdaFunctionFailed: The AWS Lambda function execution failed.
        | :? LambdaFunctionFailedEventAttributes as attr ->
            event.EventType <- EventType.LambdaFunctionFailed
            event.LambdaFunctionFailedEventAttributes <- args :?> LambdaFunctionFailedEventAttributes

        // LambdaFunctionTimedOut: The AWS Lambda function execution timed out.
        | :? LambdaFunctionTimedOutEventAttributes as attr ->
            event.EventType <- EventType.LambdaFunctionTimedOut
            event.LambdaFunctionTimedOutEventAttributes <- args :?> LambdaFunctionTimedOutEventAttributes

        // ScheduleLambdaFunctionFailed: Failed to process ScheduleLambdaFunction decision. This happens when the workflow execution does not have the proper IAM role attached to invoke AWS Lambda functions.
        | :? ScheduleLambdaFunctionFailedEventAttributes as attr ->
            event.EventType <- EventType.ScheduleLambdaFunctionFailed
            event.ScheduleLambdaFunctionFailedEventAttributes <- args :?> ScheduleLambdaFunctionFailedEventAttributes

        // StartLambdaFunctionFailed: Failed to invoke the scheduled function in the AWS Lambda service. This happens when the AWS Lambda service is not available in the current region, or received too many requests.
        | :? StartLambdaFunctionFailedEventAttributes as attr ->
            event.EventType <- EventType.StartLambdaFunctionFailed
            event.StartLambdaFunctionFailedEventAttributes <- args :?> StartLambdaFunctionFailedEventAttributes
        
        | _ -> failwith "bad"

    let OfflineDecisionTask (workflowType:WorkflowType) (workflowExecution:WorkflowExecution) : (unit -> DecisionTask) =
        let dt = new DecisionTask(WorkflowType=workflowType, WorkflowExecution=workflowExecution);
        dt.Events <- new ResizeArray<HistoryEvent>();
        
        (fun () -> dt) 

    let OfflineHistoryEventAt (args: 'a when 'a :> System.Object) (timestamp:DateTime) (f:(unit -> DecisionTask)) : (unit -> DecisionTask) =
        let dt = f()

        let hev = new HistoryEvent()
        hev.EventId <- int64 (dt.Events.Count + 1)
        hev.EventTimestamp <- timestamp

        AssignAttributes hev args

        dt.Events.Add(hev)
        (fun () -> dt) 

    let OfflineHistoryEvent (args: 'a when 'a :> System.Object) (f:(unit -> DecisionTask)) : (unit -> DecisionTask) = 
        OfflineHistoryEventAt args (DateTime.Now) f

    let OfflineDecisionTaskSequence (dt:DecisionTask) =
        
        let EventSegments = 
            let events = new ResizeArray<HistoryEvent>();
            let PreviousStartedEventId = ref 0L
            let StartedEventId = ref 0L

            seq {
                for i = 1 to dt.Events.Count do
                    let event = dt.Events.[i-1]
                    events.Add event

                    if event.EventType = EventType.DecisionTaskStarted then
                        StartedEventId := event.EventId
                        yield (!PreviousStartedEventId, !StartedEventId, (new ResizeArray<HistoryEvent>(events)))
                        PreviousStartedEventId := !StartedEventId
                    
            }

        seq {
            for (prev, start, events) in EventSegments do
                let newdt = new DecisionTask()
                newdt.WorkflowExecution <- dt.WorkflowExecution
                newdt.WorkflowType <- dt.WorkflowType
                newdt.TaskToken <- "OfflineHistory TaskToken"
                newdt.Events <- events
                newdt.PreviousStartedEventId <- prev
                newdt.StartedEventId <- start
                yield newdt
        }

    let GenerateOfflineDecisionTaskCodeSnippet (tw:System.IO.TextWriter) (events:ResizeArray<HistoryEvent>) (substitutions:Map<string, string> option) =
        let subs =
            match substitutions with
            | Some(m) -> m
            | None -> Map.empty<string, string>

        let PropertyValueIsEmpty ((name:string), (value:System.Object)) =
            match value with
            | :? string as s -> s = null
            | :? int64 as eventId when name.EndsWith("EventId") -> eventId = 0L
            | :? ResizeArray<string> as taglist when name = "TagList" -> taglist.Count = 0
            | _ -> value = null

        let GenAttributePropertyValueAssignment (tw:TextWriter) (event:HistoryEvent, name:string, value:System.Object) =
            // name=value (value with correct F# syntax)

            let attrname = event.EventType.ToString() + "EventAttributes." + name

            match value with
            | _ when subs.ContainsKey(attrname) ->                             fprintf tw "%s=%s" name (subs.[attrname])
            | _ when subs.ContainsKey(name) ->                                  fprintf tw "%s=%s" name (subs.[name])
            | :? string as s ->                                                 fprintf tw "%s=\"%s\"" name (s.Replace("\"", "\\\""))
            | :? int64 as eventId ->                                            fprintf tw "%s=%dL" name eventId
            | :? ActivityType as at ->                                          fprintf tw "%s=ActivityType(Name=\"%s\", Version=\"%s\")" name (at.Name) (at.Version)
            | :? WorkflowType as wt ->                                          fprintf tw "%s=WorkflowType(Name=\"%s\", Version=\"%s\")" name (wt.Name) (wt.Version)
            | :? WorkflowExecution as we ->                                     fprintf tw "%s=WorkflowExecution(RunId=\"%s\", WorkflowId=\"%s\")" name (we.RunId) (we.WorkflowId)
            | :? ChildPolicy as cp ->                                           fprintf tw "%s=ChildPolicy.%O" name cp
            | :? TaskList as tl ->                                              fprintf tw "%s=TaskList(Name=\"%s\")" name (tl.Name)
            | :? ActivityTaskTimeoutType as timeout ->                          fprintf tw "%s=ActivityTaskTimeoutType.%O" name timeout
            | :? WorkflowExecutionTimeoutType as timeout ->                     fprintf tw "%s=WorkflowExecutionTimeoutType.%O" name timeout
            | :? DecisionTaskTimeoutType as timeout ->                          fprintf tw "%s=DecisionTaskTimeoutType.%O" name timeout
            | :? LambdaFunctionTimeoutType as timeout ->                        fprintf tw "%s=LambdaFunctionTimeoutType.%O" name timeout
            | :? WorkflowExecutionTimeoutType as timeout ->                     fprintf tw "%s=WorkflowExecutionTimeoutType.%O" name timeout
            | :? CancelTimerFailedCause as cause ->                             fprintf tw "%s=CancelTimerFailedCause.%O" name cause
            | :? CancelWorkflowExecutionFailedCause as cause ->                 fprintf tw "%s=CancelWorkflowExecutionFailedCause.%O" name cause
            | :? CompleteWorkflowExecutionFailedCause as cause ->               fprintf tw "%s=CompleteWorkflowExecutionFailedCause.%O" name cause
            | :? ContinueAsNewWorkflowExecutionFailedCause as cause ->          fprintf tw "%s=ContinueAsNewWorkflowExecutionFailedCause.%O" name cause
            | :? FailWorkflowExecutionFailedCause as cause ->                   fprintf tw "%s=FailWorkflowExecutionFailedCause.%O" name cause
            | :? RecordMarkerFailedCause as cause ->                            fprintf tw "%s=RecordMarkerFailedCause.%O" name cause
            | :? RequestCancelActivityTaskFailedCause as cause ->               fprintf tw "%s=RequestCancelActivityTaskFailedCause.%O" name cause
            | :? RequestCancelExternalWorkflowExecutionFailedCause as cause ->  fprintf tw "%s=RequestCancelExternalWorkflowExecutionFailedCause.%O" name cause
            | :? ScheduleActivityTaskFailedCause as cause ->                    fprintf tw "%s=ScheduleActivityTaskFailedCause.%O" name cause
            | :? ScheduleLambdaFunctionFailedCause as cause ->                  fprintf tw "%s=ScheduleLambdaFunctionFailedCause.%O" name cause
            | :? StartLambdaFunctionFailedCause as cause ->                     fprintf tw "%s=StartLambdaFunctionFailedCause.%O" name cause
            | :? SignalExternalWorkflowExecutionFailedCause as cause ->         fprintf tw "%s=SignalExternalWorkflowExecutionFailedCause.%O" name cause
            | :? StartChildWorkflowExecutionFailedCause as cause ->             fprintf tw "%s=StartChildWorkflowExecutionFailedCause.%O" name cause
            | :? StartTimerFailedCause as cause ->                              fprintf tw "%s=StartTimerFailedCause.%O" name cause
            | :? WorkflowExecutionCancelRequestedCause as cause ->              fprintf tw "%s=WorkflowExecutionCancelRequestedCause.%O" name cause
            | :? WorkflowExecutionTerminatedCause as cause ->                   fprintf tw "%s=WorkflowExecutionTerminatedCause.%O" name cause
            | :? DateTime as d ->                                               fprintf tw "%s=DateTime(%d, %d, %d, %d, %d, %d, %d)" name (d.Year) (d.Month) (d.Day) (d.Hour) (d.Minute) (d.Second) (d.Millisecond)
            | :? ResizeArray<string> as taglist when name = "TagList" ->        () // TODO TagList
            | _ -> failwith "Unhandled property type."
            
        let GenOfflineHistoryEvent (tw:System.IO.TextWriter) (event:HistoryEvent) =
            // OfflineHistoryEvent function call
            fprintf tw """
                          |> OfflineHistoryEvent (        // EventId = %d""" (event.EventId)
            
            // Attribute Constructor
            fprintf tw """
                              %sEventAttributes(""" (event.EventType.ToString())

            let rec GenAttributeProperties epv =
                match epv with
                | (n, v) :: t -> 
                        fprintf tw "%a" GenAttributePropertyValueAssignment (event, n, v)

                        if t <> [] then
                            tw.Write(", ")

                        GenAttributeProperties t
                | [] -> 
                    tw.Write(")")

            // Get the EventAttributes object for this EventType
            let EventAttributesPropertyInfo = 
                typeof<HistoryEvent>.GetProperty((event.EventType.ToString()) + "EventAttributes")
                
            let EventAttributesValue = 
                EventAttributesPropertyInfo.GetValue(event)

            let EventAttributesProperties =
                EventAttributesPropertyInfo.PropertyType.GetProperties()
                |> Array.toList

            // Generate the attribute properties
            let ThisEventAttributesNonEmptyProperties = 
                EventAttributesProperties |>
                List.map (fun p -> (p.Name, p.GetValue(EventAttributesValue))) |>
                List.filter (fun nvp -> not (PropertyValueIsEmpty nvp))
            
            GenAttributeProperties ThisEventAttributesNonEmptyProperties
            tw.Write(")")

        // Write the OfflineDecisionTask
        fprintf tw """
        // OfflineDecisionTask
        let offlineFunc = OfflineDecisionTask (TestConfiguration.TestWorkflowType) (WorkflowExecution(RunId="Offline RunId", WorkflowId = workflowId))""" 

        // Write History Events
        events |>
        Seq.iter (fun event -> GenOfflineHistoryEvent tw event)


