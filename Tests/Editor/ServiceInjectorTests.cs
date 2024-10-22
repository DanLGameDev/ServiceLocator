using System;
using DGP.ServiceLocator.Injectable;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ServiceInjectorTests
    {
        private interface IAmMockService : ILocatableService
        {
            public void DoSomething();
        }
        
        private class MyMockService : IAmMockService
        {
            public bool MyBool { get; set; }
            public void DoSomething() { }
        }
        
        private class MyOtherMockService : IAmMockService
        {
            public int MyInt { get; set; }
            public void DoSomething() { }
        }
        
        private class MyMockRequiredSubscriber
        {
            [Inject] public MyMockService MyServiceField;
            [Inject] public MyMockService MyServiceProperty { get; set; }
        }
        
        private class MyMockOptionalSubscriber
        {
            [Inject(InjectorFlags.Optional)] public MyMockService MyServiceField;
            [Inject(InjectorFlags.Optional)] public MyMockService MyServiceProperty { get; set; }
            
        }
        
        private class MyMockAsynchronousSubscriber
        {
            [Inject(InjectorFlags.Asynchronous)] public MyMockService MyService;
            [Inject(InjectorFlags.Asynchronous)] public MyMockService MyServiceProperty { get; set; }
        }
        
        private class MyMockMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            [Inject] public void InjectService(MyMockService myService) => MyService = myService;
        }

        private class MyMockComplexOptionalMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyOtherMockService MyOtherService { get; private set; }
            
            [Inject(InjectorFlags.Optional)] public void InjectServices(MyMockService myService, MyOtherMockService myOtherService) {
                MyService = myService;
                MyOtherService = myOtherService;
            }
        }
        
        private class MyMockComplexRequiredMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyOtherMockService MyOtherService { get; private set; }
            
            [Inject] public void InjectServices(MyMockService myService, MyOtherMockService myOtherService) {
                MyService = myService;
                MyOtherService = myOtherService;
            }
        }
        
        private class MyMockComplexAsyncMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyOtherMockService MyOtherService { get; private set; }
            
            [Inject(InjectorFlags.Asynchronous)] public void InjectServices(MyMockService myService, MyOtherMockService myOtherService) {
                MyService = myService;
                MyOtherService = myOtherService;
            }
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
        
        private class MockUnmarkedSubscriber
        {
            public MyMockService MyService;
            
            public MockUnmarkedSubscriber(MyMockService myService) {
                MyService = myService;
            }
        }

        // This test passes if the CreateAndInject method is able to create an instance of a class with an unmarked
        // constructor and inject the required dependencies.
        [Test]
        public void TestUnmarkedConstructAndInject() {
            ServiceContainer container = new();
            
            MyMockService service = new();
            
            Assert.IsNull(container.Injector.CreateAndInject<MockUnmarkedSubscriber>());
            Assert.IsFalse(container.Injector.TryCreateAndInject<MockUnmarkedSubscriber>(out var returnedService));
            Assert.IsNull(returnedService);
            
            container.RegisterService(service);
            
            MockUnmarkedSubscriber subscriber = container.Injector.CreateAndInject<MockUnmarkedSubscriber>();
            
            Assert.IsNotNull(subscriber);
            Assert.AreSame(service, subscriber.MyService);
            Assert.IsTrue(container.Injector.TryCreateAndInject<MockUnmarkedSubscriber>(out returnedService));
            Assert.IsNotNull(returnedService);
        }

        [Test]
        public void TestInjectionTypeSpecification() {
            ServiceContainer container = new();
            
            InterfacedServiceA service = new();
            MockedInterfaceSubscriber subscriber = new();
            
            // This will fail because the subscriber has specifically asked for something registered as InterfacedServiceA
            container.RegisterService<IMockService>(service);
            Assert.Throws<Exception>(() => container.Injector.Inject(subscriber));
            
            // This will pass since we allow the service to use its normal type
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }

        [Test]
        public void TestLocalServiceInjection() {
            ServiceContainer container = new();
            
            MockParentClass parent = new MockParentClass();
            var child = parent.CreateChild();
            
            Assert.AreSame(child.MockService, parent.MyMockService);
        }
        
        [Test]
        public void TestInjectingOptionalDependency() {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var subscriber = new MyMockOptionalSubscriber();
            
            container.Injector.Inject(subscriber);
            Assert.IsNull(subscriber.MyServiceField);
            Assert.IsNull(subscriber.MyServiceProperty);
            
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyServiceField);
            Assert.AreSame(service, subscriber.MyServiceProperty);
        }
        
        [Test]
        public void TestInjectingRequiredDependency() {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var subscriber = new MyMockRequiredSubscriber();
            
            Assert.Throws<System.Exception>(() => ServiceLocator.Injector.Inject(subscriber));
            
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyServiceProperty);
            Assert.AreSame(service, subscriber.MyServiceField);
        }
        
        [Test]
        public void TestInjectingAsynchronousDependency() {
            ServiceContainer container = new();
                
            var service = new MyMockService();
            var subscriber = new MyMockAsynchronousSubscriber();
            
            container.Injector.Inject(subscriber);
            Assert.IsNull(subscriber.MyService);
            
            container.RegisterService(service);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingMethodDependency() {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var subscriber = new MyMockMethodSubscriber();
            
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestComplexMethodInjection() {
            ServiceContainer container = new();
            
            var optionalSubscriber = new MyMockComplexOptionalMethodSubscriber();
            var requiredSubscriber = new MyMockComplexRequiredMethodSubscriber();
            var asyncSubscriber = new MyMockComplexAsyncMethodSubscriber();
            
            var service = new MyMockService();
            var complexService = new MyOtherMockService();
            
            container.Injector.Inject(optionalSubscriber);
            Assert.IsNull(optionalSubscriber.MyService);
            
            Assert.Throws<Exception>(() => container.Injector.Inject(requiredSubscriber));
            
            container.Injector.Inject(asyncSubscriber);
            Assert.IsNull(asyncSubscriber.MyService);
            
            container.RegisterService(service);
            container.RegisterService(complexService);
            
            container.Injector.Inject(optionalSubscriber);
            Assert.AreSame(service, optionalSubscriber.MyService);
            Assert.AreSame(complexService, optionalSubscriber.MyOtherService);
            
            container.Injector.Inject(requiredSubscriber);
            Assert.AreSame(service, requiredSubscriber.MyService);
            Assert.AreSame(complexService, requiredSubscriber.MyOtherService);
            
            Assert.AreSame(service, asyncSubscriber.MyService);
            Assert.AreSame(complexService, asyncSubscriber.MyOtherService);
        }
        
        [Test]
        public void TestAsyncComplexMethodInjection() {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var complexService = new MyOtherMockService();
            var subscriber = new MyMockComplexAsyncMethodSubscriber();
            
            container.Injector.Inject(subscriber);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyOtherService);
            
            container.RegisterService(service);
            
            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(subscriber.MyOtherService);
            
            container.RegisterService(complexService);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyOtherService);
        }
        
        [Test]
        public void TestExceptionComplexMethodInjection() {
            ServiceContainer container = new();
            
            var subscriber = new MyMockComplexRequiredMethodSubscriber();
            var service = new MyMockService();
            var complexService = new MyOtherMockService();
            
            container.RegisterService(service);
            
            Assert.Throws<System.Exception>(() => container.Injector.Inject(subscriber));
            
            container.RegisterService(complexService);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyOtherService);
        }

        [Test]
        public void TestInterfaceInjections() {
            ServiceContainer container = new();
            
            MockInterfacedService service = new MockInterfacedService();
            MockInterfaceSubscriber subscriber = new MockInterfaceSubscriber();
            
            container.RegisterService<IAmMockService>(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(service, subscriber.PublicService);
        }
        
        [Test]
        public void TestIrreplacableInjection() {
            ServiceContainer container = new();
            
            MyMockService service = new MyMockService();
            MyMockService service3 = new MyMockService();
            MyMockService service4 = new MyMockService();
            
            MockIrreplacableSubscriber subscriber = new MockIrreplacableSubscriber();
            MockIrreplacableSubscriber subscriber2 = new MockIrreplacableSubscriber();
            
            subscriber.MyService = service3;
            subscriber.MyService2 = service4;
            
            container.RegisterService(service);
            
            container.Injector.Inject(subscriber);
            container.Injector.Inject(subscriber2);
            
            Assert.AreSame(service3, subscriber.MyService);
            Assert.AreSame(service4, subscriber.MyService2);
            
            Assert.AreSame(service, subscriber2.MyService);
            Assert.AreSame(service, subscriber2.MyService2);
        }
        
        [Test]
        public void TestConstructableInjection() {
            ServiceContainer container = new();
            
            MyMockService service = new MyMockService();

            var subscriber = container.Injector.CreateAndInject<MockConstructableSubscriber>();
            
            Assert.IsNull(subscriber);
            
            container.RegisterService(service);
            
            subscriber = container.Injector.CreateAndInject<MockConstructableSubscriber>();
            
            Assert.IsNotNull(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }

    }
}