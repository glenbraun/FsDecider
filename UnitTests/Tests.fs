module FsDecider.UnitTests.Tests

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
                testCase "Schedule using do!"   <| TestScheduleActivityTask.``Schedule Activity Task using do!``
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
                testCase "Cancel using do!"         <| TestRequestCancelActivityTask.``Request Cancel Activity Task using do!``
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
                testCase "Start using do!"      <| TestStartChildWorkflowExecution.``Start Child Workflow Execution using do!``
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
                testCase "Cancel using do!"     <| TestRequestCancelExternalWorkflowExecution.``Request Cancel External Workflow Execution using do!``
            ]
            
            testList "ScheduleLambdaFunction" [
                testCase "Scheduling"           <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of Scheduling``
                testCase "Scheduled"            <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of Scheduled``
                testCase "Started"              <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of Started``
                testCase "Completed"            <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of Completed``
                testCase "Failed"               <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of Failed``
                testCase "TimedOut"             <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of TimedOut``
                testCase "StartFailed"          <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of StartFailed``
                testCase "ScheduleFailed"       <| TestScheduleLambdaFunction.``Schedule Lambda Function with result of ScheduleFailed``
                testCase "Schedule using do!"   <| TestScheduleLambdaFunction.``Schedule Lambda Function using do!``
            ]

            testList "WaitForLambdaFunction" [
                testCase "Completed"            <| TestWaitForLambdaFunction.``Wait For Lambda Function with One Completed Lambda Function``
                testCase "Failed"               <| TestWaitForLambdaFunction.``Wait For Lambda Function with One Failed Lambda Function``
                testCase "TimedOut"             <| TestWaitForLambdaFunction.``Wait For Lambda Function with One Timed Out Lambda Function``
                testCase "StartFailed"          <| TestWaitForLambdaFunction.``Wait For Lambda Function with Lambda Function Start Failure``
                testCase "ScheduleFailed"       <| TestWaitForLambdaFunction.``Wait For Lambda Function with Lambda Function Schedule Failure``
            ]
            
            testList "WaitForAnyLambdaFunction" [
                testCase "AllCompleted"         <| TestWaitForAnyLambdaFunction.``Wait For Any Lambda Function with All Completed Lambda Functions``
                testCase "OneCompleted"         <| TestWaitForAnyLambdaFunction.``Wait For Any Lambda Function with One Completed Lambda Function``
                testCase "NoneCompleted"        <| TestWaitForAnyLambdaFunction.``Wait For Any Lambda Function with No Completed Lambda Functions``
            ]
            
            testList "WaitForAllLambdaFunction" [
                testCase "AllCompleted"         <| TestWaitForAllLambdaFunction.``Wait For All Lambda Function with All Completed Lambda Functions``
                testCase "OneCompleted"         <| TestWaitForAllLambdaFunction.``Wait For All Lambda Function with One Completed Lambda Function``
                testCase "NoneCompleted"        <| TestWaitForAllLambdaFunction.``Wait For All Lambda Function with No Completed Lambda Functions``
            ]

            testList "StartTimer" [
                testCase "Starting"             <| TestStartTimer.``Start Timer with result of Starting``
                testCase "Started"              <| TestStartTimer.``Start Timer with result of Started``
                testCase "Fired"                <| TestStartTimer.``Start Timer with result of Fired``
                testCase "Canceled"             <| TestStartTimer.``Start Timer with result of Canceled``
                testCase "StartTimerFailed"     <| TestStartTimer.``Start Timer with result of StartTimerFailed``
                testCase "Start using do!"      <| TestStartTimer.``Start Timer using do!``
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
                testCase "Cancel using do!"     <| TestCancelTimer.``Cancel Timer using do!``
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
                testCase "Record using do!"     <| TestRecordMarker.``Record Marker using do!``
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

            testList "GetExecutionContext" [
                testCase "GetExecutionContext"          <| TestGetExecutionContext.``Get Execution Context``
                testCase "SetAndGetExecutionContext"    <| TestGetExecutionContext.``Set and Get Execution Context``
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

        testList "Primary ExecutionContextManager Tests" [
            
            testList "ScheduleActivityTask" [
                testCase "Completed"                                <| TestExecutionContextManager.``Test context for ScheduleActivityTask with Completed result``
                testCase "Canceled"                                 <| TestExecutionContextManager.``Test context for ScheduleActivityTask with Canceled result``
                testCase "TimedOut"                                 <| TestExecutionContextManager.``Test context for ScheduleActivityTask with TimedOut result``
                testCase "Failed"                                   <| TestExecutionContextManager.``Test context for ScheduleActivityTask with Failed result``
                testCase "ScheduleFailed"                           <| TestExecutionContextManager.``Test context for ScheduleActivityTask with ScheduleFailed result``
            ]

            testList "ScheduleLambdaFunction" [
                testCase "Completed"                                <| TestExecutionContextManager.``Test context for ScheduleLambdaFunction with Completed result``
                testCase "Failed"                                   <| TestExecutionContextManager.``Test context for ScheduleLambdaFunction with Failed result``
                testCase "TimedOut"                                 <| TestExecutionContextManager.``Test context for ScheduleLambdaFunction with TimedOut result``
                testCase "StartFailed"                              <| TestExecutionContextManager.``Test context for ScheduleLambdaFunction with StartFailed result``
                testCase "ScheduleFailed"                           <| TestExecutionContextManager.``Test context for ScheduleLambdaFunction with ScheduleFailed result``
            ]

            testList "StartChildWorkflowExecution" [
                testCase "Completed"                                <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with Completed result``
                testCase "Canceled"                                 <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with Canceled result``
                testCase "Failed"                                   <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with Failed result``
                testCase "TimedOut"                                 <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with TimedOut result``
                testCase "Terminated"                               <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with Terminated result``
                testCase "StartFailed"                              <| TestExecutionContextManager.``Test context for StartChildWorkflowExecution with StartFailed result``
            ]

            testList "StartTimer" [
                testCase "Fired"                                    <| TestExecutionContextManager.``Test context for StartTimer with Fired result``
                testCase "Canceled"                                 <| TestExecutionContextManager.``Test context for StartTimer with Canceled result``
                testCase "StartTimerFailed"                         <| TestExecutionContextManager.``Test context for StartTimer with StartTimerFailed result``
            ]

            testList "WorkflowExecutionSignaled" [
                testCase "Signaled"                                 <| TestExecutionContextManager.``Test context for WorkflowExecutionSignaled with Signaled result``
            ]

            testList "SignalExternalWorkflowExecution" [
                testCase "Signaled"                                 <| TestExecutionContextManager.``Test context for SignalExternalWorkflowExecution with Signaled result``
                testCase "Failed"                                   <| TestExecutionContextManager.``Test context for SignalExternalWorkflowExecution with Failed result``
            ]

            testList "RecordMarker" [
                testCase "Recorded"                                 <| TestExecutionContextManager.``Test context for RecordMarker with Recorded result``
                testCase "RecordMarkerFailed"                       <| TestExecutionContextManager.``Test context for RecordMarker with RecordMarkerFailed result``
            ]

            testList "MarkerRecorded" [
                testCase "Recorded"                                 <| TestExecutionContextManager.``Test context for MarkerRecorded with Recorded result``
                testCase "RecordMarkerFailed"                       <| TestExecutionContextManager.``Test context for MarkerRecorded with RecordMarkerFailed result``
            ]

            testList "RemoveFromContext" [
                testCase "ScheduleActivityTaskAction"               <| TestRemoveFromContext.``Test context for RemoveFromContext using ScheduleActivityTaskAction``
                testCase "ScheduleLambdaFunctionAction"             <| TestRemoveFromContext.``Test context for RemoveFromContext using ScheduleLambdaFunctionAction``
                testCase "StartChildWorkflowExecutionAction"        <| TestRemoveFromContext.``Test context for RemoveFromContext using StartChildWorkflowExecutionAction``
                testCase "StartTimerAction"                         <| TestRemoveFromContext.``Test context for RemoveFromContext using StartTimerAction``
                testCase "WorkflowExecutionSignaledAction"          <| TestRemoveFromContext.``Test context for RemoveFromContext using WorkflowExecutionSignaledAction``
                testCase "SignalExternalWorkflowExecutionAction"    <| TestRemoveFromContext.``Test context for RemoveFromContext using SignalExternalWorkflowExecutionAction``
                testCase "RecordMarkerAction"                       <| TestRemoveFromContext.``Test context for RemoveFromContext using RecordMarkerAction``
                testCase "MarkerRecordedAction"                     <| TestRemoveFromContext.``Test context for RemoveFromContext using MarkerRecordedAction``
            ]
        ]
        
    ]

