using System;
using DGP.ServiceLocator.Injectable;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ServiceInjectorTests
    {
        private interface IAmMockService
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
            [Inject(flags: InjectorFlags.Optional)] public MyMockService MyServiceField;
            [Inject(flags: InjectorFlags.Optional)] public MyMockService MyServiceProperty { get; set; }
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
            
            [Inject(flags: InjectorFlags.Optional)] public void InjectServices(MyMockService myService, MyOtherMockService myOtherService)
            {
                MyService = myService;
                MyOtherService = myOtherService;
            }
        }
        
        private class MyMockComplexRequiredMethodSubscriber
        {
            public MyMockService MyService { get; private set; }
            public MyOtherMockService MyOtherService { get; private set; }
            
            [Inject] public void InjectServices(MyMockService myService, MyOtherMockService myOtherService)
            {
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
            
            [Inject] public MockConstructableSubscriber(MyMockService myService)
            {
                MyService = myService;
            }
        }

        private class MockIrreplacableSubscriber
        {
            [Inject(flags: InjectorFlags.DontReplace)] public MyMockService MyService;
            [Inject(flags: InjectorFlags.DontReplace)] public MyMockService MyService2 { get; set; }
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

        private interface IMockService { }
        private class InterfacedServiceA : IMockService { }
        
        private class MockedInterfaceSubscriber
        {
            [Inject(serviceType: typeof(InterfacedServiceA))] public IMockService MyService;
        }
        
        private class MockUnmarkedSubscriber
        {
            public MyMockService MyService;
            
            public MockUnmarkedSubscriber(MyMockService myService)
            {
                MyService = myService;
            }
        }

        [Test]
        public void TestUnmarkedConstructAndInject()
        {
            ServiceContainer container = new();
            
            MyMockService service = new();
            
            Assert.Throws<MissingDependencyException>(() => container.Injector.Create<MockUnmarkedSubscriber>());
            Assert.IsFalse(container.Injector.TryCreate<MockUnmarkedSubscriber>(out var returnedService));
            Assert.IsNull(returnedService);
            
            container.RegisterService(service);
            
            MockUnmarkedSubscriber subscriber = container.Injector.Create<MockUnmarkedSubscriber>();
            
            Assert.IsNotNull(subscriber);
            Assert.AreSame(service, subscriber.MyService);
            Assert.IsTrue(container.Injector.TryCreate<MockUnmarkedSubscriber>(out returnedService));
            Assert.IsNotNull(returnedService);
        }

        [Test]
        public void TestInjectionTypeSpecification()
        {
            ServiceContainer container = new();
            
            InterfacedServiceA service = new();
            MockedInterfaceSubscriber subscriber = new();
            
            // This will fail because the subscriber has specifically asked for something registered as InterfacedServiceA
            container.RegisterService<IMockService>(service);
            Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(subscriber));
            
            // This will pass since we allow the service to use its normal type
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestInjectingOptionalDependency()
        {
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
        public void TestInjectingRequiredDependency()
        {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var subscriber = new MyMockRequiredSubscriber();
            
            Assert.Throws<MissingDependencyException>(() => ServiceLocator.Injector.Inject(subscriber));
            
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            Assert.AreSame(service, subscriber.MyServiceProperty);
            Assert.AreSame(service, subscriber.MyServiceField);
        }
        
        [Test]
        public void TestInjectingMethodDependency()
        {
            ServiceContainer container = new();
            
            var service = new MyMockService();
            var subscriber = new MyMockMethodSubscriber();
            
            container.RegisterService(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
        }
        
        [Test]
        public void TestComplexMethodInjection()
        {
            ServiceContainer container = new();
            
            var optionalSubscriber = new MyMockComplexOptionalMethodSubscriber();
            var requiredSubscriber = new MyMockComplexRequiredMethodSubscriber();
            
            var service = new MyMockService();
            var complexService = new MyOtherMockService();
            
            container.Injector.Inject(optionalSubscriber);
            Assert.IsNull(optionalSubscriber.MyService);
            
            Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(requiredSubscriber));
            
            container.RegisterService(service);
            container.RegisterService(complexService);
            
            container.Injector.Inject(optionalSubscriber);
            Assert.AreSame(service, optionalSubscriber.MyService);
            Assert.AreSame(complexService, optionalSubscriber.MyOtherService);
            
            container.Injector.Inject(requiredSubscriber);
            Assert.AreSame(service, requiredSubscriber.MyService);
            Assert.AreSame(complexService, requiredSubscriber.MyOtherService);
        }
        
        [Test]
        public void TestExceptionComplexMethodInjection()
        {
            ServiceContainer container = new();
            
            var subscriber = new MyMockComplexRequiredMethodSubscriber();
            var service = new MyMockService();
            var complexService = new MyOtherMockService();
            
            container.RegisterService(service);
            
            Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(subscriber));
            
            container.RegisterService(complexService);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(complexService, subscriber.MyOtherService);
        }

        [Test]
        public void TestInterfaceInjections()
        {
            ServiceContainer container = new();
            
            MockInterfacedService service = new MockInterfacedService();
            MockInterfaceSubscriber subscriber = new MockInterfaceSubscriber();
            
            container.RegisterService<IAmMockService>(service);
            container.Injector.Inject(subscriber);
            
            Assert.AreSame(service, subscriber.MyService);
            Assert.AreSame(service, subscriber.PublicService);
        }
        
        [Test]
        public void TestIrreplacableInjection()
        {
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
        public void TestConstructableInjection()
        {
            ServiceContainer container = new();
            
            MyMockService service = new MyMockService();

            Assert.IsFalse(container.Injector.TryCreate<MockConstructableSubscriber>(out var subscriber));
            Assert.IsNull(subscriber);
            
            container.RegisterService(service);
            
            subscriber = container.Injector.Create<MockConstructableSubscriber>();
            
            Assert.IsNotNull(subscriber);
            Assert.AreSame(service, subscriber.MyService);
        }

        private class MockMarkedConstructorSubscriber
        {
            public readonly bool UsedMarkedConstructor;

            public MockMarkedConstructorSubscriber(MyMockService myService, MyOtherMockService myOtherService) { }

            [Inject] public MockMarkedConstructorSubscriber(MyMockService myService)
            {
                UsedMarkedConstructor = true;
            }
        }

        [Test]
        public void TestMarkedConstructorWinsOverGreedierUnmarked()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());
            container.RegisterService(new MyOtherMockService());

            var subscriber = container.Injector.Create<MockMarkedConstructorSubscriber>();

            Assert.IsTrue(subscriber.UsedMarkedConstructor);
        }

        private class MockGreedyConstructorSubscriber
        {
            public readonly int ParameterCount;

            public MockGreedyConstructorSubscriber() { }

            public MockGreedyConstructorSubscriber(MyMockService myService)
            {
                ParameterCount = 1;
            }

            public MockGreedyConstructorSubscriber(MyMockService myService, MyOtherMockService myOtherService)
            {
                ParameterCount = 2;
            }
        }

        [Test]
        public void TestGreediestSatisfiableUnmarkedConstructorWins()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());
            Assert.AreEqual(1, container.Injector.Create<MockGreedyConstructorSubscriber>().ParameterCount);

            container.RegisterService(new MyOtherMockService());
            Assert.AreEqual(2, container.Injector.Create<MockGreedyConstructorSubscriber>().ParameterCount);
        }

        private class MockDoubleMarkedConstructorSubscriber
        {
            [Inject] public MockDoubleMarkedConstructorSubscriber() { }
            [Inject] public MockDoubleMarkedConstructorSubscriber(MyMockService myService) { }
        }

        [Test]
        public void TestSeveralMarkedConstructorsThrow()
        {
            ServiceContainer container = new();

            Assert.Throws<InvalidOperationException>(() => container.Injector.Create<MockDoubleMarkedConstructorSubscriber>());
        }

        private class MockCreatedWithMembersSubscriber
        {
            public readonly MyMockService ConstructorService;

            [Inject] public MyOtherMockService FieldService;

            [Inject] public MockCreatedWithMembersSubscriber(MyMockService myService)
            {
                ConstructorService = myService;
            }
        }

        [Test]
        public void TestCreateInjectsMembers()
        {
            ServiceContainer container = new();

            var service = container.RegisterService(new MyMockService());

            Assert.IsFalse(container.Injector.TryCreate<MockCreatedWithMembersSubscriber>(out _));
            Assert.Throws<MissingDependencyException>(() => container.Injector.Create<MockCreatedWithMembersSubscriber>());

            var otherService = container.RegisterService(new MyOtherMockService());
            var subscriber = container.Injector.Create<MockCreatedWithMembersSubscriber>();

            Assert.AreSame(service, subscriber.ConstructorService);
            Assert.AreSame(otherService, subscriber.FieldService);
        }

        private class MockBaseWithPrivateInject
        {
            [Inject] private MyMockService _baseService;
            public MyMockService BaseService => _baseService;
        }

        private class MockDerivedFromPrivateInject : MockBaseWithPrivateInject
        {
            [Inject] public MyOtherMockService DerivedService;
        }

        [Test]
        public void TestInheritedPrivateMembersAreInjected()
        {
            ServiceContainer container = new();

            var service = container.RegisterService(new MyMockService());
            var otherService = container.RegisterService(new MyOtherMockService());
            var subscriber = new MockDerivedFromPrivateInject();

            container.Injector.Inject(subscriber);

            Assert.AreSame(service, subscriber.BaseService);
            Assert.AreSame(otherService, subscriber.DerivedService);
        }

        private class MockVirtualMethodBase
        {
            public int TimesInjected;
            [Inject] public virtual void Setup(MyMockService myService) => TimesInjected++;
        }

        private class MockVirtualMethodOverride : MockVirtualMethodBase
        {
            public bool OverrideCalled;

            [Inject] public override void Setup(MyMockService myService)
            {
                OverrideCalled = true;
                base.Setup(myService);
            }
        }

        [Test]
        public void TestOverriddenMethodIsInjectedOnce()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());

            var subscriber = new MockVirtualMethodOverride();
            container.Injector.Inject(subscriber);

            Assert.IsTrue(subscriber.OverrideCalled);
            Assert.AreEqual(1, subscriber.TimesInjected);
        }

        private class MockOrderedSubscriber
        {
            public bool MembersWereSetBeforeMethod;

            [Inject] public void Setup(MyOtherMockService myOtherService) => MembersWereSetBeforeMethod = FieldService != null && PropertyService != null;

            [Inject] public MyMockService FieldService;
            [Inject] public MyMockService PropertyService { get; set; }
        }

        [Test]
        public void TestMembersAreInjectedBeforeMethods()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());
            container.RegisterService(new MyOtherMockService());

            var subscriber = new MockOrderedSubscriber();
            container.Injector.Inject(subscriber);

            Assert.IsTrue(subscriber.MembersWereSetBeforeMethod);
        }

        [Test]
        public void TestFailedInjectionWritesNothing()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());

            var subscriber = new MyMockComplexRequiredMethodSubscriber();
            var fieldSubscriber = new MockDerivedFromPrivateInject();

            Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(subscriber));
            Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(fieldSubscriber));

            Assert.IsNull(subscriber.MyService);
            Assert.IsNull(fieldSubscriber.BaseService);
        }

        [Test]
        public void TestMissingDependencyDescribesTheGap()
        {
            ServiceContainer container = new();

            var exception = Assert.Throws<MissingDependencyException>(() => container.Injector.Inject(new MockDerivedFromPrivateInject()));

            Assert.AreEqual(typeof(MockDerivedFromPrivateInject), exception.TargetType);
            Assert.AreEqual("_baseService", exception.MemberName);
            Assert.AreEqual(typeof(MyMockService), exception.ServiceType);
        }

        private class MockReadonlyFieldSubscriber
        {
            [Inject] public readonly MyMockService MyService;
        }

        private class MockGetterOnlySubscriber
        {
            [Inject] public MyMockService MyService { get; }
        }

        private class MockUnassignableTypeSubscriber
        {
            [Inject(typeof(MyOtherMockService))] public MyMockService MyService;
        }

        private class MockMethodWithServiceTypeSubscriber
        {
            [Inject(typeof(MyMockService))] public void Setup(MyMockService myService) { }
        }

        [Test]
        public void TestInvalidInjectionPointsThrow()
        {
            ServiceContainer container = new();

            container.RegisterService(new MyMockService());
            container.RegisterService(new MyOtherMockService());

            Assert.Throws<InvalidOperationException>(() => container.Injector.Inject(new MockReadonlyFieldSubscriber()));
            Assert.Throws<InvalidOperationException>(() => container.Injector.Inject(new MockGetterOnlySubscriber()));
            Assert.Throws<InvalidOperationException>(() => container.Injector.Inject(new MockUnassignableTypeSubscriber()));
            Assert.Throws<InvalidOperationException>(() => container.Injector.Inject(new MockMethodWithServiceTypeSubscriber()));
        }
    }
}