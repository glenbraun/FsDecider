namespace FlowSharp.ExecutionContext

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.ContextExpression

module internal Extensions =
    type ScheduleActivityTaskDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = ScheduleActivityTaskDecisionAttributes()
            attr.ActivityId     <- ReadParameterStringValue "ActivityId" parameters
            attr.ActivityType   <- ReadParameterActivityTypeValue parameters
            attr.Control        <- ReadParameterStringValue "Control" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("ScheduleActivityTaskDecisionAttributes"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("ActivityId"), Value=ParameterValue.StringValue(this.ActivityId))
                            Parameter.NameAndValue
                                (
                                    Name=Label.Text("ActivityType"),
                                    Value=ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters
                                                (
                                                    Name=Label.Text("ActivityType"),
                                                    Parameters=
                                                        [
                                                            Parameter.NameAndValue(Name=Label.Text("Name"), Value=ParameterValue.StringValue(this.ActivityType.Name))
                                                            Parameter.NameAndValue(Name=Label.Text("Version"), Value=ParameterValue.StringValue(this.ActivityType.Version))
                                                        ]
                                                )
                                        )
                                )
                        ] @ 
                        if this.Control <> null then
                            [ Parameter.NameAndValue(Name=Label.Text("Control"), Value=ParameterValue.StringValue(this.Control)) ]
                        else []
                )

    type ScheduleActivityTaskAction with
        member this.GetAttributes() =
            match this with
            | ScheduleActivityTaskAction.ResultFromContext(attr, _) -> attr
            | ScheduleActivityTaskAction.Attributes(attr, _) -> attr

    type ScheduleAndWaitForActivityTaskAction with
        member this.GetAttributes() =
            match this with
            | ScheduleAndWaitForActivityTaskAction.ResultFromContext(attr, _) -> attr
            | ScheduleAndWaitForActivityTaskAction.Attributes(attr, _) -> attr

    type ScheduleActivityTaskResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Completed"), parameters) -> 
                let completed = ActivityTaskCompletedEventAttributes()
                completed.Result <- ReadParameterStringValue "Result" parameters
                ScheduleActivityTaskResult.Completed(completed)

            | ObjectInitialization.NameAndParameters(Label.Text("Canceled"), parameters) -> 
                let canceled = ActivityTaskCanceledEventAttributes()
                canceled.Details <- ReadParameterStringValue "Details" parameters
                ScheduleActivityTaskResult.Canceled(canceled)

            | ObjectInitialization.NameAndParameters(Label.Text("Failed"), parameters) ->
                let failed = ActivityTaskFailedEventAttributes()
                failed.Reason <- ReadParameterStringValue "Reason" parameters
                failed.Details <- ReadParameterStringValue "Details" parameters
                ScheduleActivityTaskResult.Failed(failed)

            | ObjectInitialization.NameAndParameters(Label.Text("TimedOut"), parameters) ->
                let timedOut = ActivityTaskTimedOutEventAttributes()
                timedOut.TimeoutType <- ActivityTaskTimeoutType.FindValue(ReadParameterStringValue "TimeoutType" parameters)
                timedOut.Details <- ReadParameterStringValue "TimeoutType" parameters
                ScheduleActivityTaskResult.TimedOut(timedOut)

            | ObjectInitialization.NameAndParameters(Label.Text("ScheduleFailed"), parameters) -> 
                let scheduleFailed = ScheduleActivityTaskFailedEventAttributes()
                scheduleFailed.ActivityId <- ReadParameterStringValue "ActivityId" parameters
                scheduleFailed.ActivityType <- ReadParameterActivityTypeValue parameters
                scheduleFailed.Cause <- ScheduleActivityTaskFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                ScheduleActivityTaskResult.ScheduleFailed(scheduleFailed)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | ScheduleActivityTaskResult.Completed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Completed"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("Result"), Value=ParameterValue.StringValue(attr.Result))
                        ]
                    )
            | ScheduleActivityTaskResult.Canceled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Canceled"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("Details"), Value=ParameterValue.StringValue(attr.Details))
                        ]
                    )
            | ScheduleActivityTaskResult.Failed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Failed"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("Reason"), Value=ParameterValue.StringValue(attr.Reason))
                            Parameter.NameAndValue(Name=Label.Text("Details"), Value=ParameterValue.StringValue(attr.Details))
                        ]
                    )
            | ScheduleActivityTaskResult.TimedOut(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("TimedOut"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("TimeoutType"), Value=ParameterValue.StringValue(attr.TimeoutType.Value))
                            Parameter.NameAndValue(Name=Label.Text("Details"), Value=ParameterValue.StringValue(attr.Details))
                        ]
                    )
            | ScheduleActivityTaskResult.ScheduleFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("ScheduleFailed"),
                    Parameters=
                        [
                            Parameter.NameAndValue(Name=Label.Text("ActivityId"), Value=ParameterValue.StringValue(attr.ActivityId))
                            Parameter.NameAndValue
                                (
                                    Name=Label.Text("ActivityType"),
                                    Value=ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters
                                                (
                                                    Name=Label.Text("ActivityType"),
                                                    Parameters=
                                                        [
                                                            Parameter.NameAndValue(Name=Label.Text("Name"), Value=ParameterValue.StringValue(attr.ActivityType.Name))
                                                            Parameter.NameAndValue(Name=Label.Text("Version"), Value=ParameterValue.StringValue(attr.ActivityType.Version))
                                                        ]
                                                )
                                        )
                                )
                            Parameter.NameAndValue(Name=Label.Text("Cause"), Value=ParameterValue.StringValue(attr.Cause.Value))
                        ]
                    )
            | _ -> failwith "error"

        
