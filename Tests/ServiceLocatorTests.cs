using System.Collections;
using DGP.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ServiceLocatorTests
{
    private class MyTestService : ILocatableService
    {
        public bool MyBool { get; set; }
    }
    
    [Test]
    public void TestSynchronousLocating() {
        var myService = new MyTestService();
        ServiceLocator.RegisterService(myService);
        
        var locatedService = ServiceLocator.LocateService<MyTestService>();
        
        Assert.AreSame(myService, locatedService);
        
        ServiceLocator.ClearServices();
    }

    [Test]
    public void TestClearServices() {
        var myService = new MyTestService();
        ServiceLocator.RegisterService(myService);
        ServiceLocator.ClearServices();
        
        var locatedService = ServiceLocator.LocateService<MyTestService>();
        
        Assert.IsNull(locatedService);
        
        ServiceLocator.ClearServices();
    }

    [Test]
    public void TestAsynchronousLocating() {
        var myService = new MyTestService();
        
        int callbackCount = 0;
        
        ServiceLocator.LocateServiceAsync<MyTestService>((service) => {
            Assert.AreSame(myService, service);
            callbackCount++;
        });
        
        ServiceLocator.RegisterService(myService);
        
        Assert.AreEqual(1, callbackCount);
        
        ServiceLocator.ClearServices();
    }

    [Test]
    public void TestTryLocating() {
        var myService = new MyTestService();
        
        if (ServiceLocator.TryLocateService<MyTestService>(out var locatedService)) {
            Assert.Fail("Service should not be located");
        }
        
        ServiceLocator.RegisterService(myService);
        
        if (!ServiceLocator.TryLocateService<MyTestService>(out locatedService)) {
            Assert.Fail("Service should be located");
        }
        
        ServiceLocator.ClearServices();
    }

    [Test]
    public void TestDeregistration() {
        var myService = new MyTestService();
        ServiceLocator.RegisterService(myService);
        
        if (!ServiceLocator.TryLocateService<MyTestService>(out var locatedService)) {
            Assert.Fail("Service should be located");
        }
        
        ServiceLocator.DeregisterService<MyTestService>();
        
        if (ServiceLocator.TryLocateService<MyTestService>(out locatedService)) {
            Assert.Fail("Service should not be located");
        }
        
        ServiceLocator.ClearServices();
    }

}
