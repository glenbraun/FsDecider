module FlowSharp.UnitTests.Main

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open Fuchu

let tests = 
    testList "Primary Unit Tests" [
        
        testList "Primary Decider Actions" [
            
            testList "ScheduleActivityTask" [
                testCase "Scheduling"           <| TestScheduleActivityTask.``Schedule Activity Task with result of Scheduling``
                testCase "Scheduled"            <| TestScheduleActivityTask.``Schedule Activity Task with result of Scheduled``
                testCase "Started"              <| TestScheduleActivityTask.``Schedule Activity Task with result of Started``
                testCase "Completed"            <| TestScheduleActivityTask.``Schedule Activity Task with result of Completed``
                testCase "Canceled"             <| TestScheduleActivityTask.``Schedule Activity Task with result of Canceled``
                testCase "TimedOut"             <| TestScheduleActivityTask.``Schedule Activity Task with result of TimedOut``
                testCase "Failed"               <| TestScheduleActivityTask.``Schedule Activity Task with result of Failed``
                testCase "ScheduleFailed"       <| TestScheduleActivityTask.``Schedule Activity Task with Schedule Failure``
            ]
            
            testList "ScheduleAndWaitForActivityTask" [
                testCase "Completed"            <| TestScheduleAndWaitForActivityTask.``Schedule And Wait For Activity Task with One Completed Activity Task``
                testCase "Canceled"             <| TestScheduleAndWaitForActivityTask.``Schedule And Wait For Activity Task with One Canceled Activity Task``
                testCase "Failed"               <| TestScheduleAndWaitForActivityTask.``Schedule And Wait For Activity Task with One Failed Activity Task``
                testCase "TimedOut"             <| TestScheduleAndWaitForActivityTask.``Schedule And Wait For Activity Task with One Timed Out Activity Task``
                testCase "ScheduleFailed"       <| TestScheduleAndWaitForActivityTask.``Schedule And Wait For Activity Task with Activity Task Schedule Failure``
            ]
            
            testList "WaitForActivityTask" [
                testCase "Completed"            <| TestWaitForActivityTask.``Wait For Activity Task with One Completed Activity Task``
                testCase "Canceled"             <| TestWaitForActivityTask.``Wait For Activity Task with One Canceled Activity Task``
                testCase "Failed"               <| TestWaitForActivityTask.``Wait For Activity Task with One Failed Activity Task``
                testCase "TimedOut"             <| TestWaitForActivityTask.``Wait For Activity Task with One Timed Out Activity Task``
                testCase "ScheduleFailed"       <| TestWaitForActivityTask.``Wait For Activity Task with Activity Task Schedule Failure``
            ]
            
            testList "WaitForAnyActivityTask" [
                testCase "AllCompleted"         <| TestWaitForAnyActivityTask.``Wait For Any Activity Task with All Completed Activity Tasks``
                testCase "OneCompleted"         <| TestWaitForAnyActivityTask.``Wait For Any Activity Task with One Completed Activity Tasks``
                testCase "NoneCompleted"        <| TestWaitForAnyActivityTask.``Wait For Any Activity Task with No Completed Activity Tasks``
            ]
            
            testList "WaitForAllActivityTask" [
                testCase "AllCompleted"         <| TestWaitForAllActivityTask.``Wait For All Activity Task with All Completed Activity Tasks``
                testCase "OneCompleted"         <| TestWaitForAllActivityTask.``Wait For All Activity Task with One Completed Activity Tasks``
                testCase "NoneCompleted"        <| TestWaitForAllActivityTask.``Wait For All Activity Task with No Completed Activity Tasks``
            ]
            
            testList "RequestCancelActivityTask" [
                testCase "CancelRequested"          <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of CancelRequested``
                testCase "RequestCancelFailed"      <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of RequestCancelFailed``
                testCase "ActivityFinished"         <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of ActivityFinished``
                testCase "ActivityScheduleFailed"   <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of ActivityScheduleFailed``
            ]
            
            testList "StartChildWorkflowExecution" [
                testCase "Starting"             <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Starting``
                testCase "Initiated"            <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Initiated``
                testCase "Started"              <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Started``
                testCase "Completed"            <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Completed``
                testCase "Canceled"             <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Canceled``
                testCase "Failed"               <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Failed``
                testCase "TimedOut"             <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of TimedOut``
                testCase "Terminated"           <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of Terminated``
                testCase "StartFailed"          <| TestStartChildWorkflowExecution.``Start Child Workflow Execution with result of StartFailed``
            ]
            
            testList "WaitForChildWorkflowExecution" [
                testCase "Completed"            <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Completed``
                testCase "Canceled"             <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Canceled``
                testCase "Failed"               <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Failed``
                testCase "TimedOut"             <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of TimedOut``
                testCase "Terminated"           <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of Terminated``
                testCase "StartFailed"          <| TestWaitForChildWorkflowExecution.``Wait for Child Workflow Execution with result of StartFailed``
            ]
            
            testList "WaitForAnyChildWorkflowExecution" [
                testCase "AllCompleted"         <| TestWaitForAnyChildWorkflowExecution.``Wait For Any Child Workflow Execution with All Completed Child Workflow Execution``
                testCase "OneCompleted"         <| TestWaitForAnyChildWorkflowExecution.``Wait For Any Child Workflow Execution with One Completed Child Workflow Execution``
                testCase "NoneCompleted"        <| TestWaitForAnyChildWorkflowExecution.``Wait For Any Child Workflow Execution with No Completed Child Workflow Execution``
            ]
            
            testList "WaitForAllChildWorkflowExecution" [
                testCase "AllCompleted"         <| TestWaitForAllChildWorkflowExecution.``Wait For All Child Workflow Execution with All Completed Child Workflow Execution``
                testCase "OneCompleted"         <| TestWaitForAllChildWorkflowExecution.``Wait For All Child Workflow Execution with One Completed Child Workflow Execution``
                testCase "NoneCompleted"        <| TestWaitForAllChildWorkflowExecution.``Wait For All Child Workflow Execution with No Completed Child Workflow Execution``
            ]
            
            testList "RequestCancelExternalWorkflowExecution" [
                testCase "Requesting"           <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Requesting``
                testCase "Initiated"            <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Initiated``
                testCase "Delivered"            <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Delivered``
                testCase "Failed"               <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution with result of Failed``
            ]
            
            testList "ScheduleAndWaitForLambdaFunction" [
                testCase "ScheduleFailed"       <| TestScheduleAndWaitForLambdaFunction.``Schedule and wait for Lambda Function with result of ScheduleFailed``
                testCase "StartFailed"          <| TestScheduleAndWaitForLambdaFunction.``Schedule and wait for Lambda Function with result of StartFailed``
                testCase "Completed"            <| TestScheduleAndWaitForLambdaFunction.``Schedule and wait for Lambda Function with result of Completed``
                testCase "Failed"               <| TestScheduleAndWaitForLambdaFunction.``Schedule and wait for Lambda Function with result of Failed``
                testCase "TimedOut"             <| TestScheduleAndWaitForLambdaFunction.``Schedule and wait for Lambda Function with result of TimedOut``
            ]
            
            testList "StartTimer" [
                testCase "Starting"             <| TestStartTimer.``Start Timer with result of Starting``
                testCase "Started"              <| TestStartTimer.``Start Timer with result of Started``
                testCase "Fired"                <| TestStartTimer.``Start Timer with result of Fired``
                testCase "Canceled"             <| TestStartTimer.``Start Timer with result of Canceled``
                testCase "StartTimerFailed"     <| TestStartTimer.``Start Timer with result of StartTimerFailed``
            ]
            
            testList "WaitForTimer" [
                testCase "Fired"                <| TestWaitForTimer.``Wait for Timer with result of Fired``
                testCase "Canceled"             <| TestWaitForTimer.``Wait for Timer with result of Canceled``
                testCase "StartTimerFailed"     <| TestWaitForTimer.``Wait for Timer with result of StartTimerFailed``
            ]            
            
            testList "CancelTimer" [
                testCase "Canceling"            <| TestCancelTimer.``Cancel Timer with result of Canceling``
                testCase "Canceled"             <| TestCancelTimer.``Cancel Timer with result of Canceled``
                testCase "Fired"                <| TestCancelTimer.``Cancel Timer with result of Fired``
                testCase "StartTimerFailed"     <| TestCancelTimer.``Cancel Timer with result of StartTimerFailed``
                testCase "CancelTimerFailed"    <| TestCancelTimer.``Cancel Timer with result of CancelTimerFailed``
            ]
            
            testList "MarkerRecorded" [
                testCase "NotRecorded"          <| TestMarkerRecorded.``Marker Recorded with result of NotRecorded``
                testCase "RecordMarkerFailed"   <| TestMarkerRecorded.``Marker Recorded with result of RecordMarkerFailed``
                testCase "MarkerRecorded"       <| TestMarkerRecorded.``Marker Recorded with result of MarkerRecorded``
            ]
            
            testList "RecordMarker" [
                testCase "Recording"            <| TestRecordMarker.``Record Marker with result of Recording``
                testCase "RecordMarkerFailed"   <| TestRecordMarker.``Record Marker with result of RecordMarkerFailed``
                testCase "MarkerRecorded"       <| TestRecordMarker.``Record Marker with result of MarkerRecorded``
            ]
            
            testList "SignalExternalWorkflowExecution" [
                testCase "Signaling"            <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Signaling``
                testCase "Initiated"            <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Initiated``
                testCase "Signaled"             <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Signaled``
                testCase "Failed"               <| TestSignalExternalWorkflowExecution.``Signal External Workflow Execution with result of Failed``
            ]

            testList "WorkflowExecutionSignaled" [
                testCase "NotSignaled"          <| TestWorkflowExecutionSignaled.``Workflow Execution Signaled with result of NotSignaled``
                testCase "Signaled"             <| TestWorkflowExecutionSignaled.``Workflow Execution Signaled with result of Signaled``
            ]

            testList "WaitForWorkflowExecutionSignaled" [
                testCase "Signaled"             <| TestWaitForWorkflowExecutionSignaled.``Wait For Workflow Execution Signaled with result of Signaled``
            ]

            testList "WorkflowExecutionCancelRequested" [
                testCase "NotRequested"         <| TestWorkflowExecutionCancelRequested.``Workflow Execution Cancel Requested with result of NotRequested``
                testCase "CancelRequested"      <| TestWorkflowExecutionCancelRequested.``Workflow Execution Cancel Requested with result of CancelRequested``
            ]

            testList "GetWorkflowExecutionInput" [
                testCase "GetNonNullInput"      <| TestGetWorkflowExecutionInput.``Get Workflow Execution Input with non-null input``
                testCase "GetNullInput"         <| TestGetWorkflowExecutionInput.``Get Workflow Execution Input with null input``
            ]

            testList "ReturnResult" [
                testCase "RespondDecisionTaskCompleted"         <| TestReturnResult.``Return Result of RespondDecisionTaskCompleted``
                testCase "CompleteWorkflowExecution"            <| TestReturnResult.``Return Result of CompleteWorkflowExecution``
                testCase "CompleteWorkflowExecutionFailed"      <| TestReturnResult.``Return Result of CompleteWorkflowExecutionFailed``
                testCase "CancelWorkflowExecution"              <| TestReturnResult.``Return Result of CancelWorkflowExecution``
                testCase "CancelWorkflowExecutionFailed"        <| TestReturnResult.``Return Result of CancelWorkflowExecutionFailed``
                testCase "FailWorkflowExecution"                <| TestReturnResult.``Return Result of FailWorkflowExecution``
                testCase "FailWorkflowExecutionFailed"          <| TestReturnResult.``Return Result of FailWorkflowExecutionFailed``
                testCase "ContinueAsNewWorkflowExecution"       <| TestReturnResult.``Return Result of ContinueAsNewWorkflowExecution``
                testCase "ContinueAsNewWorkflowExecutionFailed" <| TestReturnResult.``Return Result of ContinueAsNewWorkflowExecutionFailed``
            ]
            
        ]        
        
        testList "Primary Builder Tests" [
            
            testList "Zero" [
                testCase "EmptyComputationExpression"           <| TestZero.``An Empty Computation Expression which results in Unit``
            ]
            
            testList "For Loop" [
                testCase "ForToLoopUnitBody"                      <| TestForLoop.``A For To Loop with an empty body expression which results in Unit``
                testCase "ForInLoopUnitBody"                      <| TestForLoop.``A For In Loop with an empty body expression which results in Unit``
                testCase "ForToLoopStartWaitActivityBody"         <| TestForLoop.``A For To Loop with a body that Starts and Waits for an Activity Task with unique results per iteration``
                testCase "ForInLoopStartActivityBody"             <| TestForLoop.``A For In Loop with a body that Starts an Activity Task with unique results per iteration``
            ]
            
            testList "While Loop" [
                testCase "WhileLoopUnitBody"                      <| TestWhileLoop.``A While Loop with an empty body expression which results in Unit``
                testCase "WhileLoopRetryActivity"                 <| TestWhileLoop.``A While Loop with a body that tries up to three times for a successful Activity Task completion``
            ]

            testList "Try With" [
                testCase "TryWithReturnCompleted"                 <| TestTryWith.``A Try With expression with a Return Completed``
                testCase "TryWithException"                       <| TestTryWith.``A Try With expression with an exception raised in the body``
                testCase "TryWithReturnException"                 <| TestTryWith.``A Try With expression with an excpetion from a ContinueAsNew``
            ]
    
            testList "Try Finally" [
                testCase "TryFinallyUnitBody"                     <| TestTryFinally.``A Try Finally expression with a body of Unit``
                testCase "TryFinallyWithActivity"                 <| TestTryFinally.``A Try Finally expression with a Start and Wait for Activity Task``
                testCase "TryFinallyContinueAsNew"                <| TestTryFinally.``A Try Finally expression with an excpetion from a ContinueAsNew``
            ]            
        ]
        
    ]

[<EntryPoint>]
let main argv = 
    TestConfiguration.GenerateOfflineHistory <- false
    TestConfiguration.IsConnected <- false
    TestConfiguration.ReverseOrder <- true

    //let tests = testCase "One Off"  <| TestRequestCancelActivityTask.``Request Cancel Activity Task with result of ActivityScheduleFailed``

    //runParallel tests |> ignore  // Note: Can't run in parallel when IsConnected is true because there's no matching of decision tasks with the right decider
    run tests |> ignore
     
    0


