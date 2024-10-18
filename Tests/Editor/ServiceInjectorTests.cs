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

        private class MockConstructableSubscriber
        {
            public readonly MyMockService MyService;
            
            [Inject] public MockConstructableSubscriber(MyMockService myService) {
                MyService = myService;
            }
        }

        private class MockIrreplacableSubscriber
        {
            [Inject(InjectorFlags.DontReplace)] public MyMockService MyService;
            [Inject(InjectorFlags.DontReplace)] public MyMockService MyService2 { get; set; }
        }

        private class MockParentClass
        {
            [Provide] public readonly MyMockService MyMockService = new();
            public MockChildClass CreateChild() => this.InjectLocalServices(new MockChildClass());
        }
        
        private class MockChildClass : MockParentClass
        {
            [Inject] public MyMockService MockService;
        }

        private interface IMockService : ILocatableService { }
        private class InterfacedServiceA : IMockService { }
        
        private class MockedInterfaceSubscriber
        {
            [Inject(serviceType:typeof(InterfacedServiceA))] public IMockService MyService;
        }

        [Test]
        public void TestInjectionTypeSpecification() {
            ServiceLocator.ClearServices();
            
            InterfacedServiceA service = new();
            MockedInterfaceSubscriber subscriber = new();
            
            ServiceLocator.RegisterService(service);
            ServiceLocator.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }

        [Test]
        public void TestLocalServiceInjection() {
            ServiceLocator.ClearServices();
            
            MockParentClass parent = new MockParentClass();
            var child = parent.CreateChild();
            
            Assert.AreSame(child.MockService, parent.MyMockService);
        }
        
        [Test]
        public void TestInjectingOptionalDependency() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var subscriber = new MyMockOptionalSubscriber();
            
            ServiceLocator.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            ServiceLocator.RegisterService(service);
            ServiceLocator.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingRequiredDependency() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var subscriber = new MyMockRequiredSubscriber();
            
            Assert.Throws<System.Exception>(() => ServiceLocator.Inject(subscriber));
            
            ServiceLocator.RegisterService(service);
            ServiceLocator.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingAsynchronousDependency() {
            ServiceLocator.ClearServices();
                
            var service = new MyMockService();
            var subscriber = new MyMockAsynchronousSubscriber();
            
            ServiceLocator.Inject(subscriber);
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
            ServiceLocator.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.GetService());
        }
        
        [Test]
        public void TestInjectingPropertyDependency() {
            ServiceLocator.ClearServices();
            
            var subscriber = new MyMockPropertySubscriber();
            var service = new MyMockService();
            
            ServiceLocator.RegisterService(service);
            ServiceLocator.Inject(subscriber);
            
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
            ServiceLocator.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyComplexService);
        }
        
        [Test]
        public void TestAsyncComplexMethodInjection() {
            ServiceLocator.ClearServices();
            
            var service = new MyMockService();
            var complexService = new MyMockComplexService();
            var subscriber = new MyMockComplexAsyncMethodSubscriber();
            
            ServiceLocator.Inject(subscriber);
            
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
            
            Assert.Throws<System.Exception>(() => ServiceLocator.Inject(subscriber));
            
            ServiceLocator.RegisterService(complexService);
            ServiceLocator.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyComplexService);
        }

        [Test]
        public void TestInterfaceInjections() {
            ServiceLocator.ClearServices();
            
            MockInterfacedService service = new MockInterfacedService();
            MockInterfaceSubscriber subscriber = new MockInterfaceSubscriber();
            
            ServiceLocator.RegisterService<IAmMockService>(service);
            ServiceLocator.Inject(subscriber);
            
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
            
            ServiceLocator.Inject(subscriber);
            ServiceLocator.Inject(subscriber2);
            
            Assert.AreSame(service3, subscriber.MyService);
            Assert.AreSame(service4, subscriber.MyService2);
            
            Assert.AreSame(service, subscriber2.MyService);
            Assert.AreSame(service, subscriber2.MyService2);
        }
        
        [Test]
        public void TestConstructableInjection() {
            ServiceLocator.ClearServices();
            
            MyMockService service = new MyMockService();

            var subscriber = ServiceLocator.Injector.CreateAndInject<MockConstructableSubscriber>();
            
            Assert.IsNull(subscriber);
            
            ServiceLocator.RegisterService(service);
            
            subscriber = ServiceLocator.Injector.CreateAndInject<MockConstructableSubscriber>();
            
            Assert.IsNotNull(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }

    }
}