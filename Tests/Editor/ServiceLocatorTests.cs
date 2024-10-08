using System;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ServiceLocatorTests
    {
        private class MyMockService : ILocatableService
        {
            public bool MyBool { get; set; }
        }
    
        [Test]
        public void TestSynchronousLocating() {
            var myService = new MyMockService();
            
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.GetService<MyMockService>());
            
            ServiceLocator.RegisterService(myService);
        
            var locatedService = ServiceLocator.GetService<MyMockService>();
        
            Assert.AreSame(myService, locatedService);
            ServiceLocator.ClearServices();
        }

        [Test]
        public void TestClearServices() {
            var myService = new MyMockService();
            ServiceLocator.RegisterService(myService);
            ServiceLocator.ClearServices();
        
            if (ServiceLocator.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should not be located");
            }
        
            ServiceLocator.ClearServices();
        }

        [Test]
        public void TestAsynchronousLocating() {
            var myService = new MyMockService();
        
            int callbackCount = 0;
        
            ServiceLocator.LocateServiceAsync<MyMockService>((service) => {
                Assert.AreSame(myService, service);
                callbackCount++;
            });
        
            ServiceLocator.RegisterService(myService);
        
            Assert.AreEqual(1, callbackCount);
        
            ServiceLocator.ClearServices();
        }

        [Test]
        public void TestTryLocating() {
            var myService = new MyMockService();
        
            if (ServiceLocator.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should not be located");
            }
        
            ServiceLocator.RegisterService(myService);
        
            if (!ServiceLocator.TryLocateService<MyMockService>(out locatedService)) {
                Assert.Fail("Service should be located");
            }
        
            ServiceLocator.ClearServices();
        }

        [Test]
        public void TestDeregistration() {
            var myService = new MyMockService();
            ServiceLocator.RegisterService(myService);
        
            if (!ServiceLocator.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should be located");
            }
        
            ServiceLocator.DeregisterService<MyMockService>();
        
            if (ServiceLocator.TryLocateService<MyMockService>(out locatedService)) {
                Assert.Fail("Service should not be located");
            }
        
            ServiceLocator.ClearServices();
        }

    }
}
