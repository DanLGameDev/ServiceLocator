using System;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ServiceContainerTests
    {
        private class MyMockService
        {
        }
    
        [Test]
        public void TestSynchronousLocating() {
            var container = new ServiceContainer();
            var myService = new MyMockService();
            
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.GetService<MyMockService>());
            
            container.RegisterService(myService);
        
            var locatedService = container.GetService<MyMockService>();
        
            Assert.AreSame(myService, locatedService);
        }

        [Test]
        public void TestClearServices() {
            var container = new ServiceContainer();
            
            var myService = new MyMockService();
            container.RegisterService(myService);
            container.ClearServices();
        
            if (container.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should not be located");
            }
        }

        [Test]
        public void TestAsynchronousLocating() {
            var container = new ServiceContainer();
            
            var myService = new MyMockService();
        
            int callbackCount = 0;
        
            container.LocateServiceAsync<MyMockService>((service) => {
                Assert.AreSame(myService, service);
                callbackCount++;
            });
        
            container.RegisterService(myService);
        
            Assert.AreEqual(1, callbackCount);
        }

        [Test]
        public void TestTryLocating() {
            var container = new ServiceContainer();
            
            var myService = new MyMockService();
        
            if (container.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should not be located");
            }
        
            container.RegisterService(myService);
        
            if (!container.TryLocateService<MyMockService>(out locatedService)) {
                Assert.Fail("Service should be located");
            }
        }

        [Test]
        public void TestDeregister() {
            var container = new ServiceContainer();
            
            var myService = new MyMockService();
            container.RegisterService(myService);
        
            if (!container.TryLocateService<MyMockService>(out var locatedService)) {
                Assert.Fail("Service should be located");
            }
        
            container.DeregisterService<MyMockService>();
        
            if (container.TryLocateService<MyMockService>(out locatedService)) {
                Assert.Fail("Service should not be located");
            }
        }
    }
}