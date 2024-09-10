Create a service by implement ILocatableService interface.
```csharp
public class MyService : ILocatableService
{
    //code
}
```

Register the service.
```csharp

var myService = new MyService();
ServiceLocator.RegisterService(myService);
```

Locate the service synchronously, if the service is not found, it will throw an `InvalidOperationException`.
```csharp
var locatedService = ServiceLocator.LocateService<MyTestService>();
```

Locate the service asynchronously, if the service is found the callback will be called with the located service. If the service is not found, the callback will be called when the service is registered.
```csharp
ServiceLocator.LocateServiceAsync<MyTestService>((service) => {
    //service will be the located service
});

ServiceLocator.RegisterService(myService);
```

Try pattern:
```csharp        
if (ServiceLocator.TryLocateService<MyTestService>(out locatedService)) {
    //use located service
}
```

Deregister the service.
```csharp
ServiceLocator.DeregisterService<MyTestService>();
```