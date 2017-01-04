module FlowSharp.UnitTests.Main

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open Fuchu

      
// StartAndWaitForActivityTask (done)
// StartActivityTask 
// WaitForActivityTask
// RequestCancelActivityTask
// ExecuteLambdaFunction
// StartTimer
// CancelTimer
// RecordMarker
// StartChildWorkflowExecution
// CompleteChildWorkflowExecution
// SignalExternalWorkflowExecution
// RequestCancelExternalWorkflowExecution
// SignalReceivedSinceMarker
// SignalReceived
// CheckForWorkflowExecutionCancelRequested
// GetWorkflowExecutionInput

let tests = 
    testList "Primary Decider Actions" [
            testList "StartAndWaitForActivityTask" [
                testCase "Completed"        <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Completed Activity Task``
                testCase "Canceled"         <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Canceled Activity Task``
                testCase "Failed"           <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Failed Activity Task``
                testCase "TimedOut"         <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Timed Out Activity Task``
                testCase "ScheduleFailed"   <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with Activity Task Schedule Failure``
            ]
        ]



[<EntryPoint>]
let main argv = 
    TestConfiguration.GenerateOfflineHistory <- false
    TestConfiguration.IsConnected <- false

    //runParallel tests |> ignore
    run tests |> ignore
    0


