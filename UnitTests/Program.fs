module FlowSharp.UnitTests.Main

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open Fuchu

      
// StartAndWaitForActivityTask (done)
// StartActivityTask (done)
// WaitForActivityTask (done)
// RequestCancelActivityTask (done)
// StartAndWaitForLambdaFunction (done)
// StartTimer (done)
// CancelTimer (done)
// WaitForTimer (done)
// RecordMarker (done)
// StartChildWorkflowExecution (done)
// WaitForChildWorkflowExecution (done)
// SignalExternalWorkflowExecution (done)
// RequestCancelExternalWorkflowExecution (done)
// WorkflowExecutionSignaled (done)
// WaitForWorkflowExecutionSignaled (done)
// WorkflowExecutionCancelRequested (done)
// GetWorkflowExecutionInput
// ReturnResult

let tests = 
    testList "Primary Decider Actions" [
            (*
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

            testList "StartAndWaitForLambdaFunction" [
                testCase "ScheduleFailed"       <| TestStartAndWaitForLambdaFunction.``Start and wait for Lambda Function with result of ScheduleFailed``
                testCase "StartFailed"          <| TestStartAndWaitForLambdaFunction.``Start and wait for Lambda Function with result of StartFailed``
                testCase "Completed"            <| TestStartAndWaitForLambdaFunction.``Start and wait for Lambda Function with result of Completed``
                testCase "Failed"               <| TestStartAndWaitForLambdaFunction.``Start and wait for Lambda Function with result of Failed``
                testCase "TimedOut"             <| TestStartAndWaitForLambdaFunction.``Start and wait for Lambda Function with result of TimedOut``
            ]
            
            testList "StartTimer" [
                testCase "StartTimerFailed"     <| TestStartTimer.``Start Timer with result of StartTimerFailed``
                testCase "Starting"             <| TestStartTimer.``Start Timer with result of Starting``
                testCase "Started"              <| TestStartTimer.``Start Timer with result of Started``
            ]
            
            testList "CancelTimer" [
                testCase "NotStarted"           <| TestCancelTimer.``Cancel Timer with result of NotStarted``
                testCase "CancelTimerFailed"    <| TestCancelTimer.``Cancel Timer with result of CancelTimerFailed``
                testCase "Canceling"            <| TestCancelTimer.``Cancel Timer with result of Canceling``
                testCase "Canceled"             <| TestCancelTimer.``Cancel Timer with result of Canceled``
                testCase "Fired"                <| TestCancelTimer.``Cancel Timer with result of Fired``
            ]

            testList "WaitForTimer" [
                testCase "StartTimerFailed"     <| TestWaitForTimer.``Wait for Timer with result of StartTimerFailed``
                testCase "Canceled"             <| TestWaitForTimer.``Wait for Timer with result of Canceled``
                testCase "Fired"                <| TestWaitForTimer.``Wait for Timer with result of Fired``
            ]

            testList "RecordMarker" [
                testCase "Recording"            <| TestRecordMarker.``Record Marker with result of Recording``
                testCase "RecordMarkerFailed"   <| TestRecordMarker.``Record Marker with result of RecordMarkerFailed``
                testCase "MarkerRecorded"       <| TestRecordMarker.``Record Marker with result of MarkerRecorded``
            ]

            testList "StartChildWorkflowExecution" [
                testCase "Scheduling"           <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Scheduling``
                testCase "StartFailed"          <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of StartFailed``
                testCase "Initiated"            <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Initiated``
                testCase "Started"              <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Started``
            ]

            testList "WaitForChildWorkflowExecution" [
                testCase "StartFailed"          <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of StartFailed``
                testCase "Completed"            <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Completed``
                testCase "Canceled"             <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Canceled``
                testCase "TimedOut"             <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of TimedOut``
                testCase "Failed"               <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Failed``
                testCase "Terminated"           <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Terminated``
            ]

            testList "SignalExternalWorkflowExecution" [
                testCase "Signaling"            <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Signaling``
                testCase "Initiated"            <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Initiated``
                testCase "Signaled"             <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Signaled``
                testCase "Failed"               <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Failed``
            ]

            testList "RequestCancelExternalWorkflowExecution" [
                testCase "Requesting"           <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Requesting``
                testCase "Initiated"            <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Initiated``
                testCase "Delivered"            <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Delivered``
                testCase "Failed"               <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Failed``
            ]

            testList "WorkflowExecutionSignaled" [
                testCase "NotSignaled"          <| TestWorkflowExecutionSignaled.``Workflow Execution Signaled with result of NotSignaled``
                testCase "Signaled"             <| TestWorkflowExecutionSignaled.``Workflow Execution Signaled with result of Signaled``
            ]

            testList "WaitForWorkflowExecutionSignaled" [
                testCase "Signaled"             <| TestWaitForWorkflowExecutionSignaled.``Wait For Workflow Execution Signaled with result of Signaled``
            ]
            *)
            testList "WorkflowExecutionCancelRequested" [
                testCase "NotRequested"         <| TestWorkflowExecutionCancelRequested.``Workflow Execution Cancel Requested with result of NotRequested``
                testCase "CancelRequested"      <| TestWorkflowExecutionCancelRequested.``Workflow Execution Cancel Requested with result of CancelRequested``
            ]
        ]
        

[<EntryPoint>]
let main argv = 
    TestConfiguration.GenerateOfflineHistory <- true
    TestConfiguration.IsConnected <- false

    //runParallel tests |> ignore  // Note: Can't run in parallel when IsConnected is true because there's no matching of decision tasks with the right decider
    run tests |> ignore
    0


