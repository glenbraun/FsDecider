namespace FlowSharp.Actions

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type WorkflowExecutionCancelRequestedAction =
    | Attributes of unit

type WorkflowExecutionCancelRequestedResult =
    | NotRequested
    | CancelRequested of WorkflowExecutionCancelRequestedEventAttributes

type GetWorkflowExecutionInputAction =
    | Attributes of unit

type GetExecutionContextAction =
    | Attributes of unit

type SetExecutionContextAction =
    | Attributes of ExecutionContext:string

type RemoveFromContextAction =
    | ScheduleActivityTask              of ScheduleActivityTaskDecisionAttributes
    | ScheduleLambdaFunction            of ScheduleLambdaFunctionDecisionAttributes
    | StartChildWorkflowExecution       of StartChildWorkflowExecutionDecisionAttributes
    | StartTimer                        of StartTimerDecisionAttributes
    | WorkflowExecutionSignaled         of string
    | SignalExternalWorkflowExecution   of SignalExternalWorkflowExecutionDecisionAttributes
    | RecordMarker                      of RecordMarkerDecisionAttributes
    | MarkerRecorded                    of string
    
type WaitAction =
    | Attributes of unit

type ReturnResult = 
    | RespondDecisionTaskCompleted
    | CompleteWorkflowExecution         of Result:string
    | CancelWorkflowExecution           of Details:string
    | FailWorkflowExecution             of Reason:string * Details:string
    | ContinueAsNewWorkflowExecution    of ContinueAsNewWorkflowExecutionDecisionAttributes

exception CompleteWorkflowExecutionFailedException      of CompleteWorkflowExecutionFailedEventAttributes
exception CancelWorkflowExecutionFailedException        of CancelWorkflowExecutionFailedEventAttributes
exception FailWorkflowExecutionFailedException          of FailWorkflowExecutionFailedEventAttributes
exception ContinueAsNewWorkflowExecutionFailedException of ContinueAsNewWorkflowExecutionFailedEventAttributes

type ScheduleActivityTaskResult =
    | Scheduling        of ScheduleActivityTaskDecisionAttributes
    | Scheduled         of ActivityTaskScheduledEventAttributes
    | Started           of Started:         ActivityTaskStartedEventAttributes *
                           Scheduled:       ActivityTaskScheduledEventAttributes
    | Completed         of ActivityTaskCompletedEventAttributes
    | Canceled          of ActivityTaskCanceledEventAttributes
    | Failed            of ActivityTaskFailedEventAttributes
    | TimedOut          of ActivityTaskTimedOutEventAttributes
    | ScheduleFailed    of ScheduleActivityTaskFailedEventAttributes

    member this.IsFinished() =
        match this with
        | Completed(_) -> true
        | Canceled(_) -> true
        | TimedOut(_) -> true
        | Failed(_) -> true
        | ScheduleFailed(_) -> true
        | _ -> false

type ScheduleActivityTaskAction =
    | Attributes        of ScheduleActivityTaskDecisionAttributes * bool
    | ResultFromContext of ScheduleActivityTaskDecisionAttributes * ScheduleActivityTaskResult

    member this.GetAttributes() =
            match this with
            | ScheduleActivityTaskAction.ResultFromContext(attr, _) -> attr
            | ScheduleActivityTaskAction.Attributes(attr, _) -> attr

type WaitForActivityTaskAction =
    | ScheduleResult of ScheduleActivityTaskResult

type WaitForAnyActivityTaskAction =
    | ScheduleResults of ScheduleActivityTaskResult list

type WaitForAllActivityTaskAction =
    | ScheduleResults of ScheduleActivityTaskResult list

type RequestCancelActivityTaskAction =
    | ScheduleResult of ScheduleActivityTaskResult

type RequestCancelActivityTaskResult =
    | Requesting
    | CancelRequested       of ActivityTaskCancelRequestedEventAttributes
    | RequestCancelFailed   of RequestCancelActivityTaskFailedEventAttributes
    | ActivityFinished
    | ActivityScheduleFailed

