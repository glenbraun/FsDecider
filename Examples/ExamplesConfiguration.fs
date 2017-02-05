namespace FlowSharp.Examples

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type ExamplesConfiguration() =
    static member val public ReverseOrder = false with get, set
    static member val public GetSwfClient = fun () -> new AmazonSimpleWorkflowClient(RegionEndpoint.USWest2) with get, set
    static member val public Domain = "wo_admin" with get, set
    static member val public Identity = "FlowSharp.Examples" with get, set
    static member val public TaskList = new TaskList(Name="main") with get, set
    static member val public WorkflowType = new WorkflowType(Name="Workflow1", Version="1") with get, set
    static member val public ActivityType = new ActivityType(Name = "Activity1", Version = "2") with get, set
    static member val public TwentyMinuteTimeout = (TimeSpan.FromMinutes(20.0)).TotalSeconds.ToString()
