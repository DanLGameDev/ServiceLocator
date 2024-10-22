using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ObjectInjectionTests
    {
        private class MockService { }

        private class OriginClass
        {
            [Provide] public readonly MockService Service = new();
            public SubscriberClass CreateSubscriber() => this.CreateWithLocalServices<SubscriberClass>();
        }

        private class SubscriberClass
        {
            public readonly MockService Service;
            
            public SubscriberClass(MockService service) {
                Service = service;
            }
        }
        
        [Test]
        public void CreateWithLocalTest() {
            var origin = new OriginClass();
            var subscriber = origin.CreateSubscriber();
            
            Assert.AreSame(origin.Service, subscriber.Service);
        }
        
    }
}