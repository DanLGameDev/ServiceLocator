namespace DGP.ServiceLocator
{
    public class MonoBehaviourExtensions
    {
        public static void InjectServices(MonoBehaviourExtensions target)
        {
            ServiceInjector.Inject(target);
        }
    }
}