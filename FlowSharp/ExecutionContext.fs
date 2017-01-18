namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

module ExecutionContext =

    type IContextManager =
        interface
            abstract member Read : string   -> unit
            abstract member Write: unit     -> string

            abstract member Pull : ScheduleActivityTaskAction               -> ScheduleActivityTaskAction
            abstract member Pull : ScheduleAndWaitForActivityTaskAction     -> ScheduleAndWaitForActivityTaskAction
            abstract member Pull : ScheduleAndWaitForLambdaFunctionAction   -> ScheduleAndWaitForLambdaFunctionAction
            abstract member Pull : StartChildWorkflowExecutionAction        -> StartChildWorkflowExecutionAction
            abstract member Pull : StartTimerAction                         -> StartTimerAction
            abstract member Pull : WorkflowExecutionSignaledAction          -> WorkflowExecutionSignaledAction
            abstract member Pull : WaitForWorkflowExecutionSignaledAction   -> WaitForWorkflowExecutionSignaledAction
            abstract member Pull : SignalExternalWorkflowExecutionAction    -> SignalExternalWorkflowExecutionAction
            abstract member Pull : RecordMarkerAction                       -> RecordMarkerAction
            abstract member Pull : MarkerRecordedAction                     -> MarkerRecordedAction

            abstract member Push : ScheduleActivityTaskAction * ScheduleActivityTaskResult                          -> unit
            abstract member Push : ScheduleAndWaitForActivityTaskAction * ScheduleActivityTaskResult                -> unit
            abstract member Push : ScheduleAndWaitForLambdaFunctionAction * ScheduleAndWaitForLambdaFunctionResult  -> unit
            abstract member Push : StartChildWorkflowExecutionAction * StartChildWorkflowExecutionResult            -> unit
            abstract member Push : StartTimerAction * StartTimerResult                                              -> unit
            abstract member Push : WorkflowExecutionSignaledAction * WorkflowExecutionSignaledResult                -> unit
            abstract member Push : WaitForWorkflowExecutionSignaledAction * WorkflowExecutionSignaledResult         -> unit
            abstract member Push : SignalExternalWorkflowExecutionAction * SignalExternalWorkflowExecutionResult    -> unit
            abstract member Push : RecordMarkerAction * RecordMarkerResult                                          -> unit
            abstract member Push : MarkerRecordedAction * MarkerRecordedResult                                      -> unit
        end

    type DefaultContextManager() =
        let mutable actionToResultMap = Map.empty
    
        interface IContextManager with
            member this.Read(executionContext:string) : unit = ()
            member this.Write() = "hi"
 
            member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = action
            member this.Push(action:ScheduleActivityTaskAction, result:ScheduleActivityTaskResult) : unit = ()

            member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = action
            member this.Push(action:ScheduleAndWaitForActivityTaskAction, result:ScheduleActivityTaskResult) : unit = ()

            member this.Pull(action:ScheduleAndWaitForLambdaFunctionAction) : ScheduleAndWaitForLambdaFunctionAction = action
            member this.Push(action:ScheduleAndWaitForLambdaFunctionAction, result:ScheduleAndWaitForLambdaFunctionResult) : unit = ()

            member this.Pull(action:StartChildWorkflowExecutionAction) : StartChildWorkflowExecutionAction = action
            member this.Push(action:StartChildWorkflowExecutionAction, result:StartChildWorkflowExecutionResult) : unit = ()

            member this.Pull(action:StartTimerAction) : StartTimerAction = action
            member this.Push(action:StartTimerAction, result:StartTimerResult) : unit = ()

            member this.Pull(action:WorkflowExecutionSignaledAction) : WorkflowExecutionSignaledAction =
                match action with
                | WorkflowExecutionSignaledAction.Attributes(signalName, _) ->
                    if signalName = "From Context" then
                        let attr = WorkflowExecutionSignaledEventAttributes(SignalName=signalName, Input="Some Input")
                        WorkflowExecutionSignaledAction.ResultFromContext(WorkflowExecutionSignaledResult.Signaled(attr))
                    else 
                        action

                | _ -> action

            member this.Push(action:WorkflowExecutionSignaledAction, result:WorkflowExecutionSignaledResult) : unit =
                ()

            member this.Pull(action:WaitForWorkflowExecutionSignaledAction) : WaitForWorkflowExecutionSignaledAction = action
            member this.Push(action:WaitForWorkflowExecutionSignaledAction, result:WorkflowExecutionSignaledResult) : unit = ()

            member this.Pull(action:SignalExternalWorkflowExecutionAction) : SignalExternalWorkflowExecutionAction = action
            member this.Push(action:SignalExternalWorkflowExecutionAction, result:SignalExternalWorkflowExecutionResult) : unit = ()

            member this.Pull(action:RecordMarkerAction) : RecordMarkerAction = action
            member this.Push(action:RecordMarkerAction, result:RecordMarkerResult) : unit = ()

            member this.Pull(action:MarkerRecordedAction) : MarkerRecordedAction = action
            member this.Push(action:MarkerRecordedAction, result:MarkerRecordedResult) : unit = ()

            (*
        let ParseExecutionContext () =
                execution-context :=
                    action-type-name '(' label '=' string-literal [',' label '=' string-literal]... ')' '=>'  result-type-name '(' label '=' string-literal [',' label '=' string-literal]... ')'

                action-type-name :=
                    label of 
                        'ActivityTask' | 
                        'LambdaFunction' |
                        'ChildWorkflowExecution' |
                        'Timer' |
                        'Signal' |
                        'Marker'

                result-type-name :=
                    label of 
                        when action-type-name = 'ActivityTask' then
                            'Completed' | 'Canceled' | 'Failed' | 'TimedOut'
                        when action-type-name = 'LambdaFunction' then
                            'Completed' | 'Failed' | 'TimedOut'
                        when action-type-name = 'ChildWorkflowExecution' then
                            'Completed' | 'Canceled' | 'Failed' | 'TimedOut' | 'Terminated'
                        when action-type-name = 'Timer' then
                            'Started' | 'Fired' | 'Canceled'
                        when action-type-name = 'Signal' then
                            'NotSignaled' | 'Signaled'

            ()
            *)

