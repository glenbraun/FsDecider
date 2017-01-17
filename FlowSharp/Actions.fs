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

type ScheduleAndWaitForActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type ScheduleActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type ScheduleActivityTaskResult =
    | Scheduling        of ScheduleActivityTaskDecisionAttributes
    | Scheduled         of Scheduled:       ActivityTaskScheduledEventAttributes
    | Started           of Started:         ActivityTaskStartedEventAttributes *
                           Scheduled:       ActivityTaskScheduledEventAttributes
    | Completed         of Completed:       ActivityTaskCompletedEventAttributes
    | Canceled          of Canceled:        ActivityTaskCanceledEventAttributes
    | Failed            of Failed:          ActivityTaskFailedEventAttributes
    | TimedOut          of TimedOut:        ActivityTaskTimedOutEventAttributes
    | ScheduleFailed    of ScheduleFailed:  ScheduleActivityTaskFailedEventAttributes

    member this.IsFinished() =
        match this with
        | Completed(_) -> true
        | Canceled(_) -> true
        | TimedOut(_) -> true
        | Failed(_) -> true
        | ScheduleFailed(_) -> true
        | _ -> false

type WaitForActivityTaskAction =
    | ScheduleResult of ScheduleActivityTaskResult

type WaitForAnyActivityTaskAction =
    | ScheduleResults of ScheduleActivityTaskResult list

type WaitForAllActivityTaskAction =
    | ScheduleResults of ScheduleActivityTaskResult list

type RequestCancelActivityTaskAction =
    | ScheduleResult of ScheduleActivityTaskResult

type RequestCancelActivityTaskResult =
    | CancelRequested       of ActivityTaskCancelRequestedEventAttributes
    | RequestCancelFailed   of RequestCancelActivityTaskFailedEventAttributes
    | ActivityFinished
    | ActivityScheduleFailed

type ScheduleAndWaitForLambdaFunctionAction =
    | Attributes of ScheduleLambdaFunctionDecisionAttributes

type ScheduleAndWaitForLambdaFunctionResult =
    | Completed         of LambdaFunctionCompletedEventAttributes
    | Failed            of LambdaFunctionFailedEventAttributes
    | TimedOut          of LambdaFunctionTimedOutEventAttributes
    | StartFailed       of StartLambdaFunctionFailedEventAttributes
    | ScheduleFailed    of ScheduleLambdaFunctionFailedEventAttributes

type StartChildWorkflowExecutionAction =
    | Attributes of StartChildWorkflowExecutionDecisionAttributes

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

type StartTimerAction =
    | Attributes of StartTimerDecisionAttributes

type StartTimerResult =
    | Starting          of StartTimerDecisionAttributes
    | Started           of TimerStartedEventAttributes
    | Fired             of Fired:               TimerFiredEventAttributes
    | Canceled          of Canceled:            TimerCanceledEventAttributes
    | StartTimerFailed  of StartTimerFailed:    StartTimerFailedEventAttributes

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

type WorkflowExecutionSignaledAction =
    | Attributes of SignalName:string

type WorkflowExecutionSignaledResult =
    | NotSignaled
    | Signaled of WorkflowExecutionSignaledEventAttributes

type WaitForWorkflowExecutionSignaledAction =
    | Attributes of SignalName:string

type WaitForWorkflowExecutionSignaledResult =
    | Signaled of WorkflowExecutionSignaledEventAttributes

type SignalExternalWorkflowExecutionAction = 
    | Attributes of SignalExternalWorkflowExecutionDecisionAttributes

type SignalExternalWorkflowExecutionResult = 
    | Signaling
    | Initiated of SignalExternalWorkflowExecutionInitiatedEventAttributes
    | Signaled  of ExternalWorkflowExecutionSignaledEventAttributes
    | Failed    of SignalExternalWorkflowExecutionFailedEventAttributes

type RecordMarkerAction = 
    | Attributes of RecordMarkerDecisionAttributes

type RecordMarkerResult = 
    | Recording
    | RecordMarkerFailed    of RecordMarkerFailedEventAttributes
    | MarkerRecorded        of MarkerRecordedEventAttributes

type MarkerRecordedAction = 
    | Attributes of MarkerName:string

type MarkerRecordedResult = 
    | NotRecorded
    | RecordMarkerFailed    of RecordMarkerFailedEventAttributes
    | MarkerRecorded        of MarkerRecordedEventAttributes


