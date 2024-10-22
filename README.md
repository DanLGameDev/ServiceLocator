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

### Property and Field Injection
```c#
// The receiver marks properties and fields with the [Inject] attribute
private class MyMockRequiredSubscriber
{
    [Inject] public MyMockService MyServiceField;
    [Inject] public MyMockService MyServiceProperty { get; set; }
}

// Create a container (or could use ServiceLocator static)
ServiceContainer container = new();

var service = new MyMockService();
var subscriber = new MyMockRequiredSubscriber();

// Register the service
container.RegisterService(service);

// Inject the service into the subscriber
container.Injector.Inject(subscriber);

// The property and field should now be set
Assert.AreSame(service, subscriber.MyServiceProperty);
Assert.AreSame(service, subscriber.MyServiceField);
```

### Method Injection
```c#
// The receiver marks methods with the [Inject] attribute
private class MyMockMethodSubscriber
{
    public MyMockService MyService { get; private set; }
    [Inject] public void InjectService(MyMockService myService) => MyService = myService;
}
        
ServiceContainer container = new();

var service = new MyMockService();
var subscriber = new MyMockMethodSubscriber();

container.RegisterService(service);
container.Injector.Inject(subscriber);

Assert.AreSame(service, subscriber.MyService);
```