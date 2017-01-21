module internal FlowSharp.ContextExpression

open System
open System.Text

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

type internal ContextExpression =
    | Mappings of ActionToResultMapping list
and ActionToResultMapping =
    | ActionAndResult of Action:ObjectInitialization * Result:ObjectInitialization
and ObjectInitialization =
    | NameAndParameters of Name:Label * Parameters:Parameter list
and Label =
    | Text of string
and Parameter =
    | NameAndValue of Name:Label * Value:ParameterValue
and ParameterValue =
    | StringValue of string
    | IntValue of int
    | ObjectValue of ObjectInitialization


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
    | _ -> failwith "error"

    activityType

and ReadParameterWorkflowTypeValue (parameters:Parameter list) : WorkflowType =
    let workflowType = WorkflowType()

    let parameter = FindParameter "WorkflowType" parameters
    match parameter with
    | Some(Parameter.NameAndValue(_, ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(_, atParameters)))) -> 
        workflowType.Name <- ReadParameterStringValue "Name" atParameters
        workflowType.Version <- ReadParameterStringValue "Version" atParameters
    | _ -> failwith "error"

    workflowType

and ReadParameterWorkflowExecutionValue (parameters:Parameter list) : WorkflowExecution =
    let workflowExecution = WorkflowExecution()

    let parameter = FindParameter "WorkflowExecution" parameters
    match parameter with
    | Some(Parameter.NameAndValue(_, ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(_, atParameters)))) -> 
        workflowExecution.RunId <- ReadParameterStringValue "RunId" atParameters
        workflowExecution.WorkflowId <- ReadParameterStringValue "WorkflowId" atParameters
    | _ -> failwith "error"

    workflowExecution

and ReadParameterStringValue (name:string) (parameters:Parameter list) : string =
    let parameter = FindParameter name parameters

    match parameter with
    | Some(Parameter.NameAndValue(_, ParameterValue.StringValue(value))) -> value
    | _ -> null

type internal Writer() =
    let sb = StringBuilder()

    let WriteChar (c:Char) =
        Printf.bprintf sb "%c" c

    let WriteString (s:string) = 
        Printf.bprintf sb "%s" s

    let rec WriteLabel (Label.Text(s)) =
        WriteString s

    and WriteParameterValueString (s:string) =
        let WriteStringBodyChar c =
            match c with
            | '\"' ->
                WriteChar '\\'
                WriteChar '\"'

            | _ ->
                WriteChar c

        WriteChar '\"'
        s |> Seq.iter (WriteStringBodyChar)
        WriteChar '\"'

    and WriteParameterValue (pv:ParameterValue) =
        match pv with
        | ParameterValue.StringValue(s) -> WriteParameterValueString s
        | ParameterValue.IntValue(i) -> WriteString (i.ToString())
        | ParameterValue.ObjectValue(oi) -> WriteObjectInitialization oi

    and WriteParameter (Parameter.NameAndValue(name, value)) =
        WriteLabel name
        WriteChar '='
        WriteParameterValue value

    and WriteParameters (parameters:Parameter list) =
        match parameters with
        | [] -> ()
        | p :: [] -> WriteParameter p
        | p :: t ->
            WriteParameter p
            WriteChar ','
            WriteParameters t

    and WriteObjectInitialization (ObjectInitialization.NameAndParameters(name, parameters)) =
        WriteLabel name
        WriteChar '('
        WriteParameters parameters
        WriteChar ')'

    let WriteActivityToResultMapping (ActionToResultMapping.ActionAndResult(activity, result)) =
        WriteObjectInitialization activity
        WriteString "=>"
        WriteObjectInitialization result

    let rec WriteActivityToResultMappings (mappings:ActionToResultMapping list) =
        match mappings with
        | [] -> ()
        | m :: [] -> WriteActivityToResultMapping m
        | m :: t -> 
            WriteActivityToResultMapping m
            WriteString Environment.NewLine
            WriteActivityToResultMappings t

    member this.Write(ContextExpression.Mappings(mappings)) =
        sb.Clear() |> ignore
        
        WriteActivityToResultMappings mappings
        
        if sb.Length = 0 then
            null
        else 
            sb.ToString()
        
