using NUnit.Framework;
using UnityEngine;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ServiceInjectorTests
    {
        private class MyMockService : ILocatableService
        {
            public bool MyBool { get; set; }
        }
        
        private class MyMockComplexService : ILocatableService
        {
            public int MyInt { get; set; }
        }
        
        private class MyMockOptionalSubscriber
        {
            [Inject] public MyMockService MyService;
        }
        
        private class MyMockRequiredSubscriber
        {
            [Inject(InjectorFlags.ExceptionIfMissing)] public MyMockService MyService;
        }
        
        private class MyMockAsynchronousSubscriber
        {
            [Inject(InjectorFlags.Asynchronous)] public MyMockService MyService;
        }
        
        private class MyMockMethodSubscriber
        {
            private MyMockService _myService;
            
            [Inject] public void InjectService(MyMockService myService) {
                Debug.Log("Injecting service");
                _myService = myService;
            }
            
            public MyMockService GetService() {
                return _myService;
            }
        }
        
        private class MyMockPropertySubscriber
        {
            [Inject] public MyMockService MyService { get; set; }
        }

        private class MyMockComplexMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyMockComplexService MyComplexService { get; private set; }
            
            [Inject] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {
                MyService = myService;
                MyComplexService = myComplexService;
            }
        }
        
        private class MyMockComplexRequiredMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyMockComplexService MyComplexService { get; private set; }
            
            [Inject(InjectorFlags.ExceptionIfMissing)] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {
                MyService = myService;
                MyComplexService = myComplexService;
            }
        }
        
        private class MyMockComplexAsyncMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyMockComplexService MyComplexService { get; private set; }
            
            [Inject(InjectorFlags.Asynchronous)] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {
                MyService = myService;
                MyComplexService = myComplexService;
            }
        }
        
        [Test]
        public void TestInjectingOptionalDependency() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockOptionalSubscriber();
            var service = new MyMockService();
            
            ServiceInjector.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingRequiredDependency() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockRequiredSubscriber();
            var service = new MyMockService();
            
            Assert.Throws<System.Exception>(() => ServiceInjector.Inject(subscriber));
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingAsynchronousDependency() {
            ServiceLocator.ClearServices();
                
            var subscriber = new MyMockAsynchronousSubscriber();
            var service = new MyMockService();
            
            ServiceInjector.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            ServiceLocator.RegisterService(service);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingMethodDependency() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockMethodSubscriber();
            var service = new MyMockService();
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.GetService());
        }
        
        [Test]
        public void TestInjectingPropertyDependency() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockPropertySubscriber();
            var service = new MyMockService();
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestComplexMethodInjection() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockComplexMethodSubscriber();
            var service = new MyMockService();
            var complexService = new MyMockComplexService();
            
            ServiceLocator.RegisterService(service);
            ServiceLocator.RegisterService(complexService);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyComplexService);
        }
        
        [Test]
        public void TestAsyncComplexMethodInjection() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockComplexAsyncMethodSubscriber();
            var service = new MyMockService();
            var complexService = new MyMockComplexService();
            
            ServiceInjector.Inject(subscriber);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyComplexService);
            
            ServiceLocator.RegisterService(service);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyComplexService);
            
            ServiceLocator.RegisterService(complexService);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyComplexService);
        }
        
        [Test]
        public void TestExceptionComplexMethodInjection() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockComplexRequiredMethodSubscriber();
            var service = new MyMockService();
            var complexService = new MyMockComplexService();
            
            ServiceLocator.RegisterService(service);
            
            Assert.Throws<System.Exception>(() => ServiceInjector.Inject(subscriber));
            
            ServiceLocator.RegisterService(complexService);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyComplexService);
        }

    }
}