using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class HierarchicalContainerTests
    {
        private class TestService
        {
        } 
        
        [Test]
        public void TestGlobalFirstSearch() {
            ServiceLocator.ClearServices();

            var childContainer = new ServiceContainer(ServiceLocator.Container);
            
            var myServiceInGlobal = new TestService();
            
            ServiceLocator.RegisterService(myServiceInGlobal);
            
            Assert.AreEqual(ServiceLocator.TryLocateService<TestService>(out var locatedService, ServiceSearchMode.GlobalFirst), true);
            Assert.AreEqual(locatedService, myServiceInGlobal);
            
            Assert.AreEqual(childContainer.TryLocateService<TestService>(out locatedService, ServiceSearchMode.GlobalFirst), true);
            Assert.AreEqual(locatedService, myServiceInGlobal);
            
            // should not be found in child container but then found in global
            Assert.AreEqual(childContainer.TryLocateService<TestService>(out locatedService, ServiceSearchMode.LocalFirst), true);
            Assert.AreEqual(locatedService, myServiceInGlobal);
            
            //should be false since not in child container
            Assert.AreEqual(childContainer.TryLocateService<TestService>(out locatedService, ServiceSearchMode.LocalOnly), false);
            
            var myServiceInChild = new TestService();
            childContainer.RegisterService(myServiceInChild);
            
            // should be found in child container
            Assert.AreEqual(childContainer.TryLocateService<TestService>(out locatedService, ServiceSearchMode.LocalFirst), true);
            Assert.AreEqual(locatedService, myServiceInChild);
            
            Assert.AreEqual(childContainer.TryLocateService<TestService>(out locatedService, ServiceSearchMode.LocalOnly), true);
            Assert.AreEqual(locatedService, myServiceInChild);
        }
        
        [Test]
        public void TestAsyncLocating() {
            ServiceLocator.ClearServices();

            var myService = new TestService();
            var myContainer = new ServiceContainer(ServiceLocator.Container);
            
            int callbackCount = 0;
            myContainer.LocateServiceAsync<TestService>((service) => {
                Assert.AreEqual(service, myService);
                callbackCount++;
            });
            
            Assert.AreEqual(callbackCount, 0);
            
            ServiceLocator.RegisterService(myService);
            
            Assert.AreEqual(callbackCount, 1);
            
        }
        
    }
}