type internal Parser() = 
    let rec SkipWhiteSpace (chars:char list) : char list =
        match chars with
        | c :: t when Char.IsWhiteSpace(c) -> SkipWhiteSpace t
        | _ -> chars

    let ParseLiteralChar (chars:char list) (literal: char) : (bool * char list) =
        match chars with
        | c :: t when c = literal -> (true, t)
        | _ -> (false, chars)

    let rec ParseLiteralString (chars:char list) (literal: char list) : (bool * char list) =
        match (chars, literal) with
        | (_, []) -> (true, chars)
        | (cc :: ct, lc :: lt) when cc = lc -> ParseLiteralString ct lt
        | _ -> (false, chars)

    let rec ParseLabel (chars:char list) : (string option * char list) =
        let rec ParseLableChars (chars:char list) (label:char list) : (string option * char list) =
            match (chars, label) with
            | (c :: t, _) when Char.IsLetterOrDigit(c) -> ParseLableChars t (c :: label)
            | (_, []) -> (None, chars)
            | _ -> (Some(String(label |> List.rev |> List.toArray)), chars)
    
        ParseLableChars chars []

    and ParseParameterValueInt (chars:char list) : (int option * char list) =
        let rec ParseIntValue (chars:char list) (value:char list) : (int option * char list) =
            match (chars, value) with
            | (c :: t, _) when Char.IsDigit(c) -> ParseIntValue t (c :: value)
            | (_, []) -> (None, chars)
            | _ -> (Some(Convert.ToInt32(String(value |> List.rev |> List.toArray))), chars)

        (None, chars)

    and ParseParameterValueString (chars:char list) : (string option * char list) =

        let rec ParseStringValue (chars:char list) (value:char list) : (string option * char list) =
            match (chars, value) with
            | ('\"' :: t, _) -> (Some(String(value |> List.rev |> List.toArray)), t)
            | ('\\' :: ('\"' :: t), _) -> ParseStringValue t ('\"' :: value)
            | (c :: t, _) -> ParseStringValue t (c :: value)
            | _ -> (None, chars)

        let (ok, chars) = ParseLiteralChar chars '\"'    
        if ok then
            let (stringValue, chars) = ParseStringValue chars []
            match stringValue with
            | Some(s) -> (Some(s), chars)
            | _ -> (None, chars)
        else
            (None, chars)

    and ParseParameterValue (chars:char list) : (ParameterValue option * char list) =
        let (stringValue, chars) = ParseParameterValueString chars
        match stringValue with
        | Some(s) -> (Some(ParameterValue.StringValue(s)), chars)
        | None ->
            let (intValue, chars) = ParseParameterValueInt chars
            match intValue with
            | Some(i) -> (Some(ParameterValue.IntValue(i)), chars)
            | None ->
                let (objectValue, chars) = ParseObjectInitialization chars
                match objectValue with
                | Some(ov) -> (Some(ParameterValue.ObjectValue(ov)), chars)
                | None -> (None, chars)

    and ParseParameter (chars:char list) : (Parameter option * char list) =
        let (name, chars) = ParseLabel chars

        match name with
        | None -> (None, chars)
        | Some(n) -> 
            let chars = SkipWhiteSpace chars
            let (ok, chars) = ParseLiteralChar chars '='
            if ok then
                let chars = SkipWhiteSpace chars
                let (value, chars) = ParseParameterValue chars
                match value with
                | None -> (None, chars)
                | Some(v) -> (Some(Parameter.NameAndValue(Label.Text(n), v)), chars)
            else
                (None, chars)    

    and ParseParameters (chars:char list) (parameters: Parameter list) : (Parameter list * char list) =
        let (parameter, chars) = ParseParameter chars

        match parameter with
        | None -> ([], chars)
        | Some(p) -> 
            let chars = SkipWhiteSpace chars
            let (ok, chars) = ParseLiteralChar chars ','
            if ok then
                let chars = SkipWhiteSpace chars
                ParseParameters chars (p :: parameters)
            else
                ((p :: parameters), chars)

    and ParseObjectInitialization (chars:char list) : (ObjectInitialization option * char list) =
        let (name, chars) = ParseLabel chars
        match name with
        | None -> (None, chars)
        | Some(n) -> 
            let chars = SkipWhiteSpace chars
            let (ok, chars) = ParseLiteralChar chars '('

            if ok then
                let chars = SkipWhiteSpace chars
                let (parameters, chars) = ParseParameters chars []

                let chars = SkipWhiteSpace chars
                let (ok, chars) = ParseLiteralChar chars ')'

                if ok then
                    (Some(ObjectInitialization.NameAndParameters(Label.Text(n), (parameters |> List.rev))), chars)
                else
                    (None, chars)
            else
                (None, chars)

    let ParseActivityToResultMapping (chars:char list) : (ActionToResultMapping option * char list) =
        let (activity, chars) = ParseObjectInitialization chars
        match (activity) with
        | None -> (None, chars)
        | Some(a) -> 
            let chars = SkipWhiteSpace chars
            let (ok, chars) = ParseLiteralString chars ("=>" |> List.ofSeq)
            if ok then
                let chars = SkipWhiteSpace chars
                let (result, chars) = ParseObjectInitialization chars

                match (result) with
                | None -> (None, chars)
                | Some(r) -> (Some(ActionToResultMapping.ActionAndResult(a, r)), chars)
            else
                (None, chars)

    let rec ParseActivityToResultMappings (chars:char list) (mappings:ActionToResultMapping list) : (ActionToResultMapping list * char list) =
        let chars = SkipWhiteSpace chars

        let (mapping, chars) = ParseActivityToResultMapping chars

        match mapping with
        | None -> (mappings, chars)
        | Some(m) -> 
            let chars = SkipWhiteSpace chars
            ParseActivityToResultMappings chars (m :: mappings)

    member this.TryParseExecutionContext (executionContext:string) : ContextExpression option =
        if String.IsNullOrWhiteSpace(executionContext) 
        then None
        else
            let (mappings, chars) = ParseActivityToResultMappings (executionContext |> List.ofSeq) []

            let chars = SkipWhiteSpace chars

            match chars with
            | [] -> Some(ContextExpression.Mappings(mappings))
            | _ -> None
    
