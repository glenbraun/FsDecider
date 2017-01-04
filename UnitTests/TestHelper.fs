namespace FlowSharp.UnitTests

open FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

module TestHelper =
    let PollAndDecide (deciderFunc: (DecisionTask -> RespondDecisionTaskCompletedRequest)) (offlineFunc: (unit -> DecisionTask)) (roundtrips: int) : (int * RespondDecisionTaskCompletedRequest) seq =
        
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
                                TaskList = TestConfiguration.TestTaskList
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
        else
            // nothing to do when offline
            ()

    let StartWorkflowExecutionOnTaskList (workflow:WorkflowType) (workflowId:string) (taskList:TaskList) (input:string option) = 
        if TestConfiguration.IsConnected then
            use swf = TestConfiguration.GetSwfClient()

            let startRequest = new Amazon.SimpleWorkflow.Model.StartWorkflowExecutionRequest()
            startRequest.WorkflowId <- workflowId
            startRequest.WorkflowType <- workflow
            startRequest.TaskList <- taskList
            if input.IsSome then startRequest.Input <- input.Value

            startRequest.ChildPolicy <- ChildPolicy.TERMINATE
            startRequest.Domain <- TestConfiguration.TestDomain
            startRequest.ExecutionStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout.ToString()
            startRequest.TaskStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout.ToString()
            startRequest.LambdaRole <- TestConfiguration.TestLambdaRole

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
        else 
            // nothing to do when offline
            ()

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

