module FsDecider.Registrar

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

let RegisterDomain (getswf:unit -> IAmazonSimpleWorkflow) (domainRequest:RegisterDomainRequest) = 
    use swf = getswf()

    let isListed =
        let listRequest = new Model.ListDomainsRequest()
        listRequest.RegistrationStatus <- RegistrationStatus.REGISTERED
        let listResponse = swf.ListDomains(listRequest)
        if listResponse.DomainInfos.NextPageToken <> null then failwith "RegisterDomain doesn't support multiple results sets."
        Seq.exists (fun (d:DomainInfo) -> d.Name = domainRequest.Name) (listResponse.DomainInfos.Infos)

    if not isListed then
        swf.RegisterDomain(domainRequest) |> ignore

// Looks to see if the workflow type is registered.
// If it is not yet registered, it registers it.
// If it is registered with the same configuration, then nothing else is done.
// If it is registered with a different configuration, an exception is thrown.
let RegisterWorkflowType (getswf:unit -> IAmazonSimpleWorkflow) (workflowRequest:RegisterWorkflowTypeRequest) = 
    use swf = getswf()
        
    // First find out if the name is registered in the domain
    let isListed = 
        let listRequest = new ListWorkflowTypesRequest()
        listRequest.Domain <- workflowRequest.Domain
        listRequest.Name <- workflowRequest.Name
        listRequest.RegistrationStatus <- RegistrationStatus.REGISTERED
        let response = swf.ListWorkflowTypes(listRequest)
        if response.WorkflowTypeInfos.NextPageToken <> null then failwith "RegisterWorkflow doesn't support multiple results sets."
        Seq.exists (fun (a:WorkflowTypeInfo) -> a.WorkflowType.Version = workflowRequest.Version) (response.WorkflowTypeInfos.TypeInfos)
            
    let sameConfiguration =
        match isListed with
        | false -> false // Configuration not the same if not listed
        | true ->
            // Get the configuration and compare
            let describeRequest = new DescribeWorkflowTypeRequest()
            describeRequest.Domain <- workflowRequest.Domain
            describeRequest.WorkflowType <- WorkflowType(Name=workflowRequest.Name, Version=workflowRequest.Version)
            let response = swf.DescribeWorkflowType(describeRequest)

            let sameChildPolicy = (workflowRequest.DefaultChildPolicy = response.WorkflowTypeDetail.Configuration.DefaultChildPolicy)
            let sameExecutionStartToCloseTimeout = (workflowRequest.DefaultExecutionStartToCloseTimeout = response.WorkflowTypeDetail.Configuration.DefaultExecutionStartToCloseTimeout)
            let sameLambdaRole = (workflowRequest.DefaultLambdaRole = response.WorkflowTypeDetail.Configuration.DefaultLambdaRole)
            let sameTaskList = (workflowRequest.DefaultTaskList = null && response.WorkflowTypeDetail.Configuration.DefaultTaskList = null) ||
                                (workflowRequest.DefaultTaskList.Name = response.WorkflowTypeDetail.Configuration.DefaultTaskList.Name)
            let sameTaskPriority = (workflowRequest.DefaultTaskPriority = response.WorkflowTypeDetail.Configuration.DefaultTaskPriority)
            let sameTaskStartToCloseTimeout = (workflowRequest.DefaultTaskStartToCloseTimeout = response.WorkflowTypeDetail.Configuration.DefaultTaskStartToCloseTimeout)

            if (sameChildPolicy &&
                sameExecutionStartToCloseTimeout &&
                sameLambdaRole &&
                sameTaskList &&
                sameTaskPriority &&
                sameTaskStartToCloseTimeout) then
                true // Registered with same configuration
            else
                failwith "Can't register workflow type. A workflow type is already registered with the same name and version but different configuration."
            
    if (not sameConfiguration) then
        swf.RegisterWorkflowType(workflowRequest) |> ignore

// Looks to see if the activity type is registered.
// If it is not yet registered, it registers it.
// If it is registered with the same configuration, then nothing else is done.
// If it is registered with a different configuration, an exception is thrown.
let RegisterActivityType (getswf:unit -> IAmazonSimpleWorkflow) (activityRequest:RegisterActivityTypeRequest) = 
    use swf = getswf()

    // First find out if the name is registered in the domain
    let isListed = 
        let listRequest = new ListActivityTypesRequest()
        listRequest.Domain <- activityRequest.Domain
        listRequest.Name <- activityRequest.Name
        listRequest.RegistrationStatus <- RegistrationStatus.REGISTERED
        let response = swf.ListActivityTypes(listRequest)
        if response.ActivityTypeInfos.NextPageToken <> null then failwith "RegisterActivity doesn't support multiple results sets."
        Seq.exists (fun (a:ActivityTypeInfo) -> a.ActivityType.Version = activityRequest.Version) (response.ActivityTypeInfos.TypeInfos)
            
    let sameConfiguration =
        match isListed with
        | false -> false // Configuration not the same if not listed
        | true ->
            // Get the configuration and compare
            let describeRequest = new DescribeActivityTypeRequest()
            describeRequest.Domain <- activityRequest.Domain
            describeRequest.ActivityType <- ActivityType(Name=activityRequest.Name, Version=activityRequest.Version)
            let response = swf.DescribeActivityType(describeRequest)

            if ((activityRequest.DefaultTaskHeartbeatTimeout = response.ActivityTypeDetail.Configuration.DefaultTaskHeartbeatTimeout) &&
                (activityRequest.DefaultTaskList.Name = response.ActivityTypeDetail.Configuration.DefaultTaskList.Name) &&
                (activityRequest.DefaultTaskPriority = response.ActivityTypeDetail.Configuration.DefaultTaskPriority) &&
                (activityRequest.DefaultTaskScheduleToCloseTimeout = response.ActivityTypeDetail.Configuration.DefaultTaskScheduleToCloseTimeout) &&
                (activityRequest.DefaultTaskScheduleToStartTimeout = response.ActivityTypeDetail.Configuration.DefaultTaskScheduleToStartTimeout) &&
                (activityRequest.DefaultTaskStartToCloseTimeout = response.ActivityTypeDetail.Configuration.DefaultTaskStartToCloseTimeout)) then
                true // Registered with same configuration
            else
                failwith "Can't register activity type. An activity type is already registered with the same name and version but different configuration."
            
    if (not sameConfiguration) then
        do swf.RegisterActivityType(activityRequest) |> ignore
