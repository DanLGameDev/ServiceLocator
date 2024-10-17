namespace DGP.ServiceLocator.Injectable
{
    public static class ObjectExtensions
    {
        public static T InjectLocalServices<T>(this object source, T target)
        {
            return ServiceInjector.InjectFromSource(source, target);
        }
        
    }
}