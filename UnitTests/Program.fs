module FlowSharp.UnitTests.Main

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open Fuchu

      
// StartAndWaitForActivityTask (done)
// StartActivityTask (done)
// WaitForActivityTask (done)
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
                testCase "Completed"            <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Completed Activity Task``
                testCase "Canceled"             <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Canceled Activity Task``
                testCase "Failed"               <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Failed Activity Task``
                testCase "TimedOut"             <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Timed Out Activity Task``
                testCase "ScheduleFailed"       <| TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with Activity Task Schedule Failure``
            ]

            testList "StartActivityTask" [
                testCase "Scheduling"           <| TestStartActivityTask.``Start Activity Task with result of Scheduling``
                testCase "Scheduled"            <| TestStartActivityTask.``Start Activity Task with result of Scheduled``
                testCase "Started"              <| TestStartActivityTask.``Start Activity Task with result of Started``
                testCase "ScheduleFailed"       <| TestStartActivityTask.``Start Activity Task with Schedule Failure``
            ]

            testList "WaitForActivityTask" [
                testCase "Completed"            <| TestWaitForActivityTask.``Wait For Activity Task with One Completed Activity Task``
                testCase "Canceled"             <| TestWaitForActivityTask.``Wait For Activity Task with One Canceled Activity Task``
                testCase "Failed"               <| TestWaitForActivityTask.``Wait For Activity Task with One Failed Activity Task``
                testCase "TimedOut"             <| TestWaitForActivityTask.``Wait For Activity Task with One Timed Out Activity Task``
                testCase "ScheduleFailed"       <| TestWaitForActivityTask.``Wait For Activity Task with Activity Task Schedule Failure``
            ]

            testList "RequestCancelActivityTask" [
                testCase "ScheduleFailed"       <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of ScheduleFailed``
                testCase "RequestCancelFailed"  <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of RequestCancelFailed``
                testCase "CancelRequested"      <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of CancelRequested``
                testCase "Completed"            <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of Completed``
                testCase "Canceled"             <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of Canceled``
                testCase "TimedOut"             <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of TimedOut``
                testCase "Failed"               <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of Failed``
            ]
        ]



[<EntryPoint>]
let main argv = 
    TestConfiguration.GenerateOfflineHistory <- true
    TestConfiguration.IsConnected <- false

    //runParallel tests |> ignore  // Note: Can't run in parallel when IsConnected is true because there's no matching of decision tasks with the right decider
    run tests |> ignore
    0


