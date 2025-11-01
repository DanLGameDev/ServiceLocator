# Service Locator & Dependency Injection

A flexible service locator and dependency injection system for Unity with support for hierarchical service providers.

## Quick Start

### Basic Service Registration

Register services with the global `ServiceLocator`:

```csharp
var myService = new MyService();
ServiceLocator.RegisterService(myService);
```

### Locating Services

**Synchronous** - Throws `InvalidOperationException` if not found:
```csharp
var service = ServiceLocator.GetService<MyService>();
```

**Try Pattern** - Safe, returns bool:
```csharp
if (ServiceLocator.TryLocateService<MyService>(out var service)) {
    // use service
}
```

**Asynchronous** - Callback invoked when service is available:
```csharp
ServiceLocator.LocateServiceAsync<MyService>((service) => {
    // service will be called immediately if available,
    // or later when registered
});
```

### Deregistering Services

```csharp
ServiceLocator.DeregisterService<MyService>();
```

## Dependency Injection

The system supports automatic dependency injection using the `[Inject]` attribute on fields, properties, methods, and constructors.

### Field and Property Injection

```csharp
public class MyComponent
{
    [Inject] private MyService _serviceField;
    [Inject] public MyService ServiceProperty { get; set; }
}

// Manual injection
var component = new MyComponent();
ServiceLocator.Injector.Inject(component);
```

### Method Injection

```csharp
public class MyComponent
{
    private MyService _service;
    
    [Inject]
    public void Setup(MyService service)
    {
        _service = service;
    }
}
```

### Constructor Injection

```csharp
public class MyComponent
{
    public readonly MyService Service;
    
    [Inject]
    public MyComponent(MyService service)
    {
        Service = service;
    }
}

// Create and inject in one step
var component = ServiceLocator.Injector.CreateAndInject<MyComponent>();
```

### Injection Flags

Control injection behavior with flags:

```csharp
// Optional - Won't throw if service is missing
[Inject(flags: InjectorFlags.Optional)]
private MyService _optionalService;

// DontReplace - Won't overwrite if already set
[Inject(flags: InjectorFlags.DontReplace)]
private MyService _existingService;

// Combine flags
[Inject(flags: InjectorFlags.Optional | InjectorFlags.DontReplace)]
private MyService _service;
```

### Type Override

Specify a different type for injection:

```csharp
[Inject(serviceType: typeof(ConcreteService))]
private IService _service;
```

## Unity MonoBehaviour Integration

### Automatic Hierarchy Injection

Inherit from `InjectedMonoBehaviour` to automatically inject services from the GameObject hierarchy and global ServiceLocator:

```csharp
public class PlayerUI : InjectedMonoBehaviour
{
    [Inject] private PlayerInventory _inventory;
    [Inject] private PlayerStats _stats;
    
    // Services automatically injected in Awake()
    // First searches up GameObject hierarchy, then falls back to global ServiceLocator
}
```

### Manual Injection (Without Inheriting)

If you can't inherit from `InjectedMonoBehaviour`, use extension methods:

```csharp
public class MyComponent : MonoBehaviour
{
    [Inject] private PlayerInventory _inventory;
    
    void Awake()
    {
        // Inject from hierarchy then global
        this.InjectFromHierarchy();
        
        // Or inject only from global ServiceLocator
        // this.InjectFromGlobal();
        
        // Or inject from a specific container
        // this.InjectFromContainer(myContainer);
    }
}
```

### Providing Services in Hierarchy

Mark fields/properties with `[Provide]` to make them discoverable by child components:

```csharp
public class PlayerController : MonoBehaviour
{
    [Provide] public PlayerInventory Inventory;
    [Provide] public PlayerStats Stats;
}

// Child components will find these automatically via InjectedMonoBehaviour
```

### How Hierarchy Search Works