open Extensions

type IContextManager =
    interface 
        abstract member Read : string   -> unit
        abstract member Write: unit     -> string

        abstract member Push : ScheduleActivityTaskDecisionAttributes * ScheduleActivityTaskResult              -> unit

        abstract member Push : ScheduleAndWaitForLambdaFunctionAction * ScheduleAndWaitForLambdaFunctionResult  -> unit
        abstract member Push : StartChildWorkflowExecutionAction * StartChildWorkflowExecutionResult            -> unit
        abstract member Push : StartTimerAction * StartTimerResult                                              -> unit
        abstract member Push : WorkflowExecutionSignaledAction * WorkflowExecutionSignaledResult                -> unit
        abstract member Push : WaitForWorkflowExecutionSignaledAction * WorkflowExecutionSignaledResult         -> unit
        abstract member Push : SignalExternalWorkflowExecutionAction * SignalExternalWorkflowExecutionResult    -> unit
        abstract member Push : RecordMarkerAction * RecordMarkerResult                                          -> unit
        abstract member Push : MarkerRecordedAction * MarkerRecordedResult                                      -> unit

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
    end
            

type DefaultContextManager() =
    let mutable actionToResultMap = Map.empty<ObjectInitialization, ObjectInitialization>
    let mutable actionToResultKeys = List.empty<ObjectInitialization>

    let AddMapping (key:ObjectInitialization) (result:ObjectInitialization) =
        actionToResultMap <- Map.add key result actionToResultMap
        actionToResultKeys <- key :: actionToResultKeys
    
    member private this.ReadActionToResultMapping (ActionToResultMapping.ActionAndResult(action, result)) =
        match action with
        | ObjectInitialization.NameAndParameters(name, parameters) -> 
            match name with
            | Label.Text("ScheduleActivityTaskDecisionAttributes") -> 
                let attr = ScheduleActivityTaskDecisionAttributes.CreateFromExpression(action)
                let scheduleResult = ScheduleActivityTaskResult.CreateFromExpression(result)
                this.Push(attr, scheduleResult)
                
            | Label.Text("ScheduleAndWaitForLambdaFunctionAction") -> ()
            | Label.Text("StartChildWorkflowExecutionAction") -> ()
            | Label.Text("StartTimerAction") -> ()
            | Label.Text("WorkflowExecutionSignaledAction") -> ()
            | Label.Text("WaitForWorkflowExecutionSignaledAction") -> ()
            | Label.Text("SignalExternalWorkflowExecutionAction") -> ()
            | Label.Text("RecordMarkerAction") -> ()
            | Label.Text("MarkerRecordedAction") -> ()
            | _ -> ()

    member this.Read(executionContext:string) : unit =
        let parser = Parser()
        let result = parser.TryParseExecutionContext(executionContext)
        match result with
        | None -> ()
        | Some(ContextExpression.Mappings(mappings)) ->
            mappings |> List.iter (this.ReadActionToResultMapping)

    member this.Write() = 
        let mappings =
            actionToResultKeys 
            |> List.map (fun k -> ActionToResultMapping.ActionAndResult(k, actionToResultMap.[k]))
            
        let writer = Writer()
        writer.Write(ContextExpression.Mappings(mappings))    

    member this.Push(attr:ScheduleActivityTaskDecisionAttributes, result:ScheduleActivityTaskResult) : unit = 
        let key = attr.GetExpression()
        AddMapping key (result.GetExpression())

    member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = 
        let attr = action.GetAttributes()
        let key = attr.GetExpression()
        let result = actionToResultMap.TryFind key

        match result with
        | None -> action
        | Some(r) -> ScheduleActivityTaskAction.ResultFromContext(attr, ScheduleActivityTaskResult.CreateFromExpression(r))

    member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = 
        let attr = action.GetAttributes()
        let key = attr.GetExpression()
        let result = actionToResultMap.TryFind key

        match result with
        | None -> action
        | Some(r) -> ScheduleAndWaitForActivityTaskAction.ResultFromContext(attr, ScheduleActivityTaskResult.CreateFromExpression(r))


    member this.Pull(action:ScheduleAndWaitForLambdaFunctionAction) : ScheduleAndWaitForLambdaFunctionAction = action
    member this.Push(action:ScheduleAndWaitForLambdaFunctionAction, result:ScheduleAndWaitForLambdaFunctionResult) : unit = ()

    member this.Pull(action:StartChildWorkflowExecutionAction) : StartChildWorkflowExecutionAction = action
    member this.Push(action:StartChildWorkflowExecutionAction, result:StartChildWorkflowExecutionResult) : unit = ()

    member this.Pull(action:StartTimerAction) : StartTimerAction = action
    member this.Push(action:StartTimerAction, result:StartTimerResult) : unit = ()

    member this.Pull(action:WorkflowExecutionSignaledAction) : WorkflowExecutionSignaledAction = action
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




    interface IContextManager with 
        member this.Push(attr:ScheduleActivityTaskDecisionAttributes, result:ScheduleActivityTaskResult) : unit = this.Push(attr, result)
        member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = this.Pull(action)
        member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = this.Pull(action)

        member this.Pull(action:ScheduleAndWaitForLambdaFunctionAction) : ScheduleAndWaitForLambdaFunctionAction = this.Pull(action)
        member this.Push(action:ScheduleAndWaitForLambdaFunctionAction, result:ScheduleAndWaitForLambdaFunctionResult) : unit = this.Push(action, result)
        member this.Pull(action:StartChildWorkflowExecutionAction) : StartChildWorkflowExecutionAction = this.Pull(action)
        member this.Push(action:StartChildWorkflowExecutionAction, result:StartChildWorkflowExecutionResult) : unit = this.Push(action, result)
        member this.Pull(action:StartTimerAction) : StartTimerAction = this.Pull(action)
        member this.Push(action:StartTimerAction, result:StartTimerResult) : unit = this.Push(action, result)
        member this.Pull(action:WorkflowExecutionSignaledAction) : WorkflowExecutionSignaledAction = this.Pull(action)
        member this.Push(action:WorkflowExecutionSignaledAction, result:WorkflowExecutionSignaledResult) : unit = this.Push(action, result)
        member this.Pull(action:WaitForWorkflowExecutionSignaledAction) : WaitForWorkflowExecutionSignaledAction = this.Pull(action)
        member this.Push(action:WaitForWorkflowExecutionSignaledAction, result:WorkflowExecutionSignaledResult) : unit = this.Push(action, result)
        member this.Pull(action:SignalExternalWorkflowExecutionAction) : SignalExternalWorkflowExecutionAction = this.Pull(action)
        member this.Push(action:SignalExternalWorkflowExecutionAction, result:SignalExternalWorkflowExecutionResult) : unit = this.Push(action, result)
        member this.Pull(action:RecordMarkerAction) : RecordMarkerAction = this.Pull(action)
        member this.Push(action:RecordMarkerAction, result:RecordMarkerResult) : unit = this.Push(action, result)
        member this.Pull(action:MarkerRecordedAction) : MarkerRecordedAction = this.Pull(action)
        member this.Push(action:MarkerRecordedAction, result:MarkerRecordedResult) : unit = this.Push(action, result)
        member this.Read(executionContext:string) : unit = this.Read(executionContext)
        member this.Write() = this.Write()