type ScheduleLambdaFunctionResult =
    | Scheduling        of ScheduleLambdaFunctionDecisionAttributes
    | Scheduled         of LambdaFunctionScheduledEventAttributes
    | Started           of LambdaFunctionStartedEventAttributes *
                           LambdaFunctionScheduledEventAttributes
    | Completed         of LambdaFunctionCompletedEventAttributes
    | Failed            of LambdaFunctionFailedEventAttributes
    | TimedOut          of LambdaFunctionTimedOutEventAttributes
    | StartFailed       of StartLambdaFunctionFailedEventAttributes
    | ScheduleFailed    of ScheduleLambdaFunctionFailedEventAttributes

    member this.IsFinished() =
        match this with
        | Completed(_) -> true
        | Failed(_) -> true
        | TimedOut(_) -> true
        | StartFailed(_) -> true
        | ScheduleFailed(_) -> true
        | _ -> false

type ScheduleLambdaFunctionAction =
    | Attributes        of ScheduleLambdaFunctionDecisionAttributes * bool
    | ResultFromContext of ScheduleLambdaFunctionDecisionAttributes * ScheduleLambdaFunctionResult

    member this.GetAttributes() =
        match this with
        | ScheduleLambdaFunctionAction.ResultFromContext(attr, _) -> attr
        | ScheduleLambdaFunctionAction.Attributes(attr, _) -> attr

type WaitForLambdaFunctionAction =
    | ScheduleResult   of ScheduleLambdaFunctionResult

type WaitForAnyLambdaFunctionAction =
    | ScheduleResults of ScheduleLambdaFunctionResult list

type WaitForAllLambdaFunctionAction =
    | ScheduleResults of ScheduleLambdaFunctionResult list

type StartChildWorkflowExecutionResult =
    | Starting          of StartChildWorkflowExecutionDecisionAttributes
    | Initiated         of StartChildWorkflowExecutionInitiatedEventAttributes
    | Started           of ChildWorkflowExecutionStartedEventAttributes
    | Completed         of ChildWorkflowExecutionCompletedEventAttributes
    | Canceled          of ChildWorkflowExecutionCanceledEventAttributes
    | Failed            of ChildWorkflowExecutionFailedEventAttributes
    | TimedOut          of ChildWorkflowExecutionTimedOutEventAttributes
    | Terminated        of ChildWorkflowExecutionTerminatedEventAttributes
    | StartFailed       of StartChildWorkflowExecutionFailedEventAttributes

    member this.IsFinished() =
        match this with
        | Completed(_) -> true
        | Canceled(_) -> true
        | Failed(_) -> true
        | TimedOut(_) -> true
        | Terminated(_) -> true
        | StartFailed(_) -> true
        | _ -> false

type StartChildWorkflowExecutionAction =
    | Attributes        of StartChildWorkflowExecutionDecisionAttributes * bool
    | ResultFromContext of StartChildWorkflowExecutionDecisionAttributes * StartChildWorkflowExecutionResult

    member this.GetAttributes() =
        match this with
        | StartChildWorkflowExecutionAction.ResultFromContext(attr, _) -> attr
        | StartChildWorkflowExecutionAction.Attributes(attr, _) -> attr

type WaitForChildWorkflowExecutionAction =
    | StartResult of StartChildWorkflowExecutionResult

type WaitForAnyChildWorkflowExecutionAction =
    | StartResults of StartChildWorkflowExecutionResult list

type WaitForAllChildWorkflowExecutionAction =
    | StartResults of StartChildWorkflowExecutionResult list

type RequestCancelExternalWorkflowExecutionAction =
    | Attributes of RequestCancelExternalWorkflowExecutionDecisionAttributes

type RequestCancelExternalWorkflowExecutionResult =
    | Requesting    of RequestCancelExternalWorkflowExecutionDecisionAttributes
    | Initiated     of RequestCancelExternalWorkflowExecutionInitiatedEventAttributes
    | Delivered     of ExternalWorkflowExecutionCancelRequestedEventAttributes
    | Failed        of RequestCancelExternalWorkflowExecutionFailedEventAttributes

