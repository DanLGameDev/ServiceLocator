using System.Collections;
using DGP.ServiceLocator.Injectable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class HierarchyInjectionTests
    {
        private GameObject _parentObject;
        private GameObject _childObject;

        [SetUp]
        public void Setup()
        {
            _parentObject = new GameObject("Parent");
            _childObject = new GameObject("Child");
            _childObject.transform.SetParent(_parentObject.transform);
        }

        [TearDown]
        public void Teardown()
        {
            if (_parentObject != null)
                Object.DestroyImmediate(_parentObject);
            
            if (_childObject != null)
                Object.DestroyImmediate(_childObject);
            
            ServiceLocator.ClearServices();
        }

        // Test classes
        private class TestService
        {
            public int Value = 42;
        }

        private class ParentProvider : MonoBehaviour
        {
            [Provide] public TestService ProvidedService = new TestService();
        }

        private class ChildConsumer : InjectedMonoBehaviour
        {
            [Inject] public TestService InjectedService;
        }

        private class OptionalChildConsumer : InjectedMonoBehaviour
        {
            [Inject(flags: InjectorFlags.Optional)] public TestService InjectedService;
        }

        private class GlobalOnlyConsumer : InjectedMonoBehaviour
        {
            protected override bool SearchHierarchy => false;
            [Inject] public TestService InjectedService;
        }

        [UnityTest]
        public IEnumerator TestHierarchyInjection()
        {
            // Arrange
            var provider = _parentObject.AddComponent<ParentProvider>();
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act - Awake is called automatically when component is added
            yield return null;

            // Assert
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(provider.ProvidedService, consumer.InjectedService);
            Assert.AreEqual(42, consumer.InjectedService.Value);
        }

        [UnityTest]
        public IEnumerator TestHierarchyInjectionWithServiceProviderComponent()
        {
            // Arrange
            _parentObject.AddComponent<ServiceProviderMonoBehaviour>();
            var provider = _parentObject.AddComponent<ParentProvider>();
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act
            yield return null;

            // Assert
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(provider.ProvidedService, consumer.InjectedService);
        }

        [UnityTest]
        public IEnumerator TestOptionalInjectionWithoutProvider()
        {
            // Arrange
            var consumer = _childObject.AddComponent<OptionalChildConsumer>();

            // Act
            yield return null;

            // Assert - Should not throw, service should be null
            Assert.IsNull(consumer.InjectedService);
        }

        [UnityTest]
        public IEnumerator TestRequiredInjectionThrowsWithoutProvider()
        {
            // Arrange & Act & Assert
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex(".*Missing dependency.*"));
            _childObject.AddComponent<ChildConsumer>();
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestGlobalFallback()
        {
            // Arrange
            var globalService = new TestService { Value = 100 };
            ServiceLocator.RegisterService(globalService);
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act
            yield return null;

            // Assert - Should get service from global ServiceLocator
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(globalService, consumer.InjectedService);
            Assert.AreEqual(100, consumer.InjectedService.Value);
        }

        [UnityTest]
        public IEnumerator TestHierarchyTakesPrecedenceOverGlobal()
        {
            // Arrange
            var hierarchyService = new TestService { Value = 50 };
            var globalService = new TestService { Value = 100 };
            
            var provider = _parentObject.AddComponent<ParentProvider>();
            provider.ProvidedService = hierarchyService;
            
            ServiceLocator.RegisterService(globalService);
            
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act
            yield return null;

            // Assert - Hierarchy should win
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(hierarchyService, consumer.InjectedService);
            Assert.AreEqual(50, consumer.InjectedService.Value);
        }

        [UnityTest]
        public IEnumerator TestGlobalOnlySkipsHierarchy()
        {
            // Arrange
            var hierarchyService = new TestService { Value = 50 };
            var globalService = new TestService { Value = 100 };
            
            var provider = _parentObject.AddComponent<ParentProvider>();
            provider.ProvidedService = hierarchyService;
            
            ServiceLocator.RegisterService(globalService);
            
            var consumer = _childObject.AddComponent<GlobalOnlyConsumer>();

            // Act
            yield return null;

            // Assert - Should get global service, not hierarchy
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(globalService, consumer.InjectedService);
            Assert.AreEqual(100, consumer.InjectedService.Value);
        }

        [UnityTest]
        public IEnumerator TestMultipleLevelHierarchy()
        {
            // Arrange
            var grandparentObject = new GameObject("Grandparent");
            _parentObject.transform.SetParent(grandparentObject.transform);
            
            var provider = grandparentObject.AddComponent<ParentProvider>();
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act
            yield return null;

            // Assert - Should find service from grandparent
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(provider.ProvidedService, consumer.InjectedService);
            
            // Cleanup
            Object.DestroyImmediate(grandparentObject);
        }

        [UnityTest]
        public IEnumerator TestClosestProviderWins()
        {
            // Arrange
            var grandparentObject = new GameObject("Grandparent");
            _parentObject.transform.SetParent(grandparentObject.transform);
            
            var grandparentProvider = grandparentObject.AddComponent<ParentProvider>();
            grandparentProvider.ProvidedService = new TestService { Value = 10 };
            
            var parentProvider = _parentObject.AddComponent<ParentProvider>();
            parentProvider.ProvidedService = new TestService { Value = 20 };
            
            var consumer = _childObject.AddComponent<ChildConsumer>();

            // Act
            yield return null;

            // Assert - Should find closest (parent, not grandparent)
            Assert.IsNotNull(consumer.InjectedService);
            Assert.AreSame(parentProvider.ProvidedService, consumer.InjectedService);
            Assert.AreEqual(20, consumer.InjectedService.Value);
            
            // Cleanup
            Object.DestroyImmediate(grandparentObject);
        }
    }
}