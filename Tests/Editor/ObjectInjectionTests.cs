using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;
using NUnit.Framework;

namespace DGP.ServiceLocator.Editor.Tests
{
    public class ObjectInjectionTests
    {

        private class OriginClass
        {
            [Provide] public List<string> MyList = new List<string>();
        }

        private class SubscriberClass
        {
            [Inject] public List<string> MyList;
        }
        
        [Test]
        public void IsThiSStupid() {
            var origin = new OriginClass();
            var subscriber = new SubscriberClass();
            
            origin.InjectLocalServices(subscriber);
            
            Assert.AreSame(origin.MyList, subscriber.MyList);
        }
    }
}