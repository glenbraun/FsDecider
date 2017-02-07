module FlowSharp.Examples.CommandInterpreter

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.UnitTests

type Command =
    | StartWorkflow of string
    | DecisionTask of string
    | ActivityTask of string
    | SignalWorkflow of string
    | Error of string
    | Quit
    | History

type Operation =
    | StartWorkflowExecution of workflow:WorkflowType * 
                      workflowId:string * 
                      taskList:TaskList option * 
                      input:string option
    | DecisionTask of decider:(DecisionTask -> RespondDecisionTaskCompletedRequest) * 
                      TaskList option

    | ActivityTask of ActivityType *
                      (ActivityTask -> string) option *
                      TaskList option
    | History
    
let mutable internal OperationMap = Map.empty<Command, Operation>
let mutable internal CurrentWorkflowExecution : WorkflowExecution = null

let public AddOperation cmd op =
    OperationMap <- Map.add cmd op OperationMap
    
let rec internal SkipWhiteSpace (chars:char list) : char list =
    match chars with
    | c :: t when Char.IsWhiteSpace(c) -> SkipWhiteSpace t
    | _ -> chars

let rec ParseArgumentValue chars value =
    let chars = SkipWhiteSpace chars

    match chars with
    | [] -> (String(value |> List.rev |> List.toArray), [])
    | c :: t when Char.IsWhiteSpace(c) -> (String(value |> List.rev |> List.toArray), t)
    | c :: t -> ParseArgumentValue t (c::value)
    
let internal ParseCommand chars =
    let chars = SkipWhiteSpace chars

    match chars with
    | 's' :: 'w' :: s :: t when Char.IsWhiteSpace(s) -> 
        let (arg, chars) = ParseArgumentValue t []
        let chars = SkipWhiteSpace chars

        match (arg, chars) with
        | (a, []) when a.Length > 0 -> Command.StartWorkflow( a )
        | _ -> Command.Error("Expected 'sw {argument}'")

    | 'd' :: 't' :: s :: t when Char.IsWhiteSpace(s) -> 
        let (arg, chars) = ParseArgumentValue t []
        let chars = SkipWhiteSpace chars

        match (arg, chars) with
        | (a, []) when a.Length > 0 -> Command.DecisionTask( a )
        | _ -> Command.Error("Expected 'dt {argument}'")

    | 'a' :: 't' :: s :: t when Char.IsWhiteSpace(s) -> 
        let (arg, chars) = ParseArgumentValue t []
        let chars = SkipWhiteSpace chars

        match (arg, chars) with
        | (a, []) when a.Length > 0 -> Command.ActivityTask( a )
        | _ -> Command.Error("Expected 'at {argument}'")

    | 'h' :: t ->
        let chars = SkipWhiteSpace t
        match chars with
        | [] -> Command.History
        | _ -> Command.Error("Expected 'h'")

    | 'q' :: t ->
        let chars = SkipWhiteSpace t
        match chars with
        | [] -> Command.Quit
        | _ -> Command.Error("Expected 'q'")

    | _ -> Command.Error("Unrecognized command")

let internal ExecuteOperation op =
    use swf = TestConfiguration.GetSwfClient()
    
    match op with
    | Operation.StartWorkflowExecution(workflowType, workflowId, tasklist, input) ->
        let startRequest = StartWorkflowExecutionRequest()
        startRequest.ChildPolicy <- ChildPolicy.TERMINATE
        startRequest.Domain <- TestConfiguration.Domain
        startRequest.ExecutionStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        startRequest.Input <- if input.IsSome then input.Value else null
        startRequest.TaskList <- if tasklist.IsSome then tasklist.Value else TestConfiguration.TaskList
        startRequest.TaskStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        startRequest.WorkflowId <- workflowId
        startRequest.WorkflowType <- workflowType

        let startResponse = swf.StartWorkflowExecution(startRequest)
        if startResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
            failwith "Error while starting workflow execution."

        CurrentWorkflowExecution <- WorkflowExecution(WorkflowId=workflowId, RunId=startResponse.Run.RunId)

        FlowSharp.Trace.WorkflowExecutionStarted workflowType workflowId (startRequest.TaskList) input (startResponse.Run.RunId) 

    | Operation.DecisionTask(decider, tasklist) ->
        let pollRequest = PollForDecisionTaskRequest()
        pollRequest.Domain <- TestConfiguration.Domain
        pollRequest.Identity <- TestConfiguration.Identity
        pollRequest.TaskList <- if tasklist.IsSome then tasklist.Value else TestConfiguration.TaskList
        
        let pollResponse = swf.PollForDecisionTask(pollRequest)
        if pollResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
            failwith "Error while retrieving decision task."

        let respondRequest = decider(pollResponse.DecisionTask)
        let respondResponse = swf.RespondDecisionTaskCompleted(respondRequest)

        if respondResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
            failwith "Error while responding with decisions."
    
    | Operation.ActivityTask(activityType, resultFunction, tasklist) ->
        let pollRequest = PollForActivityTaskRequest()
        pollRequest.Domain <- TestConfiguration.Domain
        pollRequest.Identity <- TestConfiguration.Identity
        pollRequest.TaskList <- if tasklist.IsSome then tasklist.Value else TestConfiguration.TaskList
        
        let pollResponse = swf.PollForActivityTask(pollRequest)
        if pollResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
            failwith "Error while polling for activity task."

        let respondRequest = RespondActivityTaskCompletedRequest()
        respondRequest.Result <- if resultFunction.IsSome then resultFunction.Value(pollResponse.ActivityTask) else "OK"
        respondRequest.TaskToken <- pollResponse.ActivityTask.TaskToken
        
        let respondResponse = swf.RespondActivityTaskCompleted(respondRequest)
        if respondResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
            failwith "Error while responding activity task completed."
      
        FlowSharp.Trace.ActivityCompleted activityType (respondRequest.Result) (pollRequest.TaskList)

    | Operation.History ->
        if CurrentWorkflowExecution = null then
            System.Diagnostics.Trace.TraceInformation("Unable to get history. No workflow has been started yet.")
        else
            let request = GetWorkflowExecutionHistoryRequest()
            request.Domain <- TestConfiguration.Domain
            request.Execution <- CurrentWorkflowExecution
        
            let response = swf.GetWorkflowExecutionHistory(request)

            if response.HttpStatusCode <> System.Net.HttpStatusCode.OK then
                failwith "Error while getting workflow execution history."

            FlowSharp.Trace.History CurrentWorkflowExecution (response.History)

let rec public Loop() =
    Console.WriteLine("------------------------------------------------")
    Console.WriteLine("Enter command.")
    let s = Console.ReadLine()
    let command = ParseCommand(s |> List.ofSeq)

    printfn "%A" command

    match command with
    | Command.Quit -> ()
    | Command.History -> 
        ExecuteOperation (Operation.History)
        Loop()
    | Command.Error(_) -> Loop()
    | cmd -> 
        match (Map.tryFind cmd OperationMap) with
        | Some(op) ->
            ExecuteOperation op

        | None ->
            printfn "Error, no matching operation for command."

        Loop()
