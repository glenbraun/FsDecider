namespace FlowSharp.UnitTests

open FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

module TestHelper =
    let PollAndDecide (taskList:TaskList) (deciderFunc: (DecisionTask -> RespondDecisionTaskCompletedRequest)) (offlineFunc: (unit -> DecisionTask)) (roundtrips: int) : (int * RespondDecisionTaskCompletedRequest) seq =
        
        // Split the offline history into separate decision tasks
        let OfflineDecisionTasks =
            if TestConfiguration.IsConnected then
                Array.empty<DecisionTask>
            else 
                offlineFunc()
                |> OfflineHistory.OfflineDecisionTaskSequence
                |> Seq.toArray

        let PollOffline (n:int) =
            OfflineDecisionTasks.[n-1]

        let PollOnline() =
            use swf = TestConfiguration.GetSwfClient()
            
            let request = PollForDecisionTaskRequest
                            (
                                Domain = TestConfiguration.TestDomain,
                                Identity = TestConfiguration.TestIdentity,
                                TaskList = taskList
                            )
            let response = swf.PollForDecisionTask(request)
            (response.DecisionTask)

        seq {
            for trip = 1 to roundtrips do
                let dt =
                    if TestConfiguration.IsConnected then
                        PollOnline()
                    else
                        PollOffline trip

                let resp = deciderFunc(dt)
                yield (trip, resp)
        }

    let RespondDecisionTaskCompleted (request:RespondDecisionTaskCompletedRequest) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let response = swf.RespondDecisionTaskCompleted(request)

            if not (response.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Decision Task Completed request failed in PollAndCompleteActivityTask"

    let StartWorkflowExecutionOnTaskList (workflow:WorkflowType) (workflowId:string) (taskList:TaskList) (input:string option) (lambdaRoleOverride: string option) (childPolicy:ChildPolicy option) = 
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let startRequest = new Amazon.SimpleWorkflow.Model.StartWorkflowExecutionRequest()
            startRequest.WorkflowId <- workflowId
            startRequest.WorkflowType <- workflow
            startRequest.TaskList <- taskList
            if input.IsSome then startRequest.Input <- input.Value

            startRequest.ChildPolicy <- if childPolicy.IsSome then childPolicy.Value else ChildPolicy.TERMINATE
            startRequest.Domain <- TestConfiguration.TestDomain
            startRequest.ExecutionStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout.ToString()
            startRequest.TaskStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout.ToString()
            startRequest.LambdaRole <- if lambdaRoleOverride.IsSome then lambdaRoleOverride.Value else TestConfiguration.TestLambdaRole

            let startResponse = swf.StartWorkflowExecution(startRequest)

            if not (startResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Start Workflow request failed in PollAndCompleteActivityTask"
        
            // Return RunId
            startResponse.Run.RunId
        else
            "Offline RunId"

    let  PollAndCompleteActivityTask (activity:ActivityType) (result:string option) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let pollRequest = new PollForActivityTaskRequest();
            pollRequest.Domain <- TestConfiguration.TestDomain
            pollRequest.Identity <- TestConfiguration.TestIdentity
            pollRequest.TaskList <- TestConfiguration.TestTaskList

            let pollResponse = swf.PollForActivityTask(pollRequest)

            if not (pollResponse.ActivityTask.ActivityType <> null && pollResponse.ActivityTask.ActivityType.Name = activity.Name && pollResponse.ActivityTask.ActivityType.Version = activity.Version) then
                failwith "Expected different activity in PollAndCompleteActivityTask"

            let completedRequest = new RespondActivityTaskCompletedRequest();
            completedRequest.TaskToken <- pollResponse.ActivityTask.TaskToken
            if result.IsSome then completedRequest.Result <- result.Value

            let completedResponse = swf.RespondActivityTaskCompleted(completedRequest)
            if not (completedResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Completed request failed in PollAndCompleteActivityTask"

    let  PollAndCancelActivityTask (activity:ActivityType) (details:string option) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let pollRequest = new PollForActivityTaskRequest();
            pollRequest.Domain <- TestConfiguration.TestDomain
            pollRequest.Identity <- TestConfiguration.TestIdentity
            pollRequest.TaskList <- TestConfiguration.TestTaskList

            let pollResponse = swf.PollForActivityTask(pollRequest)

            if not (pollResponse.ActivityTask.ActivityType <> null && pollResponse.ActivityTask.ActivityType.Name = activity.Name && pollResponse.ActivityTask.ActivityType.Version = activity.Version) then
                failwith "Expected different activity in PollAndCancelActivityTask"

            let canceledRequest = new RespondActivityTaskCanceledRequest();
            canceledRequest.TaskToken <- pollResponse.ActivityTask.TaskToken
            if details.IsSome then canceledRequest.Details <- details.Value

            let canceledResponse = swf.RespondActivityTaskCanceled(canceledRequest)
            if not (canceledResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Response failed in PollAndCompleteActivityTask"

    let  PollAndFailActivityTask (activity:ActivityType) (reason: string option) (details:string option) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let pollRequest = new PollForActivityTaskRequest();
            pollRequest.Domain <- TestConfiguration.TestDomain
            pollRequest.Identity <- TestConfiguration.TestIdentity
            pollRequest.TaskList <- TestConfiguration.TestTaskList

            let pollResponse = swf.PollForActivityTask(pollRequest)

            if not (pollResponse.ActivityTask.ActivityType <> null && pollResponse.ActivityTask.ActivityType.Name = activity.Name && pollResponse.ActivityTask.ActivityType.Version = activity.Version) then
                failwith "Expected different activity in PollAndCancelActivityTask"

            let failRequest = new RespondActivityTaskFailedRequest();
            failRequest.TaskToken <- pollResponse.ActivityTask.TaskToken
            if reason.IsSome then failRequest.Reason <- reason.Value
            if details.IsSome then failRequest.Details <- details.Value

            let failedResponse = swf.RespondActivityTaskFailed(failRequest)
            if not (failedResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Response failed in PollAndFailActivityTask"

    let PollAndStartActivityTask (activity:ActivityType) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let pollRequest = new PollForActivityTaskRequest()
            pollRequest.Domain <- TestConfiguration.TestDomain
            pollRequest.Identity <- TestConfiguration.TestIdentity
            pollRequest.TaskList <- TestConfiguration.TestTaskList

            let pollResponse = swf.PollForActivityTask(pollRequest)
            if not (pollResponse.ActivityTask.ActivityType <> null && pollResponse.ActivityTask.ActivityType.Name = activity.Name && pollResponse.ActivityTask.ActivityType.Version = activity.Version) then
                failwith "Expected different activity in PollAndCompleteActivityTask"
        
            pollResponse.ActivityTask.TaskToken
        else
            "Offline TaskToken"

    let HeartbeatAndCancelActivityTask (taskToken:string) (details:string)=
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            // Send a heartbeat to check for cancel
            let heartbeatRequest = new RecordActivityTaskHeartbeatRequest()
            heartbeatRequest.TaskToken <- taskToken

            let heartbeatResponse = swf.RecordActivityTaskHeartbeat(heartbeatRequest)
            match heartbeatResponse.ActivityTaskStatus.CancelRequested with
            | true ->
                let canceledRequest = new Amazon.SimpleWorkflow.Model.RespondActivityTaskCanceledRequest()
                canceledRequest.TaskToken <- taskToken
                canceledRequest.Details <- details

                let canceledResponse = swf.RespondActivityTaskCanceled(canceledRequest)
                if not (canceledResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                    failwith "Canceled request failed in HeartbeatAndCancelActivityTask"
            | false -> failwith "HeartbeatAndCancelActivityTask failed, expected canceled in heartbeat."


    let SignalWorkflow (runId:string) (workflowId:string) (signalName:string) (input:string) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let signalRequest = new Amazon.SimpleWorkflow.Model.SignalWorkflowExecutionRequest();
            signalRequest.Domain <- TestConfiguration.TestDomain
            signalRequest.RunId <- runId
            signalRequest.SignalName <- signalName
            signalRequest.WorkflowId <- workflowId
            signalRequest.Input <- input        

            let signalResponse = swf.SignalWorkflowExecution(signalRequest)
            if not (signalResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Signal Workflow request failed in SignalWorkflow"


    let TerminateWorkflow (runId:string) (workflowId:string) (reason:string) (details:string) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let terminateRequest = new TerminateWorkflowExecutionRequest();
            terminateRequest.ChildPolicy <- ChildPolicy.TERMINATE
            terminateRequest.Details <- details
            terminateRequest.Domain <- TestConfiguration.TestDomain
            terminateRequest.Reason <- reason
            terminateRequest.RunId <- runId
            terminateRequest.WorkflowId <- workflowId

            let terminateResponse = swf.TerminateWorkflowExecution(terminateRequest)
            if not (terminateResponse.HttpStatusCode = System.Net.HttpStatusCode.OK) then
                failwith "Terminate Workflow request failed in SignalWorkflow"

    let GetNewExecutionRunId (runId:string) (workflowId:string) =
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let request = GetWorkflowExecutionHistoryRequest(
                            Domain=TestConfiguration.TestDomain,
                            Execution=WorkflowExecution(RunId=runId, WorkflowId=workflowId)
                          )

            let response = swf.GetWorkflowExecutionHistory(request)
            
            let hev =
                response.History.Events
                |> Seq.tryFindBack( fun h -> h.EventType = EventType.WorkflowExecutionContinuedAsNew )

            match hev with
            | Some(h) -> h.WorkflowExecutionContinuedAsNewEventAttributes.NewExecutionRunId
            | None -> failwith "Failed getting Continue As New RunId"

        else 
            "Offline Continued As New RunId"

    let GenerateOfflineDecisionTaskCodeSnippet (runId:string) (workflowId:string) (subs:Map<string, string>) =
        // Generate Offline History

        if TestConfiguration.GenerateOfflineHistory then
            if TestConfiguration.IsConnected then
                use swf = TestConfiguration.GetSwfClient()

                let request = new GetWorkflowExecutionHistoryRequest
                                (
                                    Domain = TestConfiguration.TestDomain,
                                    Execution = new WorkflowExecution(RunId=runId, WorkflowId=workflowId)
                                )

                let response = swf.GetWorkflowExecutionHistory(request)

                use sw = new System.IO.StringWriter()
                OfflineHistory.GenerateOfflineDecisionTaskCodeSnippet sw (response.History.Events) (Some(subs))
                sw.Close()
                        
                System.Diagnostics.Debug.WriteLine(sw.ToString())
            else
                System.Diagnostics.Debug.WriteLine("GenerateOfflineDecisionTaskCodeSnippet skipped because TestConfiguration.IsConnected is false.")

