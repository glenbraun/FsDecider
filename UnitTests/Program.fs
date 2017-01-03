module FlowSharp.UnitTests.Main

open System

open Amazon
open Amazon.SimpleWorkflow
open Amazon.SimpleWorkflow.Model

open Fuchu

      
let tests = 
        testList "Primary Decider Actions" [
            testCase "TestStartAndWaitForActivityTask" <|
                TestStartAndWaitForActivityTask.``Start And Wait For Activity Task with One Completed Activity Task``
//            testCase "another test" <|
//                ``When 2.0 is added to 2.0 expect 4.01``
        ]

[<EntryPoint>]
let main argv = 
    TestConfiguration.IsConnected <- true

    run tests |> ignore
    0