1. **Child searches up** - Walks up the transform hierarchy (parent by parent)
2. **Checks for providers** - Looks for `IServiceProvider` components or `[Provide]` attributes
3. **Falls back to global** - If not found in hierarchy, checks global `ServiceLocator`
4. **Closest wins** - The nearest parent with the service takes precedence

### Disable Hierarchy Search

```csharp
public class GlobalOnlyComponent : InjectedMonoBehaviour
{
    protected override bool SearchHierarchy => false;
    
    [Inject] private GlobalAudioManager _audio; // Only from ServiceLocator
}
```

### Manual Hierarchy Search

You can also manually search the GameObject hierarchy:

```csharp
// Try pattern
if (gameObject.TryGetServiceFromHierarchy<PlayerInventory>(out var inventory))
{
    // use inventory
}

// Direct get (throws if not found)
var stats = gameObject.GetServiceFromHierarchy<PlayerStats>();
```

## Providing Services

### Via [Provide] Attribute

```csharp
public class ServiceProvider : MonoBehaviour
{
    [Provide] public AudioManager Audio = new AudioManager();
    [Provide] public InputManager Input = new InputManager();
}
```

### Via IServiceProvider Component

For more control, implement `IServiceProvider`:

```csharp
public class CustomProvider : MonoBehaviour, IServiceProvider
{
    public bool TryGetService(Type serviceType, out object service)
    {
        if (serviceType == typeof(MyService))
        {
            service = CreateMyService();
            return true;
        }
        
        service = null;
        return false;
    }
}
```

Or use the built-in `ServiceProviderMonoBehaviour` component for reflection-based discovery (useful for Inspector visibility).

## Advanced: Hierarchical Containers

Create parent-child container relationships for scoped services:

```csharp
var globalContainer = new ServiceContainer();
globalContainer.RegisterService(new GlobalService());

var sceneContainer = new ServiceContainer(globalContainer); // globalContainer is parent
sceneContainer.RegisterService(new SceneService());

// Search modes
sceneContainer.GetService<GlobalService>(ServiceSearchMode.GlobalFirst); // Checks parent first
sceneContainer.GetService<SceneService>(ServiceSearchMode.LocalFirst);   // Checks local first
sceneContainer.GetService<SceneService>(ServiceSearchMode.LocalOnly);    // Only checks local
```

## Local Object Injection

Inject services directly from one object to another without using a container:

```csharp
public class Parent
{
    [Provide] public MyService Service = new MyService();
}

public class Child
{
    [Inject] private MyService _service;
}

var parent = new Parent();
var child = new Child();

// Inject from parent to child
parent.InjectLocalServices(child);
```

## Best Practices

1. **Use `InjectedMonoBehaviour`** for Unity components that need services
2. **Mark providers with `[Provide]`** to make services discoverable in hierarchy
3. **Register global services** in a bootstrap/initialization script
4. **Use hierarchy for local services** (player inventory, UI managers)
5. **Use global `ServiceLocator`** for truly global services (audio, input, game state)
6. **Prefer constructor injection** for non-MonoBehaviour classes
7. **Use `[Inject]` fields** for MonoBehaviours (they can't have custom constructors)

## Example: Complete Setup

```csharp
// Global bootstrap
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        ServiceLocator.RegisterService(new AudioManager());
        ServiceLocator.RegisterService(new InputManager());
    }
}

// Parent provides local services
public class PlayerController : MonoBehaviour
{
    [Provide] public PlayerInventory Inventory = new PlayerInventory();
    [Provide] public PlayerStats Stats = new PlayerStats();
}

// Child automatically receives services
public class PlayerUI : InjectedMonoBehaviour
{
    [Inject] private PlayerInventory _inventory; // From parent
    [Inject] private AudioManager _audio;        // From global
    
    void Start()
    {
        // Services are already injected by Awake()
        Debug.Log($"Health: {_inventory.Health}");
        _audio.PlaySound("ui_open");
    }
}
```