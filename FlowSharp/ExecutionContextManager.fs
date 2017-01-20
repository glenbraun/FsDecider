namespace FlowSharp.ExecutionContext

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.ContextExpression
open FlowSharp.ContextExpression.Extensions

type IContextManager =
    interface 
        abstract member Read : string   -> unit
        abstract member Write: unit     -> string

        abstract member Push : ScheduleActivityTaskDecisionAttributes * ScheduleActivityTaskResult                          -> unit
        abstract member Push : ScheduleLambdaFunctionDecisionAttributes * ScheduleAndWaitForLambdaFunctionResult            -> unit
        abstract member Push : StartChildWorkflowExecutionDecisionAttributes * StartChildWorkflowExecutionResult            -> unit
        abstract member Push : StartTimerDecisionAttributes * StartTimerResult                                              -> unit
        abstract member Push : string * WorkflowExecutionSignaledResult                                                     -> unit
        abstract member Push : SignalExternalWorkflowExecutionDecisionAttributes * SignalExternalWorkflowExecutionResult    -> unit
        abstract member Push : RecordMarkerDecisionAttributes * RecordMarkerResult                                          -> unit
        abstract member Push : string * MarkerRecordedResult                                                                -> unit

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
            

type ExecutionContextManager() =
    let mutable actionToResultMap = Map.empty<ObjectInitialization, ObjectInitialization>
    let mutable actionToResultKeys = List.empty<ObjectInitialization>

    let AddMapping (key:ObjectInitialization) (result:ObjectInitialization) =
        actionToResultMap <- Map.add key result actionToResultMap
        actionToResultKeys <- key :: actionToResultKeys
    
    member private this.ReadActionToResultMapping (ActionToResultMapping.ActionAndResult(action, result)) =
        match action with
        | ObjectInitialization.NameAndParameters(name, parameters) -> 
            match name with
            | Label.Text("ScheduleActivityTask") -> 
                let attr = ScheduleActivityTaskDecisionAttributes.CreateFromExpression(action)
                let scheduleResult = ScheduleActivityTaskResult.CreateFromExpression(result)
                this.Push(attr, scheduleResult)
                
            | Label.Text("ScheduleLambdaFunction") ->
                let attr = ScheduleLambdaFunctionDecisionAttributes.CreateFromExpression(action)
                let scheduleResult = ScheduleAndWaitForLambdaFunctionResult.CreateFromExpression(result)
                this.Push(attr, scheduleResult)

            | Label.Text("StartChildWorkflowExecution") ->
                let attr = StartChildWorkflowExecutionDecisionAttributes.CreateFromExpression(action)
                let startResult = StartChildWorkflowExecutionResult.CreateFromExpression(result)
                this.Push(attr, startResult)

            | Label.Text("StartTimer") ->
                let attr = StartTimerDecisionAttributes.CreateFromExpression(action)
                let startResult = StartTimerResult.CreateFromExpression(result)
                this.Push(attr, startResult)

            | Label.Text("WorkflowExecutionSignaled") ->
                let signalName = ReadParameterStringValue "SignalName" parameters
                let signalResult = WorkflowExecutionSignaledResult.CreateFromExpression(result)
                this.Push(signalName, signalResult)

            | Label.Text("SignalExternalWorkflowExecution") -> 
                let attr = SignalExternalWorkflowExecutionDecisionAttributes.CreateFromExpression(action)
                let signalResult = SignalExternalWorkflowExecutionResult.CreateFromExpression(result)
                this.Push(attr, signalResult)

            | Label.Text("RecordMarker") -> 
                let attr = RecordMarkerDecisionAttributes.CreateFromExpression(action)
                let markerResult = RecordMarkerResult.CreateFromExpression(result)
                this.Push(attr, markerResult)

            | Label.Text("MarkerRecorded") -> 
                let markerName = ReadParameterStringValue "MarkerName" parameters
                let markerResult = MarkerRecordedResult.CreateFromExpression(result)
                this.Push(markerName, markerResult)

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


    member this.Push(attr:ScheduleLambdaFunctionDecisionAttributes, result:ScheduleAndWaitForLambdaFunctionResult) : unit = ()
    member this.Pull(action:ScheduleAndWaitForLambdaFunctionAction) : ScheduleAndWaitForLambdaFunctionAction = action

    member this.Push(attr:StartChildWorkflowExecutionDecisionAttributes, result:StartChildWorkflowExecutionResult) : unit = ()
    member this.Pull(action:StartChildWorkflowExecutionAction) : StartChildWorkflowExecutionAction = action

    member this.Push(attr:StartTimerDecisionAttributes, result:StartTimerResult) : unit = ()
    member this.Pull(action:StartTimerAction) : StartTimerAction = action

    member this.Pull(action:WorkflowExecutionSignaledAction) : WorkflowExecutionSignaledAction = action
    member this.Push(action:WorkflowExecutionSignaledAction, result:WorkflowExecutionSignaledResult) : unit = ()

    member this.Push(signalName:string, result:WorkflowExecutionSignaledResult) : unit = ()
    member this.Pull(action:WaitForWorkflowExecutionSignaledAction) : WaitForWorkflowExecutionSignaledAction = action

    member this.Push(attr:SignalExternalWorkflowExecutionDecisionAttributes, result:SignalExternalWorkflowExecutionResult) : unit = ()
    member this.Pull(action:SignalExternalWorkflowExecutionAction) : SignalExternalWorkflowExecutionAction = action

    member this.Push(attr:RecordMarkerDecisionAttributes, result:RecordMarkerResult) : unit = ()
    member this.Pull(action:RecordMarkerAction) : RecordMarkerAction = action

    member this.Push(markerName:string, result:MarkerRecordedResult) : unit = ()
    member this.Pull(action:MarkerRecordedAction) : MarkerRecordedAction = action

    interface IContextManager with 
        member this.Push(attr:ScheduleActivityTaskDecisionAttributes, result:ScheduleActivityTaskResult) : unit = this.Push(attr, result)
        member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = this.Pull(action)
        member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = this.Pull(action)

        member this.Push(attr:ScheduleLambdaFunctionDecisionAttributes, result:ScheduleAndWaitForLambdaFunctionResult) : unit = this.Push(attr, result)
        member this.Pull(action:ScheduleAndWaitForLambdaFunctionAction) : ScheduleAndWaitForLambdaFunctionAction = this.Pull(action)

        member this.Push(attr:StartChildWorkflowExecutionDecisionAttributes, result:StartChildWorkflowExecutionResult) : unit = this.Push(attr, result)
        member this.Pull(action:StartChildWorkflowExecutionAction) : StartChildWorkflowExecutionAction = this.Pull(action)

        member this.Push(attr:StartTimerDecisionAttributes, result:StartTimerResult) : unit = this.Push(attr, result)
        member this.Pull(action:StartTimerAction) : StartTimerAction = this.Pull(action)

        member this.Push(signalName:string, result:WorkflowExecutionSignaledResult) : unit = this.Push(signalName, result)
        member this.Pull(action:WorkflowExecutionSignaledAction) : WorkflowExecutionSignaledAction = this.Pull(action)
        member this.Pull(action:WaitForWorkflowExecutionSignaledAction) : WaitForWorkflowExecutionSignaledAction = this.Pull(action)

        member this.Push(attr:SignalExternalWorkflowExecutionDecisionAttributes, result:SignalExternalWorkflowExecutionResult) : unit = this.Push(attr, result)
        member this.Pull(action:SignalExternalWorkflowExecutionAction) : SignalExternalWorkflowExecutionAction = this.Pull(action)

        member this.Push(attr:RecordMarkerDecisionAttributes, result:RecordMarkerResult) : unit = this.Push(attr, result)
        member this.Pull(action:RecordMarkerAction) : RecordMarkerAction = this.Pull(action)

        member this.Push(markerName:string, result:MarkerRecordedResult) : unit = this.Push(markerName, result)
        member this.Pull(action:MarkerRecordedAction) : MarkerRecordedAction = this.Pull(action)

        member this.Read(executionContext:string) : unit = this.Read(executionContext)
        member this.Write() = this.Write()
