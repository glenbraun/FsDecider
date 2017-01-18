namespace FlowSharp

open System
open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open FlowSharp.Actions

module SimpleExecutionContext =

    type SimpleExecutionContext(ExecutionContext:string) =
        let mutable actionToResultMap = Map.empty
    
        let ParseExecutionContext () =
            (*
                execution-context :=
                    action-type-name '(' label '=' string-literal [',' label '=' string-literal]... ')' '=>'  result-type-name '(' label '=' string-literal [',' label '=' string-literal]... ')'

                action-type-name :=
                    label of 
                        'ActivityTask' | 
                        'LambdaFunction' |
                        'ChildWorkflowExecution' |
                        'Timer' |
                        'Signal' |
                        'Marker'

                result-type-name :=
                    label of 
                        when action-type-name = 'ActivityTask' then
                            'Completed' | 'Canceled' | 'Failed' | 'TimedOut'
                        when action-type-name = 'LambdaFunction' then
                            'Completed' | 'Failed' | 'TimedOut'
                        when action-type-name = 'ChildWorkflowExecution' then
                            'Completed' | 'Canceled' | 'Failed' | 'TimedOut' | 'Terminated'
                        when action-type-name = 'Timer' then
                            'Started' | 'Fired' | 'Canceled'
                        when action-type-name = 'Signal' then
                            'NotSignaled' | 'Signaled'

            *)
            ()

        member this.ReadFromContext(WorkflowExecutionSignaledAction.Attributes(signalName)) : WorkflowExecutionSignaledFromContextAction =
            if signalName = "From Context" then
                let attr = WorkflowExecutionSignaledEventAttributes(SignalName=signalName, Input="Some Input")
                WorkflowExecutionSignaledFromContextAction.ResultFromContext(WorkflowExecutionSignaledResult.Signaled(attr))
            elif signalName = "Exception" then
                WorkflowExecutionSignaledFromContextAction.ExpectedButNotFound(System.Exception("Signal expected from context"))
            else
                WorkflowExecutionSignaledFromContextAction.NotInContext(WorkflowExecutionSignaledAction.Attributes(signalName))

        member this.WriteToContext(result:WorkflowExecutionSignaledResult) : unit =
            ()

