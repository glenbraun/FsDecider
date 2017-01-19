module internal FlowSharp.ContextExpression

open System
open System.Text

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
        | None -> ([], chars)
        | Some(m) -> 
            let chars = SkipWhiteSpace chars
            ParseActivityToResultMappings chars (m :: mappings)

    member this.TryParseExecutionContext (executionContext:string) : ContextExpression option =
        let (mappings, chars) = ParseActivityToResultMappings (executionContext |> List.ofSeq) []

        let chars = SkipWhiteSpace chars

        match chars with
        | [] -> Some(ContextExpression.Mappings(mappings))
        | _ -> None
    
