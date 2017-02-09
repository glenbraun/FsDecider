namespace FlowSharp

open System
open System.Diagnostics

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions
open FlowSharp.HistoryWalker
open FlowSharp.EventPatterns
open FlowSharp.ExecutionContext

module Trace =

    type TraceWriter() =
        static member private GetUnionCaseName (x:obj) t = 
            match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, t) with
            | case, _ -> case.Name

        static member val public WriteWorkflowExecution = 
            fun (w:WorkflowExecution) -> 
                match w with
                | null  -> "null"
                | _     -> sprintf """WorkflowExecution(WorkflowId="%s", RunId="%s")""" w.WorkflowId w.RunId
            with get, set

        static member val public WriteWorkflowType = 
            fun (w:WorkflowType) -> 
                match w with
                | null  -> "null"
                | _     -> sprintf """WorkflowType(Name="%s", Version="%s")""" w.Name w.Version
            with get, set

        static member val public WriteActivityType = 
            fun (at:ActivityType) ->
                sprintf """ActivityType(Name="%s", Version="%s")""" at.Name at.Version
            with get, set

        static member val public WriteTaskList = 
            fun (tl:TaskList) ->
                sprintf """TaskList(Name="%s")""" tl.Name
            with get, set

        static member val public WriteHeader = 
            fun (decisionTask:DecisionTask) ->
                TraceWriter.WriteWorkflowExecution decisionTask.WorkflowExecution
            with get, set

        static member val public WriteScheduleActivityTaskAction =
            fun (action:ScheduleActivityTaskAction) ->
                let attr = action.GetAttributes()
                sprintf """ScheduleActivityTaskAction(ActivityId="%s", ActivityType=%s)"""
                    (attr.ActivityId)
                    (TraceWriter.WriteActivityType attr.ActivityType)

        static member val public WriteRequestCancelActivityTaskAction =
            fun (RequestCancelActivityTaskAction.ScheduleResult(result)) ->
                sprintf """RequestCancelActivityTaskAction(ScheduleResult.%s)""" (TraceWriter.GetUnionCaseName result typeof<ScheduleActivityTaskResult>)

        static member val public WriteStartChildWorkflowExecutionAction =
            fun (action:StartChildWorkflowExecutionAction) ->
                let attr = action.GetAttributes()
                sprintf """StartChildWorkflowExecutionAction(WorkflowId="%s", WorkflowType=%s)"""
                    (attr.WorkflowId)
                    (TraceWriter.WriteWorkflowType attr.WorkflowType)

        static member val public WriteRequestCancelExternalWorkflowExecutionAction =
            fun (RequestCancelExternalWorkflowExecutionAction.Attributes(attr)) ->
                sprintf """RequestCancelExternalWorkflowExecutionAction(RunId="%s", WorkflowId="%s")"""
                    (attr.RunId)
                    (attr.WorkflowId)

        static member val public WriteScheduleLambdaFunctionAction =
            fun (action:ScheduleLambdaFunctionAction) ->
                let attr = action.GetAttributes()
                sprintf """ScheduleLambdaFunctionAction(Id="%s", Name=%s)"""
                    (attr.Id)
                    (attr.Name)

        static member val public WriteStartTimerAction =
            fun (action:StartTimerAction) ->
                let attr = action.GetAttributes()
                sprintf """StartTimerAction(TimerId="%s")"""
                    (attr.TimerId)

        static member val public WriteWaitForTimerAction =
            fun (WaitForTimerAction.StartResult(result)) ->
                sprintf """WaitForTimerAction(StartResult.%s)""" (TraceWriter.GetUnionCaseName result typeof<StartTimerResult>)

        static member val public WriteCancelTimerAction =
            fun (CancelTimerAction.StartResult(result)) ->
                sprintf """CancelTimerAction(StartResult.%s)""" (TraceWriter.GetUnionCaseName result typeof<StartTimerResult>)

        static member val public WriteMarkerRecordedAction =
            fun (action:MarkerRecordedAction) ->
                let markerName = action.GetAttributes()
                sprintf """MarkerRecordedAction(MarkerName="%s")"""
                    (markerName)

        static member val public WriteRecordMarkerAction =
            fun (action:RecordMarkerAction) ->
                let attr = action.GetAttributes()
                sprintf """RecordMarkerAction(MarkerName="%s")"""
                    (attr.MarkerName)

        static member val public WriteSignalExternalWorkflowExecutionAction =
            fun (action:SignalExternalWorkflowExecutionAction) ->
                let attr = action.GetAttributes()
                sprintf """SignalExternalWorkflowExecutionAction(SignalName="%s", WorkflowId="%s", RunId="%s")"""
                    (attr.SignalName)
                    (attr.WorkflowId)
                    (attr.RunId)

        static member val public WriteWorkflowExecutionSignaledAction =
            fun (action:WorkflowExecutionSignaledAction) ->
                let signalName = action.GetAttributes()
                sprintf """WorkflowExecutionSignaledAction(SignalName="%s")"""
                    (signalName)

        static member val public WriteWaitForWorkflowExecutionSignaledAction =
            fun (action:WaitForWorkflowExecutionSignaledAction) ->
                let signalName = action.GetAttributes()
                sprintf """WaitForWorkflowExecutionSignaledAction(SignalName="%s")"""
                    (signalName)

    // write some/none
    let internal wsn (o:Option<_>) =
        match o with
        | Some(_) -> "Some"
        | None -> "None" 

    // write null or not
    let internal wn o =
        if o = null then "Null" else "Not Null"

    let TraceSource = new TraceSource("FlowSharp", SourceLevels.All)

    let WorkflowExecutionStarted workflowType workflowId tasklist input runid =
        let ti = sprintf """
            Workflow Execution Started
                    WorkflowType                       %s
                    WorkflowId                         %s
                    TaskList                           %s
                    Input                              %s
                    RunId                              %s"""
                    (TraceWriter.WriteWorkflowType workflowType)
                    workflowId
                    (TraceWriter.WriteTaskList tasklist)
                    (wsn input)
                    runid
        TraceSource.TraceEvent(TraceEventType.Information, 1, ti)

    let ActivityCompleted activityType result tasklist =
        let ti = sprintf """
            Activity Completed
                    ActivityType                       %s
                    Result                             %s
                    TaskList                           %s"""
                    (TraceWriter.WriteActivityType activityType)
                    result
                    (TraceWriter.WriteTaskList tasklist)
        TraceSource.TraceEvent(TraceEventType.Information, 2, ti)
        
    let BuilderCreated (decisionTask:DecisionTask) (reverseOrder:bool) (contextManager:IContextManager option) =        
        let ti = sprintf """%s
            Builder Created
                    DecisionTask
                        .Events Count                  %i
                        .StartedEventId                %i
                        .PreviousStartedEventId        %i
                        .WorkflowExecution             %s
                        .WorkflowType                  %s 
                    ReverseOrder                       %b
                    ContextManager                     %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    decisionTask.Events.Count
                    decisionTask.StartedEventId
                    decisionTask.PreviousStartedEventId
                    (TraceWriter.WriteWorkflowExecution decisionTask.WorkflowExecution)
                    (TraceWriter.WriteWorkflowType decisionTask.WorkflowType)
                    reverseOrder
                    (wsn contextManager)
        
        TraceSource.TraceEvent(TraceEventType.Information, 3, ti)

    let BuilderDelay decisionTask =
        let ti = sprintf """%s
            Builder Delay()
                    DecisionTask.TaskToken             %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    (wn decisionTask.TaskToken)
        TraceSource.TraceEvent(TraceEventType.Information, 4, ti)

    let BuilderRun decisionTask = 
        let ti = sprintf """%s
            Builder Run()""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 5, ti)

    let BuilderZero decisionTask =
        let ti = sprintf """%s
            Builder Zero()""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 6, ti)

    let rec DecisionList (response:RespondDecisionTaskCompletedRequest) i (s:string) = 
        if i = response.Decisions.Count 
        then s
        else
            DecisionList response (i+1) ((if s.Length > 0 then s + "," else s) + response.Decisions.[i].DecisionType.ToString())

    let BuilderReturn decisionTask (result:ReturnResult) (response:RespondDecisionTaskCompletedRequest) (ev:HistoryEvent option) (et:EventType) =
        let ti = sprintf """%s
            Builder Return
                    Result                             %A
                    FailedEvent                        %s
                    FailedEventType                    %s
                    Decisions Count                    %i
                    Decisions Type List                %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    result
                    (wsn ev)
                    (if ev.IsSome then et.ToString() else "N/A")
                    (response.Decisions.Count)
                    (DecisionList response 0 "")
        TraceSource.TraceEvent(TraceEventType.Information, 7, ti)

    let BuilderWait decisionTask (response:RespondDecisionTaskCompletedRequest) =
        let ti = sprintf """%s
            Builder Wait
                    Decisions Count                    %i
                    Decisions Type List                %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    (response.Decisions.Count)
                    (DecisionList response 0 "")
        TraceSource.TraceEvent(TraceEventType.Information, 8, ti)

    let BuilderBindScheduleActivityTaskAction decisionTask (action:ScheduleActivityTaskAction) (result:ScheduleActivityTaskResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of ScheduleActivityTaskAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteScheduleActivityTaskAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 9, ti)

    let BuilderBindWaitForActivityTaskAction decisionTask (result:ScheduleActivityTaskResult) =
        let ti = sprintf """%s
            Builder Bind of WaitForActivityTaskAction
                    Is Finished                        %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (result.IsFinished())
        TraceSource.TraceEvent(TraceEventType.Information, 10, ti)

    let BuilderBindWaitForAnyActivityTaskAction decisionTask (results:ScheduleActivityTaskResult list) (anyFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAnyActivityTaskAction
                    Result List Length                 %i
                    Any Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    anyFinished
        TraceSource.TraceEvent(TraceEventType.Information, 11, ti)

    let BuilderBindWaitForAllActivityTaskAction decisionTask (results:ScheduleActivityTaskResult list) (allFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAllActivityTaskAction
                    Result List Length                 %i
                    All Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    allFinished
        TraceSource.TraceEvent(TraceEventType.Information, 12, ti)

    let BuilderBindRequestCancelActivityTaskAction decisionTask (action:RequestCancelActivityTaskAction) (result:RequestCancelActivityTaskResult option) = 
        let ti = sprintf """%s
            Builder Bind of RequestCancelActivityTaskAction
                    Action                             %s
                    Result                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteRequestCancelActivityTaskAction action)
                    result
        TraceSource.TraceEvent(TraceEventType.Information, 13, ti)

    let BuilderBindStartChildWorkflowExecutionAction decisionTask (action:StartChildWorkflowExecutionAction) (result:StartChildWorkflowExecutionResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of StartChildWorkflowExecutionAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteStartChildWorkflowExecutionAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 14, ti)

    let BuilderBindWaitForChildWorkflowExecutionAction decisionTask (result:StartChildWorkflowExecutionResult) =
        let ti = sprintf """%s
            Builder Bind of WaitForChildWorkflowExecutionAction
                    Is Finished                        %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (result.IsFinished())
        TraceSource.TraceEvent(TraceEventType.Information, 15, ti)

    let BuilderBindWaitForAnyChildWorkflowExecutionAction decisionTask (results:StartChildWorkflowExecutionResult list) (anyFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAnyChildWorkflowExecutionAction
                    Result List Length                 %i
                    Any Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    anyFinished
        TraceSource.TraceEvent(TraceEventType.Information, 16, ti)

    let BuilderBindWaitForAllChildWorkflowExecutionAction decisionTask (results:StartChildWorkflowExecutionResult list) (allFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAllChildWorkflowExecutionAction
                    Result List Length                 %i
                    Any Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    allFinished
        TraceSource.TraceEvent(TraceEventType.Information, 17, ti)

    let BuilderBindRequestCancelExternalWorkflowExecutionAction decisionTask (action:RequestCancelExternalWorkflowExecutionAction) (result:RequestCancelExternalWorkflowExecutionResult) =
        let ti = sprintf """%s
            Builder Bind of RequestCancelExternalWorkflowExecutionAction
                    Action                             %s
                    Result                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteRequestCancelExternalWorkflowExecutionAction action)
                    result
        TraceSource.TraceEvent(TraceEventType.Information, 18, ti)

    let BuilderBindScheduleLambdaFunctionAction decisionTask (action:ScheduleLambdaFunctionAction) (result:ScheduleLambdaFunctionResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of ScheduleLambdaFunctionAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteScheduleLambdaFunctionAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 19, ti)

    let BuilderBindWaitForLambdaFunctionAction decisionTask (result:ScheduleLambdaFunctionResult) =
        let ti = sprintf """%s
            Builder Bind of WaitForLambdaFunctionAction
                    Is Finished                        %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (result.IsFinished())
        TraceSource.TraceEvent(TraceEventType.Information, 20, ti)

    let BuilderBindWaitForAnyLambdaFunctionAction decisionTask (results:ScheduleLambdaFunctionResult list) (anyFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAnyLambdaFunctionAction
                    Result List Length                 %i
                    Any Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    anyFinished
        TraceSource.TraceEvent(TraceEventType.Information, 21, ti)

    let BuilderBindWaitForAllLambdaFunctionAction decisionTask (results:ScheduleLambdaFunctionResult list) (allFinished:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForAllLambdaFunctionAction
                    Result List Length                 %i
                    Any Finished                       %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (List.length results)
                    allFinished
        TraceSource.TraceEvent(TraceEventType.Information, 22, ti)

    let BuilderBindStartTimerAction decisionTask (action:StartTimerAction) (result:StartTimerResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of StartTimerAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteStartTimerAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 23, ti)

    let BuilderBindWaitForTimerAction decisionTask (action:WaitForTimerAction) =
        let ti = sprintf """%s
            Builder Bind of WaitForTimerAction
                    Action                             %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteWaitForTimerAction action)
        TraceSource.TraceEvent(TraceEventType.Information, 24, ti)

    let BuilderBindCancelTimerAction decisionTask (action:CancelTimerAction) (result:CancelTimerResult option) =
        let ti = sprintf """%s
            Builder Bind of CancelTimerAction
                    Action                             %s
                    Result                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteCancelTimerAction action)
                    result
        TraceSource.TraceEvent(TraceEventType.Information, 25, ti)

    let BuilderBindMarkerRecordedAction decisionTask (action:MarkerRecordedAction) (result:MarkerRecordedResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of MarkerRecordedAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteMarkerRecordedAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 26, ti)

    let BuilderBindRecordMarkerAction decisionTask (action:RecordMarkerAction) (result:RecordMarkerResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of RecordMarkerAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteRecordMarkerAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 27, ti)

    let BuilderBindSignalExternalWorkflowExecutionAction decisionTask (action:SignalExternalWorkflowExecutionAction) (result:SignalExternalWorkflowExecutionResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of SignalExternalWorkflowExecutionAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteSignalExternalWorkflowExecutionAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 28, ti)

    let BuilderBindWorkflowExecutionSignaledAction decisionTask (action:WorkflowExecutionSignaledAction) (result:WorkflowExecutionSignaledResult) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of WorkflowExecutionSignaledAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteWorkflowExecutionSignaledAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 29, ti)

    let BuilderBindWaitForWorkflowExecutionSignaledAction decisionTask (action:WaitForWorkflowExecutionSignaledAction) (result:WorkflowExecutionSignaledResult option) (fromcontext:bool) =
        let ti = sprintf """%s
            Builder Bind of WaitForWorkflowExecutionSignaledAction
                    Action                             %s
                    Result                             %A
                    Result From Context                %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    (TraceWriter.WriteWaitForWorkflowExecutionSignaledAction action)
                    result
                    fromcontext
        TraceSource.TraceEvent(TraceEventType.Information, 30, ti)

    let BuilderBindWorkflowExecutionCancelRequestedAction decisionTask (action:WorkflowExecutionCancelRequestedAction) (result:WorkflowExecutionCancelRequestedResult) =
        let ti = sprintf """%s
            Builder Bind of WorkflowExecutionCancelRequestedAction
                    Action                             %A
                    Result                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    action
                    result
        TraceSource.TraceEvent(TraceEventType.Information, 31, ti)

    let BuilderBindGetWorkflowExecutionInputAction decisionTask (action:GetWorkflowExecutionInputAction) (input:string) =
        let ti = sprintf """%s
            Builder Bind of GetWorkflowExecutionInputAction
                    Action                             %A
                    Input                              %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    action
                    (wn input)
        TraceSource.TraceEvent(TraceEventType.Information, 32, ti)

    let BuilderBindGetExecutionContextAction decisionTask (action:GetExecutionContextAction) (context:string) =
        let ti = sprintf """%s
            Builder Bind of GetExecutionContextAction
                    Action                             %A
                    ExecutionContext                   %s"""
                    (TraceWriter.WriteHeader decisionTask)
                    action
                    (wn context)
        TraceSource.TraceEvent(TraceEventType.Information, 33, ti)

    let BuilderBindSetExecutionContextAction decisionTask (action:SetExecutionContextAction) =
        let ti = sprintf """%s
            Builder Bind of SetExecutionContextAction
                    Action                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    action
        TraceSource.TraceEvent(TraceEventType.Information, 34, ti)

    let BuilderBindRemoveFromContextAction decisionTask (action:RemoveFromContextAction) =
        let ti = sprintf """%s
            Builder Bind of RemoveFromContextAction
                    Action                             %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    action
        TraceSource.TraceEvent(TraceEventType.Information, 35, ti)

    let BuilderForLoop decisionTask = 
        let ti = sprintf """%s
            Builder 'for' loop""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 36, ti)

    let BuilderForLoopIteration decisionTask blockFlag = 
        let ti = sprintf """%s
            Builder 'for' loop iteration
                    BlockFlag                          %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    blockFlag
        TraceSource.TraceEvent(TraceEventType.Information, 37, ti)

    let BuilderWhileLoop decisionTask = 
        let ti = sprintf """%s
            Builder 'while' loop""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 38, ti)

    let BuilderWhileLoopIteration decisionTask blockFlag = 
        let ti = sprintf """%s
            Builder 'while' loop iteration
                    BlockFlag                          %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    blockFlag
        TraceSource.TraceEvent(TraceEventType.Information, 39, ti)

    let BuilderCombine decisionTask blockFlag = 
        let ti = sprintf """%s
            Builder Combine
                    BlockFlag                          %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    blockFlag
        TraceSource.TraceEvent(TraceEventType.Information, 40, ti)

    let BuilderTryFinallyTry decisionTask = 
        let ti = sprintf """%s
            Builder 'try/finally' (try)""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 41, ti)

    let BuilderTryFinallyFinally decisionTask blockFlag = 
        let ti = sprintf """%s
            Builder 'try/finally' (finally) iteration
                    BlockFlag                          %b"""
                    (TraceWriter.WriteHeader decisionTask)
                    blockFlag
        TraceSource.TraceEvent(TraceEventType.Information, 42, ti)

    let BuilderTryWithTry decisionTask = 
        let ti = sprintf """%s
            Builder 'try/with' (try)""" (TraceWriter.WriteHeader decisionTask)
        TraceSource.TraceEvent(TraceEventType.Information, 43, ti)

    let BuilderTryWithWith decisionTask e = 
        let ti = sprintf """%s
            Builder 'try/with' (with) iteration
                    Exception                          %A"""
                    (TraceWriter.WriteHeader decisionTask)
                    e
        TraceSource.TraceEvent(TraceEventType.Information, 44, ti)

    let History (we:WorkflowExecution) (history:History) = 
        let rec Events i (s:string) =
            if i = history.Events.Count then
                s
            else
                let ns = 
                    sprintf """%s
                        %i      %s"""
                        s
                        (history.Events.[i].EventId)
                        (history.Events.[i].EventType.ToString())

                Events (i+1) ns
        
        let ti = sprintf """History for %s
                    %s"""
                    (TraceWriter.WriteWorkflowExecution we)
                    (Events 0 "")
        TraceSource.TraceEvent(TraceEventType.Information, 45, ti)
            
    let TraceException (ex:'T when 'T :> Exception) =
        TraceSource.TraceEvent(TraceEventType.Error, 46, ex.Message)

    let TraceInformation (message:string) =
        TraceSource.TraceEvent(TraceEventType.Information, 47, message)

