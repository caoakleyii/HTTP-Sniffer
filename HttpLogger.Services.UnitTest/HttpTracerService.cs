using System;
using System.Collections.Specialized;
using System.Threading;
using HttpLogger.Models;
using HttpLogger.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace HttpLogger.Services.UnitTest
{
    [TestClass]
    public class HttpTracerService_Test
    {
        [TestMethod]
        public void MonitorHighTraffic_AboveThreshold()
        {
            // Mock our repostiory
            var stubTraceRepository = MockRepository.GenerateStub<IHttpTraceRepository>();
            

            // Create mock data with mock traces
            var testTraces = new OrderedDictionary();
            var testDate = DateTime.Now;
            for (var i = 0; i < 6; i++)
            {
                var id = Guid.NewGuid().ToString();
                testTraces.Add(id, new HttpTrace
                {
                    Id = id,
                    RemoteUri = new Uri("http://testsite.com/"),
                    RequestDate = testDate
                });
            }

            // Stub out our mock repository withour mock data
            stubTraceRepository.Stub(x => x.ReadTraces()).Return(testTraces);
            var gui = new UIService();

            // Inject our mock repo into our service
            var service = new HttpTracerService(stubTraceRepository, gui);

            // Create a wait handle, since this method is threaded
            var stopWaitHandle = new AutoResetEvent(false);

            AssertFailedException failedAssert = null; 

            // our threads have callbacks you can hook into if desired.
            var threadObject = new ThreadObject
            {
                ThreadStartObject = 5,
                
                // create a callback that asserts our data,  and unblocks our wait
                ThreadCallback = delegate (object o)
                {

                    try
                    {
                        Assert.IsTrue(gui.TraceViewModel.CurrentNotifaction.IsOverThreshold);
                        stopWaitHandle.Set();
                    }
                    catch(AssertFailedException ex)
                    {
                        failedAssert = ex;
                    }                    
                    return true;
                }
            };

            // call the method we want to test.
            service.MonitorHighTraffic(threadObject);

            // wait for the OK from our monitor high traffic thread
            // timeout of 10 seconds.
            stopWaitHandle.WaitOne(10 * 1000);

            if (failedAssert != null)
            {
                Assert.Fail(failedAssert.Message);
            }
        }

        [TestMethod]
        public void MonitorHighTraffic_BelowThreshold()
        {
            // Mock our repostiory
            var stubTraceRepository = MockRepository.GenerateStub<IHttpTraceRepository>();


            // Create mock data with mock traces
            var testTraces = new OrderedDictionary();
            var testDate = DateTime.Now.AddMinutes(-5);
            for (var i = 0; i < 6; i++)
            {
                var id = Guid.NewGuid().ToString();
                testTraces.Add(id, new HttpTrace
                {
                    Id = id,
                    RemoteUri = new Uri("http://testsite.com/"),
                    RequestDate = testDate
                });
            }

            // Stub out our mock repository withour mock data
            stubTraceRepository.Stub(x => x.ReadTraces()).Return(testTraces);
            var gui = new UIService();

            // Inject our mock repo into our service
            var service = new HttpTracerService(stubTraceRepository, gui);

            // Create a wait handle, since this method is threaded
            var stopWaitHandle = new AutoResetEvent(false);

            AssertFailedException failedAssert = null;

            // our threads have callbacks you can hook into if desired.
            var threadObject = new ThreadObject
            {
                ThreadStartObject = 5,

                // create a callback that asserts our data,  and unblocks our wait
                ThreadCallback = delegate (object o)
                {
                    try
                    {
                        Assert.IsTrue(!gui.TraceViewModel.CurrentNotifaction.IsOverThreshold);
                        stopWaitHandle.Set();
                    }
                    catch (AssertFailedException ex)
                    {
                        failedAssert = ex;
                    }
                    return true;
                }
            };

            // call the method we want to test.
            service.MonitorHighTraffic(threadObject);

            // wait for the OK from our monitor high traffic thread
            // timeout of 10 seconds.
            stopWaitHandle.WaitOne(10 * 1000);

            if (failedAssert != null)
            {
                Assert.Fail(failedAssert.Message);
            }
        }

        [TestMethod]
        public void MonitorHighTraffic_RecordThresholdHistory()
        {
            // Mock our repostiory
            var stubTraceRepository = MockRepository.GenerateStub<IHttpTraceRepository>();


            // Create mock data with mock traces
            var testTraces = new OrderedDictionary();
            var testDate = DateTime.Now;
            for (var i = 0; i < 6; i++)
            {
                var id = Guid.NewGuid().ToString();
                testTraces.Add(id, new HttpTrace
                {
                    Id = id,
                    RemoteUri = new Uri("http://testsite.com/"),
                    RequestDate = testDate
                });
            }

            // Stub out our mock repository withour mock data
            stubTraceRepository.Stub(x => x.ReadTraces()).Return(testTraces);
            var gui = new UIService();

            // Inject our mock repo into our service
            var service = new HttpTracerService(stubTraceRepository, gui);

            // Create a wait handle, since this method is threaded
            var stopWaitHandle = new AutoResetEvent(false);

            bool addedBelowThresholdTrace = false;
            // our threads have callbacks you can hook into if desired.

            AssertFailedException failedAssert = null;
            var threadObject = new ThreadObject
            {
                ThreadStartObject = 5,

                // create a callback that asserts our data,  and unblocks our wait
                ThreadCallback = delegate (object o)
                {
                    try
                    {
                        if (!addedBelowThresholdTrace)
                        {
                            var id = Guid.NewGuid().ToString();
                            testTraces.Add(id, new HttpTrace
                            {
                                Id = id,
                                RemoteUri = new Uri("http://testsite.com/"),
                                RequestDate = DateTime.Now.AddMinutes(-3)
                            });
                            addedBelowThresholdTrace = true;
                            Assert.IsTrue(gui.TraceViewModel.NotificationHistory.Count == 0);
                            return true;
                        }

                        Assert.IsNotNull(gui.TraceViewModel.NotificationHistory.Peek());
                        stopWaitHandle.Set();
                        return true;
                    } catch (AssertFailedException ex)
                    {
                        failedAssert = ex;
                        stopWaitHandle.Set();
                        return true;
                    }
                }
            };

            // call the method we want to test.
            service.MonitorHighTraffic(threadObject);

            // wait for the OK from our monitor high traffic thread
            // timeout of 10 seconds.
            stopWaitHandle.WaitOne(10 * 1000);

            if (failedAssert != null)
            {
                Assert.Fail(failedAssert.Message);
            }
        }
    }
}
