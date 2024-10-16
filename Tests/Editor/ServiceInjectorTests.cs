using DGP.ServiceLocator.Injectable;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
        
        private class MyMockRequiredSubscriber
        {
            [Inject] public MyMockService MyService;
        }
        
        private class MyMockOptionalSubscriber
        {
            [Inject(InjectorFlags.Optional)] public MyMockService MyService;
        }
        
        
        private class MyMockAsynchronousSubscriber
        {
            [Inject(InjectorFlags.Asynchronous)] public MyMockService MyService;
        }
        
        private class MyMockMethodSubscriber
        {
            private MyMockService _myService;
            public MyMockService GetService() => _myService;
            
            [Inject] public void InjectService(MyMockService myService) {
                Debug.Log("Injecting service");
                _myService = myService;
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
            
            [Inject(InjectorFlags.Optional)] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {
                MyService = myService;
                MyComplexService = myComplexService;
            }
        }
        
        private class MyMockComplexRequiredMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyMockComplexService MyComplexService { get; private set; }
            
            [Inject] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {
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
        
        private interface IAmMockService : ILocatableService
        {
            public void DoSomething();
        }
        
        private class MockInterfacedService : IAmMockService
        {
            public int TimesCalled { get; private set; }
            public void DoSomething() => TimesCalled++;
        }

        private class MockInterfaceSubscriber
        {
            [Inject] public IAmMockService MyService;
            
            public IAmMockService PublicService;
            
            [Inject] public void InjectService(IAmMockService myService) => PublicService = myService;
        }

        private class MockIrreplacableSubscriber
        {
            [Inject(InjectorFlags.DontReplace)] public MyMockService MyService;
            [Inject(InjectorFlags.DontReplace)] public MyMockService MyService2 { get; set; }
        }
        
        [Test]
        public void TestInjectingOptionalDependency() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var subscriber = new MyMockOptionalSubscriber();
            
            ServiceInjector.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingRequiredDependency() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var subscriber = new MyMockRequiredSubscriber();
            
            Assert.Throws<System.Exception>(() => ServiceInjector.Inject(subscriber));
            
            ServiceLocator.RegisterService(service);
            ServiceInjector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingAsynchronousDependency() {
            ServiceLocator.ClearServices();
                
            var service = new MyMockService();
            var subscriber = new MyMockAsynchronousSubscriber();
            
            ServiceInjector.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            ServiceLocator.RegisterService(service);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingMethodDependency() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var subscriber = new MyMockMethodSubscriber();
            
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
            
            var service = new MyMockService();
            var complexService = new MyMockComplexService();
            var subscriber = new MyMockComplexAsyncMethodSubscriber();
            
            ServiceInjector.Inject(subscriber);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyComplexService);
            
            ServiceLocator.RegisterService(service);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyComplexService);
            
            ServiceLocator.RegisterService(complexService);
            
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

        [Test]
        public void TestInterfaceInjections() {
            ServiceLocator.ClearServices();
            
            MockInterfacedService service = new MockInterfacedService();
            MockInterfaceSubscriber subscriber = new MockInterfaceSubscriber();
            
            ServiceLocator.RegisterService<IAmMockService>(service);
            ServiceInjector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(service, subscriber.PublicService);
        }
        
        [Test]
        public void TestIrreplacableInjection() {
            ServiceLocator.ClearServices();
            
            MyMockService service = new MyMockService();

            
            MyMockService service3 = new MyMockService();
            MyMockService service4 = new MyMockService();
            
            MockIrreplacableSubscriber subscriber = new MockIrreplacableSubscriber();
            MockIrreplacableSubscriber subscriber2 = new MockIrreplacableSubscriber();
            
            subscriber.MyService = service3;
            subscriber.MyService2 = service4;
            
            ServiceLocator.RegisterService(service);
            
            ServiceInjector.Inject(subscriber);
            ServiceInjector.Inject(subscriber2);
            
            Assert.AreSame(service3, subscriber.MyService);
            Assert.AreSame(service4, subscriber.MyService2);
            
            Assert.AreSame(service, subscriber2.MyService);
            Assert.AreSame(service, subscriber2.MyService2);
        }

    }
}