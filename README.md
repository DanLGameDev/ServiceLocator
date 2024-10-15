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


## Dependency Injection
You can now inject your services using the `[Inject]` attribute and calling `ServiceInjector.Inject(obj);`

This supports field, property, and method injection. Missing services will not throw exceptions by default but you can enable this by adding a flag to the `Inject` attribute.

You can also allow for asynchronoous injection using the `Asynchronous` flag, in which case the dependency will be injected when available.

- `[Inject] public MyMockService MyService;`
- `[Inject(InjectorFlags.ExceptionIfMissing)] public MyMockService MyService;`
- `[Inject(InjectorFlags.Asynchronous)] public MyMockService MyService;`
- `[Inject] public void InjectServices(MyMockService myService, MyMockComplexService myComplexService) {}`