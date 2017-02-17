namespace FsDecider.UnitTests

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

type TestConfiguration() =
    static member val public GetSwfClient : unit -> IAmazonSimpleWorkflow = fun () -> null with get, set
    static member val public Domain : string = null with get, set
    static member val public WorkflowType : WorkflowType = null with get, set
    static member val public LambdaRole : string = null with get, set
    static member val public LambdaName : string = null with get, set
    static member val public ActivityType : ActivityType = null with get, set
    static member val public IsConnected = false with get, set
    static member val public ReverseOrder = false with get, set
    static member val public GenerateOfflineHistory = false with get, set
    static member val public Identity = "DeciderTests" with get, set
    static member val public TaskList = new TaskList(Name="main") with get, set
    static member val public LambdaInput = "\"Test lambda input\"" with get, set
    static member val public LambdaResult = "\"Hello from Lambda\"" with get, set
    static member val public TwentyMinuteTimeout = (TimeSpan.FromMinutes(20.0)).TotalSeconds.ToString()

    static member public Register() =
        if String.IsNullOrEmpty(TestConfiguration.Domain) then failwith "Error registering TestConfiguration. Domain cannot be null or empty."
        if TestConfiguration.WorkflowType = null then failwith "Error registering TestConfiguration. WorkflowType cannot be null."
        if TestConfiguration.ActivityType = null then failwith "Error registering TestConfiguration. ActivityType cannot be null."

        // Register the Domain
        let registerDomain = RegisterDomainRequest()
        registerDomain.Name <- TestConfiguration.Domain
        registerDomain.Description <- "Registered by FsDecider unit tests"
        registerDomain.WorkflowExecutionRetentionPeriodInDays <- "5"
        FsDecider.Registrar.RegisterDomain (TestConfiguration.GetSwfClient) registerDomain

        // Register the WorkflowType
        let registerWorkflowType = RegisterWorkflowTypeRequest()
        registerWorkflowType.Domain <- TestConfiguration.Domain
        registerWorkflowType.Name <- TestConfiguration.WorkflowType.Name
        registerWorkflowType.Version <- TestConfiguration.WorkflowType.Version
        registerWorkflowType.Description <- "Registered by FsDecider unit tests."
        registerWorkflowType.DefaultChildPolicy <- ChildPolicy.TERMINATE
        registerWorkflowType.DefaultTaskList <- TestConfiguration.TaskList
        registerWorkflowType.DefaultExecutionStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        registerWorkflowType.DefaultTaskStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        FsDecider.Registrar.RegisterWorkflowType (TestConfiguration.GetSwfClient) registerWorkflowType

        // Register the ActivityType
        let registerActivityType = RegisterActivityTypeRequest()
        registerActivityType.DefaultTaskHeartbeatTimeout <- "NONE"
        registerActivityType.DefaultTaskList <- TestConfiguration.TaskList
        registerActivityType.DefaultTaskScheduleToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        registerActivityType.DefaultTaskScheduleToStartTimeout <- TestConfiguration.TwentyMinuteTimeout
        registerActivityType.DefaultTaskStartToCloseTimeout <- TestConfiguration.TwentyMinuteTimeout
        registerActivityType.Description <- "Registered by FsDecider unit tests"
        registerActivityType.Domain <- TestConfiguration.Domain
        registerActivityType.Name <- TestConfiguration.ActivityType.Name
        registerActivityType.Version <- TestConfiguration.ActivityType.Version
        FsDecider.Registrar.RegisterActivityType (TestConfiguration.GetSwfClient) registerActivityType