module internal Extensions =
    let PSV name value = 
        Parameter.NameAndValue(Name=Label.Text(name), Value=ParameterValue.StringValue(value))

    let PSVOrNull name value =
        match value with
        | null -> []
        | _ -> [ PSV name value ]

    let PActivityType (activityType:ActivityType) =
        Parameter.NameAndValue(Name=Label.Text("ActivityType"),Value=ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(Name=Label.Text("ActivityType"),Parameters=[PSV "Name" (activityType.Name);   PSV "Version" (activityType.Version);])))

    let PWorkflowType (workflowType:WorkflowType) =
        Parameter.NameAndValue(Name=Label.Text("WorkflowType"),Value=ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(Name=Label.Text("WorkflowType"),Parameters=[PSV "Name" (workflowType.Name);   PSV "Version" (workflowType.Version);])))

    let PWorkflowExecution (workflowExecution:WorkflowExecution) =
        Parameter.NameAndValue(Name=Label.Text("WorkflowExecution"),Value=ParameterValue.ObjectValue(ObjectInitialization.NameAndParameters(Name=Label.Text("WorkflowExecution"),Parameters=[PSV "RunId" (workflowExecution.RunId);   PSV "WorkflowId" (workflowExecution.WorkflowId);])))

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
                    Name=Label.Text("ScheduleActivityTask"),
                    Parameters=
                        [
                            PSV "ActivityId" this.ActivityId
                            PActivityType this.ActivityType
                        ] @ 
                        PSVOrNull "Control" this.Control
                )

    type ScheduleLambdaFunctionDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = ScheduleLambdaFunctionDecisionAttributes()
            attr.Id     <- ReadParameterStringValue "Id" parameters
            attr.Name   <- ReadParameterStringValue "Name" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("ScheduleLambdaFunction"),
                    Parameters=
                        [
                            PSV "Id" this.Id
                            PSV "Name" this.Name
                        ] 
                )

    type StartChildWorkflowExecutionDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = StartChildWorkflowExecutionDecisionAttributes()
            attr.WorkflowId     <- ReadParameterStringValue "WorkflowId" parameters
            attr.WorkflowType   <- ReadParameterWorkflowTypeValue parameters
            attr.Control        <- ReadParameterStringValue "Control" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("StartChildWorkflowExecution"),
                    Parameters=
                        [
                            PSV "WorkflowId" this.WorkflowId
                            PWorkflowType this.WorkflowType
                        ] @ 
                        PSVOrNull "Control" this.Control
                )

    type StartTimerDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = StartTimerDecisionAttributes()
            attr.TimerId        <- ReadParameterStringValue "TimerId" parameters
            attr.Control        <- ReadParameterStringValue "Control" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("StartTimer"),
                    Parameters=
                        [
                            PSV "TimerId" this.TimerId
                        ] @
                        PSVOrNull "Control" this.Control 
                )

    type SignalExternalWorkflowExecutionDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = SignalExternalWorkflowExecutionDecisionAttributes()
            attr.Control        <- ReadParameterStringValue "Control" parameters
            attr.Input        <- ReadParameterStringValue "Input" parameters
            attr.RunId        <- ReadParameterStringValue "RunId" parameters
            attr.SignalName        <- ReadParameterStringValue "SignalName" parameters
            attr.WorkflowId        <- ReadParameterStringValue "WorkflowId" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("SignalExternalWorkflowExecution"),
                    Parameters=
                        [
                            PSV "SignalName" this.SignalName
                            PSV "WorkflowId" this.WorkflowId
                        ] @
                        PSVOrNull "RunId" this.RunId 
                          @
                        PSVOrNull "Control" this.Control 
                )

    type RecordMarkerDecisionAttributes with
        static member CreateFromExpression(ObjectInitialization.NameAndParameters(_, parameters)) =
            let attr = RecordMarkerDecisionAttributes()
            attr.MarkerName     <- ReadParameterStringValue "MarkerName" parameters
            attr.Details        <- ReadParameterStringValue "Details" parameters
            attr

        member this.GetExpression() =
            ObjectInitialization.NameAndParameters
                (
                    Name=Label.Text("RecordMarker"),
                    Parameters=
                        [
                            PSV "MarkerName" this.MarkerName
                        ] @
                        PSVOrNull "Details" this.Details
                )

    type ScheduleActivityTaskResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Completed"), parameters) -> 
                let attr = ActivityTaskCompletedEventAttributes()
                attr.Result <- ReadParameterStringValue "Result" parameters
                ScheduleActivityTaskResult.Completed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Canceled"), parameters) -> 
                let attr = ActivityTaskCanceledEventAttributes()
                attr.Details <- ReadParameterStringValue "Details" parameters
                ScheduleActivityTaskResult.Canceled(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Failed"), parameters) ->
                let attr = ActivityTaskFailedEventAttributes()
                attr.Reason <- ReadParameterStringValue "Reason" parameters
                attr.Details <- ReadParameterStringValue "Details" parameters
                ScheduleActivityTaskResult.Failed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("TimedOut"), parameters) ->
                let attr = ActivityTaskTimedOutEventAttributes()
                attr.TimeoutType <- ActivityTaskTimeoutType.FindValue(ReadParameterStringValue "TimeoutType" parameters)
                attr.Details <- ReadParameterStringValue "Details" parameters
                ScheduleActivityTaskResult.TimedOut(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("ScheduleFailed"), parameters) -> 
                let attr = ScheduleActivityTaskFailedEventAttributes()
                attr.ActivityId <- ReadParameterStringValue "ActivityId" parameters
                attr.ActivityType <- ReadParameterActivityTypeValue parameters
                attr.Cause <- ScheduleActivityTaskFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                ScheduleActivityTaskResult.ScheduleFailed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | ScheduleActivityTaskResult.Completed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Completed"), Parameters=PSVOrNull "Result" attr.Result)

            | ScheduleActivityTaskResult.Canceled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Canceled"), Parameters=PSVOrNull "Details" attr.Details)

            | ScheduleActivityTaskResult.Failed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Failed"), Parameters=(PSVOrNull "Reason" attr.Reason) @ (PSVOrNull "Details" attr.Details))

            | ScheduleActivityTaskResult.TimedOut(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("TimedOut"), Parameters=(PSVOrNull "TimeoutType" attr.TimeoutType.Value) @ (PSVOrNull "Details" attr.Details))

            | ScheduleActivityTaskResult.ScheduleFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("ScheduleFailed"),
                    Parameters=
                        [
                            PSV "ActivityId" attr.ActivityId
                            PActivityType attr.ActivityType
                            PSV "Cause" attr.Cause.Value
                        ]
                    )

            | _ -> failwith "error"

    type ScheduleLambdaFunctionResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Completed"), parameters) -> 
                let attr = LambdaFunctionCompletedEventAttributes()
                attr.Result <- ReadParameterStringValue "Result" parameters
                ScheduleLambdaFunctionResult.Completed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Failed"), parameters) -> 
                let attr = LambdaFunctionFailedEventAttributes()
                attr.Reason <- ReadParameterStringValue "Reason" parameters
                attr.Details <- ReadParameterStringValue "Details" parameters
                ScheduleLambdaFunctionResult.Failed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("TimedOut"), parameters) -> 
                let attr = LambdaFunctionTimedOutEventAttributes()
                attr.TimeoutType <- LambdaFunctionTimeoutType.FindValue(ReadParameterStringValue "TimeoutType" parameters)
                ScheduleLambdaFunctionResult.TimedOut(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("StartFailed"), parameters) -> 
                let attr = StartLambdaFunctionFailedEventAttributes()
                attr.Cause <- StartLambdaFunctionFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                attr.Message <- ReadParameterStringValue "Message" parameters
                ScheduleLambdaFunctionResult.StartFailed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("ScheduleFailed"), parameters) -> 
                let attr = ScheduleLambdaFunctionFailedEventAttributes()
                attr.Cause <- ScheduleLambdaFunctionFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                attr.Id <- ReadParameterStringValue "Id" parameters
                attr.Name <- ReadParameterStringValue "Name" parameters
                ScheduleLambdaFunctionResult.ScheduleFailed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | ScheduleLambdaFunctionResult.Completed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Completed"), Parameters=PSVOrNull "Result" attr.Result)

            | ScheduleLambdaFunctionResult.Failed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Failed"), Parameters=(PSVOrNull "Reason" attr.Reason) @ (PSVOrNull "Details" attr.Details))

            | ScheduleLambdaFunctionResult.TimedOut(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("TimedOut"), Parameters=(PSVOrNull "TimeoutType" attr.TimeoutType.Value))

            | ScheduleLambdaFunctionResult.StartFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("StartFailed"), Parameters=(PSVOrNull "Cause" attr.Cause.Value) @ (PSVOrNull "Message" attr.Message))

            | ScheduleLambdaFunctionResult.ScheduleFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("ScheduleFailed"), Parameters=[PSV "Cause" attr.Cause.Value; PSV "Id" attr.Id; PSV "Name" attr.Name])

            | _ -> failwith "error"

    type StartChildWorkflowExecutionResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Completed"), parameters) -> 
                let attr = ChildWorkflowExecutionCompletedEventAttributes()
                attr.Result <- ReadParameterStringValue "Result" parameters
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.Completed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Canceled"), parameters) -> 
                let attr = ChildWorkflowExecutionCanceledEventAttributes()
                attr.Details <- ReadParameterStringValue "Details" parameters
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.Canceled(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Failed"), parameters) -> 
                let attr = ChildWorkflowExecutionFailedEventAttributes()
                attr.Reason <- ReadParameterStringValue "Reason" parameters
                attr.Details <- ReadParameterStringValue "Details" parameters
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.Failed(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("TimedOut"), parameters) -> 
                let attr = ChildWorkflowExecutionTimedOutEventAttributes()
                attr.TimeoutType <- WorkflowExecutionTimeoutType.FindValue(ReadParameterStringValue "TimeoutType" parameters)
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.TimedOut(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Terminated"), parameters) -> 
                let attr = ChildWorkflowExecutionTerminatedEventAttributes()
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.Terminated(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("StartFailed"), parameters) -> 
                let attr = StartChildWorkflowExecutionFailedEventAttributes()
                attr.Cause <- StartChildWorkflowExecutionFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                attr.Control <- ReadParameterStringValue "Control" parameters
                attr.WorkflowId <- ReadParameterStringValue "WorkflowId" parameters
                attr.WorkflowType <- ReadParameterWorkflowTypeValue parameters
                StartChildWorkflowExecutionResult.StartFailed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | StartChildWorkflowExecutionResult.Completed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Completed"), 
                    Parameters=
                        PSVOrNull "Result" attr.Result @
                        [
                            PWorkflowExecution attr.WorkflowExecution
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | StartChildWorkflowExecutionResult.Canceled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Canceled"), 
                    Parameters=
                        PSVOrNull "Details" attr.Details @
                        [
                            PWorkflowExecution attr.WorkflowExecution
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | StartChildWorkflowExecutionResult.Failed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Failed"), 
                    Parameters=
                        PSVOrNull "Reason" attr.Reason @
                        PSVOrNull "Details" attr.Details @
                        [
                            PWorkflowExecution attr.WorkflowExecution
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | StartChildWorkflowExecutionResult.TimedOut(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("TimedOut"), 
                    Parameters=
                        PSVOrNull "TimeoutType" attr.TimeoutType.Value @
                        [
                            PWorkflowExecution attr.WorkflowExecution
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | StartChildWorkflowExecutionResult.Terminated(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Terminated"), 
                    Parameters=
                        [
                            PWorkflowExecution attr.WorkflowExecution
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | StartChildWorkflowExecutionResult.StartFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("StartFailed"), 
                    Parameters=
                        PSVOrNull "Cause" attr.Cause.Value @
                        PSVOrNull "Control" attr.Control @
                        [
                            PSV "WorkflowId" attr.WorkflowId
                            PWorkflowType attr.WorkflowType
                        ]
                )

            | _ -> failwith "error"

    type StartTimerResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Fired"), parameters) -> 
                let attr = TimerFiredEventAttributes()
                attr.TimerId <- ReadParameterStringValue "TimerId" parameters
                StartTimerResult.Fired(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Canceled"), parameters) -> 
                let attr = TimerCanceledEventAttributes()
                attr.TimerId <- ReadParameterStringValue "TimerId" parameters
                StartTimerResult.Canceled(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("StartTimerFailed"), parameters) -> 
                let attr = StartTimerFailedEventAttributes()
                attr.Cause <- StartTimerFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                attr.TimerId <- ReadParameterStringValue "TimerId" parameters
                StartTimerResult.StartTimerFailed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | StartTimerResult.Fired(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Fired"), Parameters=[PSV "TimerId" attr.TimerId])

            | StartTimerResult.Canceled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Canceled"), Parameters=[PSV "TimerId" attr.TimerId])

            | StartTimerResult.StartTimerFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("StartTimerFailed"), 
                    Parameters=
                        [
                            PSV "TimerId" attr.TimerId
                        ] @
                        PSVOrNull "Cause" attr.Cause.Value 
                )

            | _ -> failwith "error"

    type WorkflowExecutionSignaledResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Signaled"), parameters) -> 
                let attr = WorkflowExecutionSignaledEventAttributes()
                attr.SignalName <- ReadParameterStringValue "SignalName" parameters
                attr.Input <- ReadParameterStringValue "Input" parameters
                WorkflowExecutionSignaledResult.Signaled(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | WorkflowExecutionSignaledResult.Signaled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Signaled"), 
                    Parameters=
                        [
                            PSV "SignalName" attr.SignalName
                        ] @
                        PSVOrNull "Input" attr.Input 
                )

            | _ -> failwith "error"

    type MarkerRecordedResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Recorded"), parameters) -> 
                let attr = MarkerRecordedEventAttributes()
                attr.MarkerName <- ReadParameterStringValue "MarkerName" parameters
                attr.Details <- ReadParameterStringValue "Details" parameters
                MarkerRecordedResult.Recorded(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("RecordMarkerFailed"), parameters) -> 
                let attr = RecordMarkerFailedEventAttributes()
                attr.MarkerName <- ReadParameterStringValue "MarkerName" parameters
                attr.Cause <- RecordMarkerFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                MarkerRecordedResult.RecordMarkerFailed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | MarkerRecordedResult.Recorded(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Recorded"), 
                    Parameters=
                        [
                            PSV "MarkerName" attr.MarkerName
                        ] @
                        PSVOrNull "Details" attr.Details 
                )

            | MarkerRecordedResult.RecordMarkerFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("RecordMarkerFailed"), 
                    Parameters=
                        [
                            PSV "MarkerName" attr.MarkerName
                        ] @
                        PSVOrNull "Cause" attr.Cause.Value 
                )

            | _ -> failwith "error"

    type SignalExternalWorkflowExecutionResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Signaled"), parameters) -> 
                let attr = ExternalWorkflowExecutionSignaledEventAttributes()
                attr.WorkflowExecution <- ReadParameterWorkflowExecutionValue parameters
                SignalExternalWorkflowExecutionResult.Signaled(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("Failed"), parameters) -> 
                let attr = SignalExternalWorkflowExecutionFailedEventAttributes()
                attr.Cause <- SignalExternalWorkflowExecutionFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                attr.Control <- ReadParameterStringValue "Control" parameters
                attr.RunId <- ReadParameterStringValue "RunId" parameters
                attr.WorkflowId <- ReadParameterStringValue "WorkflowId" parameters
                SignalExternalWorkflowExecutionResult.Failed(attr)

            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | SignalExternalWorkflowExecutionResult.Signaled(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Signaled"),Parameters=[PWorkflowExecution attr.WorkflowExecution])

            | SignalExternalWorkflowExecutionResult.Failed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Failed"), 
                    Parameters=
                        (PSVOrNull "Cause" attr.Cause.Value) @
                        (PSVOrNull "Control" attr.Control) @
                        [
                            PSV "RunId" attr.RunId
                            PSV "WorkflowId" attr.WorkflowId
                        ]
                )

            | _ -> failwith "error"

    type RecordMarkerResult with
        static member CreateFromExpression(result:ObjectInitialization) =
            match result with
            | ObjectInitialization.NameAndParameters(Label.Text("Recorded"), parameters) -> 
                let attr = MarkerRecordedEventAttributes()
                attr.MarkerName <- ReadParameterStringValue "MarkerName" parameters
                attr.Details <- ReadParameterStringValue "Details" parameters
                RecordMarkerResult.Recorded(attr)

            | ObjectInitialization.NameAndParameters(Label.Text("RecordMarkerFailed"), parameters) -> 
                let attr = RecordMarkerFailedEventAttributes()
                attr.MarkerName <- ReadParameterStringValue "MarkerName" parameters
                attr.Cause <- RecordMarkerFailedCause.FindValue(ReadParameterStringValue "Cause" parameters)
                RecordMarkerResult.RecordMarkerFailed(attr)
            
            | _ -> failwith "error"

        member this.GetExpression() =
            match this with
            | RecordMarkerResult.Recorded(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("Recorded"),Parameters=[PSV "MarkerName" attr.MarkerName] @ PSVOrNull "Details" attr.Details)

            | RecordMarkerResult.RecordMarkerFailed(attr) ->
                ObjectInitialization.NameAndParameters(Name=Label.Text("RecordMarkerFailed"),Parameters=[PSV "MarkerName" attr.MarkerName; PSV "Cause" attr.Cause.Value;])

            | _ -> failwith "error"