# DGP.ServiceLocator

A Unity package for registering services in a tree of containers and supplying them to objects that declare what they need.

## Location

**Service**:
Any object registered under a type key so other code can find it by that type.
_Avoid_: Locatable service, dependency (when meaning the registered object)

**Service Container**:
A registry of services that may have a parent container, forming a tree whose root is the global container.
_Avoid_: Context, scope, locator (for a single node)

**Search Mode**:
The rule for which containers in the tree are consulted, and in what order, when a service is looked up.

**Pending Query**:
A lookup for a service that was not yet registered, held until a matching registration fulfils it.
_Avoid_: Async request, subscription

**Provider**:
Anything that can be asked for a service by type: a service container, a GameObject hierarchy, or an object's provided members.
_Avoid_: Source, resolver

## Injection

**Injection Target**:
An object whose injection points are filled from a provider.
_Avoid_: Subscriber, consumer, client

**Injection Point**:
A field, property, method or constructor on an injection target's type, including private members declared on its base classes, marked as needing services.
_Avoid_: Injectable, dependency slot

**Required Dependency**:
An injection point that must be satisfied; failing to satisfy it is a missing dependency.

**Optional Dependency**:
An injection point left untouched when its service can't be found; for a method, the whole method is skipped if any parameter is missing.

**Missing Dependency**:
The failure raised when a required dependency's service is not supplied by the provider.

**Provided Member**:
A field or property marked to offer its value as a service to other objects.
_Avoid_: Local service, exported member
