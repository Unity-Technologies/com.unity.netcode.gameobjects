# Multiprocess testing

## Why
Multiprocess testing can be used for different use cases like 
- integration tests (MLAPI + actual transport for example)
- performance testing. 
- Anything requiring a more realistic environment for testing that involves having a full client and server, communicating on a real network interface using real transports in separate Unity processes.


## How to write a multiprocess test
There's a few steps to write a multiprocess test

1. Your test class needs to inherit from `BaseMultiprocessTests`
2. Each test method needs the `MultiprocessContextBasedTest` attribute
3. Each test method needs to run `InitContextSteps();`
4. Each context based step needs to use 
```C#
yield return new ExecuteStepInContext(StepExecutionContext.Clients, stepToExecute: nbObjectsBytes => {
    // Something here
});
```

If you want to pass in dynamic test parameters (for example nunit `Values`), you need to pass them as a `byte[]` parameter to your step, since remote execution won't have context capture from the test execution and you won't see the test's parameters.


## How to run a test locally
Test players need to be built first to test locally. Integration with CI should do this automatically.

![](readme-ressources/Building-Player.png)

Then run the tests.

Performance tests should only be run from external processes (not from editor). This way the server code will run in a build, just as much as client code.

![](readme-ressources/Multiprocess.png)

## How it's done
### Multiple processes orchestration
todo 
TODO add diagrams for clients vs server and for automation bokken plans
#### Local orchestration
#### Bokken orchestration
### CI
todo
#### Performance report dashboards
todo
### Client-server test coordination
todo
### Context based step execution
todo

# Future considerations
- Integrate with local MultiInstance tests?
- Have ExecuteStepInContext a game facing feature for sequencing client-server actions?


# Sample
```cs
using System;
using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using static ExecuteStepInContext;

namespace MLAPI.MultiprocessRuntimeTests
{
    /*
     * Multiprocess testing can be used for different use cases like

        integration tests (MLAPI + actual transport for example)
        performance testing.
        Anything requiring a more realistic environment for testing that involves having a full client and server,
            communicating on a real network interface using real transports in separate Unity processes.

test locally, same tests execute on bokken (just the way they are deployed will change, same code logic executes)
     */
    public class DemoProcessTest : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 2;
        protected override bool m_IsPerformanceTest { get; } = false;

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator MyTest()
        {
            InitContextSteps();
            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Debug.Log("server stuff");
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                Debug.Log("client stuff");
                Assert.That(1, Is.EqualTo(1));
                // throw new Exception("asdf"); // this client side exception will be communicated to the coordinator, making the test fail
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                TestCoordinator.Instance.WriteTestResultsServerRpc(123);
                TestCoordinator.Instance.WriteTestResultsServerRpc(123);
                TestCoordinator.Instance.WriteTestResultsServerRpc(123); // could be replaced by json string instead for ease of use?
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                TestCoordinator.ConsumeCurrentResult();
                foreach (var clientID in TestCoordinator.AllClientIdsExceptMine)
                {
                    TestCoordinator.ConsumeCurrentResult(clientID);
                }

                foreach (var (clientID, result) in TestCoordinator.ConsumeCurrentResult())
                {
                    Assert.That(result, Is.EqualTo(123));
                }
            });


            int someValue = 456; // one caveat to executeStepInContext is contrary to instinct, this is not shared between server and client execution.
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var valueComingFromServer = BitConverter.ToInt32(bytes, 0);
            }, paramToPass: BitConverter.GetBytes(456)); // could be replaced by JSON string instead for ease of use?
            // useful for taking in [Values] method parameters as these are only known by the server


            // when you have client steps that take more than one frame
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                void Update(float _)
                {
                    NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                    TestCoordinator.Instance.ClientFinishedServerRpc(); // since finishOnInvoke is false, we need to do this manually
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, waitMultipleUpdates: true); // this keeps waiting "are you done? are you done? are you done?"
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                int cpt = 0;
                void Update(float _)
                {
                    TestCoordinator.Instance.WriteTestResultsServerRpc(Time.time);
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, additionalIsFinishedWaiter: () => // this keeps waiting "are you done? are you done? are you done?"
            {
                foreach (var (clientId, latest) in TestCoordinator.ConsumeCurrentResult())
                {
                    return latest >= 10;
                }
                return false;
            });


        }

        [UnityTest, Performance] // already existing performance framework https://docs.unity3d.com/Packages/com.unity.test-framework.performance@2.8/manual/index.html
        public IEnumerator PerfTest()
        {
            var totalAllocSampleGroup = new SampleGroup("GC Alloc", SampleUnit.Kilobyte);
            var allocStat = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Custom(totalAllocSampleGroup, allocStat / 1024); // this will record in Unity's shared Performance DB.
            // Dashboards will be able to display these stats overtime
            yield return null;
        }

        /*
         * To run these tests, go to unity menu, build
         * run the test you want from test runner
         *
         * Can run these from separate player, so both the server and client are in builds (and not just the clients while the
         * server is in the editor) good for perf tests
         *
         * I timed myself, I was about to create this demo test code with no debugging involved in 30 minutes. It's pretty empty,
         * but it can give you an idea of the overhead involved, it's pretty short (and I had to dig for talking about every single parameters)
         *
         */
    }
}

```