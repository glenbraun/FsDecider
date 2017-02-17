namespace FsDecider

open System
open System.Threading

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

module AsyncDeciderActor =
    exception AsyncDeciderActorException of PollForDecisionTaskResponse * string

    let CreatePollForDecisionTaskAsync (swf:(unit -> IAmazonSimpleWorkflow), pollRequest:PollForDecisionTaskRequest, filterByPreviousStartedEventId:bool) =
        let Between a b x =
            (a <= x && x <= b) || (b <= x && x <= a)

        async {
            let! cancelToken = Async.CancellationToken
            use swf = swf()

            let morepages = ref true
            let (decisionTask:DecisionTask ref) = ref null
            let events = ResizeArray<HistoryEvent>()
            
            while !morepages && not(cancelToken.IsCancellationRequested) do
                let! pollResponse = Async.AwaitTask(swf.PollForDecisionTaskAsync(pollRequest, cancelToken))
                if pollResponse.HttpStatusCode <> System.Net.HttpStatusCode.OK then
                    raise (AsyncDeciderActorException(pollResponse, "PollForDecisionTaskAsync resulted in an HTTP status other than OK."))

                decisionTask := pollResponse.DecisionTask

                if (!decisionTask).TaskToken = null then
                    morepages := false
                else
                    let filter (hev:HistoryEvent) =
                        if filterByPreviousStartedEventId then
                            Between ((!decisionTask).PreviousStartedEventId) ((!decisionTask).StartedEventId) (hev.EventId)
                        else
                            true

                    if (!decisionTask).NextPageToken = null then
                        events.AddRange ((!decisionTask).Events |> Seq.filter filter)
                        (!decisionTask).Events <- events
                            
                        morepages := false
                    else
                        pollRequest.NextPageToken <- pollResponse.DecisionTask.NextPageToken
                        if filterByPreviousStartedEventId then
                            if (!decisionTask).Events.Count > 0 then
                                let a = (!decisionTask).PreviousStartedEventId
                                let b = (!decisionTask).StartedEventId
                                        
                                if Between a b ((!decisionTask).Events.[0].EventId) || 
                                    Between a b ((!decisionTask).Events.[(!decisionTask).Events.Count-1].EventId) then

                                    events.AddRange ((!decisionTask).Events |> Seq.filter (fun hev -> Between a b (hev.EventId)))
                                else
                                    (!decisionTask).Events <- events
                                    morepages := false
                            else
                                (!decisionTask).Events <- events
                                morepages := false
                        else 
                            events.AddRange (!decisionTask).Events
            
            if cancelToken.IsCancellationRequested then
                return null
            else                
                return (!decisionTask)
        }


    let CreatePollAndDecideAsync (pollAsync:Async<DecisionTask>) (decider: (DecisionTask -> Async<unit>)) =
        async {
            let! cancelToken = Async.CancellationToken

            while not(cancelToken.IsCancellationRequested) do 
                let! decisionTask = pollAsync
                do! decider(decisionTask)
        }

    let CreateRunParallelAsync (a:Async<unit>) (num:int)  =
        async {
            do! Async.Parallel([1..num] |> List.map (fun _ -> a)) |> Async.Ignore
        }

