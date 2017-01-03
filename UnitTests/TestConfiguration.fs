namespace FlowSharp.UnitTests

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type TestConfiguration() =
    static member val public IsConnected = false with get, set
    static member val public GetSwfClient = fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) with get, set
    static member val public TestDomain = "wo_admin" with get, set
    static member val public TestIdentity = "DeciderTests" with get, set
    static member val public TestTaskList = new TaskList(Name="main") with get, set
    static member val public TestWorkflowType = new WorkflowType(Name="Workflow1", Version="1") with get, set
    static member val public TestLambdaRole = "arn:aws:iam::538386600280:role/swf-lambda" with get, set
    static member val public TestActivityType = new ActivityType(Name = "Activity1", Version = "2") with get, set
    static member val public TwentyMinuteTimeout = uint32 ((TimeSpan.FromMinutes(20.0)).TotalSeconds)

