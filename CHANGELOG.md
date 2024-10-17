# Changelog

## [1.4.1] - 2024-10-17
### Added
- Added constructor injection

## [1.4.0] - 2024-10-16
### Changed
- ServiceContainer is now an instantiatable locator
- ServiceLocator now uses a ServiceContainer instance internally

## [1.3.1] - 2024-10-15
### Added
- Added extension method to MonoBehaviours to inject services
- Added InjectedMonoBehaviour base class which injects on awake

## [1.3.0] - 2024-10-15
### Added
- Added dependency injection support

## [1.1.0] - 2024-10-08
### Changes
- Debugger window is no longer dependent on Odin Inspector
- Updated System.Type to Type and System.Object to object
- Updated `LocateService` to `GetService`
- Documented exception thrown by `GetService`

## [1.0.9] - 2024-10-03
### Changes
- Services will now be cleared when exiting play mode

## [1.0.8] - 2024-09-10
### Changes
- `LocateService` will now throw an exception if the service is not found.

## [1.0.0] - 2024-08-30
### First Release
- Initial Commit