type StartTimerResult =
    | Starting          of StartTimerDecisionAttributes
    | Started           of TimerStartedEventAttributes
    | Fired             of Fired:               TimerFiredEventAttributes
    | Canceled          of Canceled:            TimerCanceledEventAttributes
    | StartTimerFailed  of StartTimerFailed:    StartTimerFailedEventAttributes

type StartTimerAction =
    | Attributes        of StartTimerDecisionAttributes * bool
    | ResultFromContext of StartTimerDecisionAttributes * StartTimerResult

    member this.GetAttributes() =
        match this with
        | StartTimerAction.ResultFromContext(attr, _) -> attr
        | StartTimerAction.Attributes(attr, _) -> attr

type WaitForTimerAction =
    | StartResult of StartTimerResult

type CancelTimerAction =
    | StartResult of StartTimerResult

type CancelTimerResult =
    | Canceling
    | Canceled          of TimerCanceledEventAttributes
    | Fired             of TimerFiredEventAttributes
    | StartTimerFailed  of StartTimerFailedEventAttributes
    | CancelTimerFailed of CancelTimerFailedEventAttributes

type WorkflowExecutionSignaledResult =
    | NotSignaled
    | Signaled of WorkflowExecutionSignaledEventAttributes

type WorkflowExecutionSignaledAction =
    | Attributes        of string * bool
    | ResultFromContext of string * WorkflowExecutionSignaledResult

    member this.GetAttributes() =
        match this with
        | WorkflowExecutionSignaledAction.ResultFromContext(signalName, _) -> signalName
        | WorkflowExecutionSignaledAction.Attributes(signalName, _) -> signalName

type WaitForWorkflowExecutionSignaledAction =
    | Attributes of SignalName:string * bool
    | ResultFromContext of string * WorkflowExecutionSignaledResult

    member this.GetAttributes() =
        match this with
        | WaitForWorkflowExecutionSignaledAction.ResultFromContext(signalName, _) -> signalName
        | WaitForWorkflowExecutionSignaledAction.Attributes(signalName, _) -> signalName

type SignalExternalWorkflowExecutionResult = 
    | Signaling
    | Initiated of SignalExternalWorkflowExecutionInitiatedEventAttributes
    | Signaled  of ExternalWorkflowExecutionSignaledEventAttributes
    | Failed    of SignalExternalWorkflowExecutionFailedEventAttributes

type SignalExternalWorkflowExecutionAction = 
    | Attributes        of SignalExternalWorkflowExecutionDecisionAttributes * bool
    | ResultFromContext of SignalExternalWorkflowExecutionDecisionAttributes * SignalExternalWorkflowExecutionResult

    member this.GetAttributes() =
        match this with
        | SignalExternalWorkflowExecutionAction.ResultFromContext(attr, _) -> attr
        | SignalExternalWorkflowExecutionAction.Attributes(attr, _) -> attr

type RecordMarkerResult = 
    | Recording
    | Recorded              of MarkerRecordedEventAttributes
    | RecordMarkerFailed    of RecordMarkerFailedEventAttributes

type RecordMarkerAction = 
    | Attributes        of RecordMarkerDecisionAttributes * bool
    | ResultFromContext of RecordMarkerDecisionAttributes * RecordMarkerResult

    member this.GetAttributes() =
        match this with
        | RecordMarkerAction.ResultFromContext(attr, _) -> attr
        | RecordMarkerAction.Attributes(attr, _) -> attr

type MarkerRecordedResult = 
    | NotRecorded
    | Recorded              of MarkerRecordedEventAttributes
    | RecordMarkerFailed    of RecordMarkerFailedEventAttributes
    
type MarkerRecordedAction = 
    | Attributes        of string * bool
    | ResultFromContext of string * MarkerRecordedResult

    member this.GetAttributes() =
        match this with
        | MarkerRecordedAction.ResultFromContext(markerName, _) -> markerName
        | MarkerRecordedAction.Attributes(markerName, _) -> markerName
        


        
