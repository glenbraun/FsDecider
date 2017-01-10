namespace FlowSharp.Actions

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type WorkflowExecutionSignaledAction =
    | Attributes of SignalName:string

type WorkflowExecutionSignaledResult =
    | NotSignaled
    | Signaled of WorkflowExecutionSignaledEventAttributes

type WaitForWorkflowExecutionSignaledAction =
    | Attributes of SignalName:string

type WaitForWorkflowExecutionSignaledResult =
    | Signaled of WorkflowExecutionSignaledEventAttributes

type RecordMarkerAction = 
    | Attributes of RecordMarkerDecisionAttributes

type RecordMarkerResult = 
    | Recording
    | RecordMarkerFailed of RecordMarkerFailedEventAttributes
    | MarkerRecorded of MarkerRecordedEventAttributes

type SignalExternalWorkflowExecutionAction = 
    | Attributes of SignalExternalWorkflowExecutionDecisionAttributes

type SignalExternalWorkflowExecutionResult = 
    | Signaling
    | Initiated of SignalExternalWorkflowExecutionInitiatedEventAttributes
    | Signaled of ExternalWorkflowExecutionSignaledEventAttributes
    | Failed of SignalExternalWorkflowExecutionFailedEventAttributes

type RequestCancelExternalWorkflowExecutionAction =
    | Attributes of RequestCancelExternalWorkflowExecutionDecisionAttributes

type RequestCancelExternalWorkflowExecutionResult =
    | Requesting
    | Initiated of RequestCancelExternalWorkflowExecutionInitiatedEventAttributes
    | Delivered of ExternalWorkflowExecutionCancelRequestedEventAttributes
    | Failed of RequestCancelExternalWorkflowExecutionFailedEventAttributes

type StartActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type StartActivityTaskResult =
    | ScheduleFailed of ScheduleActivityTaskFailedEventAttributes
    | Scheduling of Activity:ActivityType * ActivityId:string
    | Scheduled of ActivityTaskScheduledEventAttributes
    | Started of Attributes:ActivityTaskStartedEventAttributes * ActivityType:ActivityType * Control:string * ActivityId:string

type StartAndWaitForActivityTaskAction =
    | Attributes of ScheduleActivityTaskDecisionAttributes

type WaitForActivityTaskAction =
    | StartResult of StartActivityTaskResult

type WaitForActivityTaskResult =
    | ScheduleFailed of ScheduleActivityTaskFailedEventAttributes
    | Completed of ActivityTaskCompletedEventAttributes
    | Canceled of ActivityTaskCanceledEventAttributes
    | TimedOut of ActivityTaskTimedOutEventAttributes
    | Failed of ActivityTaskFailedEventAttributes

type RequestCancelActivityTaskAction =
    | StartResult of StartActivityTaskResult

type RequestCancelActivityTaskResult =
    | ScheduleFailed of ScheduleActivityTaskFailedEventAttributes
    | RequestCancelFailed of RequestCancelActivityTaskFailedEventAttributes
    | CancelRequested
    | Completed of ActivityTaskCompletedEventAttributes
    | Canceled of ActivityTaskCanceledEventAttributes
    | TimedOut of ActivityTaskTimedOutEventAttributes
    | Failed of ActivityTaskFailedEventAttributes

type StartChildWorkflowExecutionAction =
    | Attributes of StartChildWorkflowExecutionDecisionAttributes

type StartChildWorkflowExecutionResult =
    | Scheduling 
    | StartFailed of StartChildWorkflowExecutionFailedEventAttributes
    | Initiated of StartChildWorkflowExecutionInitiatedEventAttributes
    | Started of Attributes:ChildWorkflowExecutionStartedEventAttributes * Control:string

type WaitForChildWorkflowExecutionAction =
    | StartResult of StartChildWorkflowExecutionResult

type WaitForChildWorkflowExecutionResult =
    | StartFailed of StartChildWorkflowExecutionFailedEventAttributes
    | Completed of ChildWorkflowExecutionCompletedEventAttributes
    | Canceled of ChildWorkflowExecutionCanceledEventAttributes
    | TimedOut of ChildWorkflowExecutionTimedOutEventAttributes
    | Failed of ChildWorkflowExecutionFailedEventAttributes
    | Terminated of ChildWorkflowExecutionTerminatedEventAttributes

type StartAndWaitForLambdaFunctionAction =
    | Attributes of ScheduleLambdaFunctionDecisionAttributes

type StartAndWaitForLambdaFunctionResult =
    | ScheduleFailed of ScheduleLambdaFunctionFailedEventAttributes
    | StartFailed of StartLambdaFunctionFailedEventAttributes
    | Completed of LambdaFunctionCompletedEventAttributes
    | Failed of LambdaFunctionFailedEventAttributes
    | TimedOut of LambdaFunctionTimedOutEventAttributes

type StartTimerAction =
    | Attributes of StartTimerDecisionAttributes

type StartTimerResult =
    | StartTimerFailed of StartTimerFailedEventAttributes
    | Starting
    | Started of TimerStartedEventAttributes

type WaitForTimerAction =
    | StartResult of StartTimerResult

type WaitForTimerResult =
    | StartTimerFailed of StartTimerFailedEventAttributes
    | Canceled of TimerCanceledEventAttributes
    | Fired of TimerFiredEventAttributes

type CancelTimerAction =
    | StartResult of StartTimerResult

type CancelTimerResult =
    | NotStarted
    | CancelTimerFailed of CancelTimerFailedEventAttributes
    | Canceling
    | Canceled of TimerCanceledEventAttributes
    | Fired of TimerFiredEventAttributes

type WorkflowExecutionCancelRequestedAction =
    | Attributes of unit

type WorkflowExecutionCancelRequestedResult =
    | NotRequested
    | CancelRequested of WorkflowExecutionCancelRequestedEventAttributes

type GetWorkflowExecutionInputAction =
    | Attributes of unit

type ReturnResult = 
    | RespondDecisionTaskCompleted
    | CompleteWorkflowExecution of Result:string
    | CancelWorkflowExecution of Details:string
    | FailWorkflowExecution of Reason:string * Details:string
    | ContinueAsNewWorkflowExecution of ContinueAsNewWorkflowExecutionDecisionAttributes

exception CompleteWorkflowExecutionFailedException of CompleteWorkflowExecutionFailedEventAttributes
exception CancelWorkflowExecutionFailedException of CancelWorkflowExecutionFailedEventAttributes
exception FailWorkflowExecutionFailedException of FailWorkflowExecutionFailedEventAttributes
exception ContinueAsNewWorkflowExecutionFailedException of ContinueAsNewWorkflowExecutionFailedEventAttributes
