namespace FlowSharp.ExecutionContext

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.ContextExpression

module internal Extensions =
    type ScheduleActivityTaskDecisionAttributes with
        member this.GetKey() =
            [
                this.ActivityId
                this.ActivityType.Name
                this.ActivityType.Version
                this.Control
            ]

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
        
open Extensions

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
    let mutable actionToResultMap = Map.empty<string list, obj>

    let AddMapping (key:string list) (result:obj) =
        actionToResultMap <- Map.add key result actionToResultMap
    
    let FindParameter (name:string) (parameters:Parameter list) : Parameter option =
        let Find (name:string) (Parameter.NameAndValue(label, _)) : bool =
            match label with
            | Label.Text(n) when n = name -> true
            | _ -> false

        parameters
        |> List.tryFind (Find name)

    let rec ReadParameterActivityTypeValue (parameters:Parameter list) : ActivityType =
        let activityType = ActivityType()

        let parameter = FindParameter "ActivityType" parameters
        match parameter with
        | Some(Parameter.NameAndValue(_, ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(_, atParameters)))) -> 
            activityType.Name <- ReadParameterStringValue "Name" atParameters
            activityType.Version <- ReadParameterStringValue "Version" atParameters
        | _ -> ()

        activityType

    and ReadParameterStringValue (name:string) (parameters:Parameter list) : string =
        let parameter = FindParameter name parameters

        match parameter with
        | Some(Parameter.NameAndValue(_, ParameterValue.StringValue(value))) -> value
        | _ -> null

    member private this.ReadScheduleActivityTaskDecisionAttributes (ObjectInitialization.NameAndParameters(_, parameters)) : ScheduleActivityTaskDecisionAttributes =
        let attr = ScheduleActivityTaskDecisionAttributes()
        attr.ActivityId     <- ReadParameterStringValue "ActivityId" parameters
        attr.ActivityType   <- ReadParameterActivityTypeValue parameters
        attr.Control        <- ReadParameterStringValue "Control" parameters
        attr

    member private this.ReadScheduleActivityTaskResult(result:ObjectInitialization) : ScheduleActivityTaskResult =
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
        
    member private this.ReadActionToResultMapping (ActionToResultMapping.ActionAndResult(action, result)) =
        match action with
        | ObjectInitialization.NameAndParameters(name, parameters) -> 
            match name with
            | Label.Text("ScheduleActivityTaskAction") -> 
                let attr = this.ReadScheduleActivityTaskDecisionAttributes(action)
                let scheduleResult = this.ReadScheduleActivityTaskResult(result)
                this.Push(ScheduleActivityTaskAction.Attributes(attr, true), scheduleResult)

            | Label.Text("ScheduleAndWaitForActivityTaskAction") -> 
                let attr = this.ReadScheduleActivityTaskDecisionAttributes(action)
                let scheduleResult = this.ReadScheduleActivityTaskResult(result)
                this.Push(ScheduleAndWaitForActivityTaskAction.Attributes(attr, true), scheduleResult)
                
            | Label.Text("ScheduleAndWaitForLambdaFunctionAction") -> ()
            | Label.Text("StartChildWorkflowExecutionAction") -> ()
            | Label.Text("StartTimerAction") -> ()
            | Label.Text("WorkflowExecutionSignaledAction") -> ()
            | Label.Text("WaitForWorkflowExecutionSignaledAction") -> ()
            | Label.Text("SignalExternalWorkflowExecutionAction") -> ()
            | Label.Text("RecordMarkerAction") -> ()
            | Label.Text("MarkerRecordedAction") -> ()
            | _ -> ()

    member private this.ReadActionToResultMappings (mappings:ActionToResultMapping list) =
        match mappings with 
        | [] -> ()
        | h :: t -> 
            this.ReadActionToResultMapping(h)
            this.ReadActionToResultMappings(t)

    member this.Read(executionContext:string) : unit =
        let parser = Parser()
        let result = parser.TryParseExecutionContext(executionContext)
        match result with
        | None -> ()
        | Some(ContextExpression.Mappings(mappings)) ->
            this.ReadActionToResultMappings(mappings)


    member this.Write() = 
        let writer = Writer()
        ""
        //writer.Write()
    

    member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = 
        let attr = action.GetAttributes()
        let key = attr.GetKey()
        let result = actionToResultMap.TryFind key

        match result with
        | None -> action
        | Some(r) -> ScheduleActivityTaskAction.ResultFromContext(attr, r :?> ScheduleActivityTaskResult)

    member this.Push(action:ScheduleActivityTaskAction, result:ScheduleActivityTaskResult) : unit = 
        let attr = action.GetAttributes()
        let key = attr.GetKey()
        AddMapping key (result :> obj)

    member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = 
        let attr = action.GetAttributes()
        let key = attr.GetKey()
        let result = actionToResultMap.TryFind key

        match result with
        | None -> action
        | Some(r) -> ScheduleAndWaitForActivityTaskAction.ResultFromContext(attr, r :?> ScheduleActivityTaskResult)

    member this.Push(action:ScheduleAndWaitForActivityTaskAction, result:ScheduleActivityTaskResult) : unit = 
        let attr = 
            match action with
            | ScheduleAndWaitForActivityTaskAction.ResultFromContext(attr, _) -> attr
            | ScheduleAndWaitForActivityTaskAction.Attributes(attr, _) -> attr
        
        let key = attr.GetKey()
        AddMapping key (result :> obj)


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




    interface IContextManager with 
        member this.Pull(action:ScheduleActivityTaskAction) : ScheduleActivityTaskAction = this.Pull(action)
        member this.Push(action:ScheduleActivityTaskAction, result:ScheduleActivityTaskResult) : unit = this.Push(action, result)
        member this.Pull(action:ScheduleAndWaitForActivityTaskAction) : ScheduleAndWaitForActivityTaskAction = this.Pull(action)
        member this.Push(action:ScheduleAndWaitForActivityTaskAction, result:ScheduleActivityTaskResult) : unit = this.Push(action, result)
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
