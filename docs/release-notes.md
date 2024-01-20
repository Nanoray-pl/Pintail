[← back to readme](README.md)

# Release notes

## 2.4.2
Released 20 January 2024.

* Allowing default method implementations with same number of parameters to be added, as long as their parameters names differ.

## 2.4.1
Released 19 January 2024.

* Handling default interface method implementations, making them completely optional on the other side.

## 2.4.0
Released 20 December 2023.

* Added an option to (partially) ignore access checks when proxying.
* Added `TryObtainProxyFactory`, which returns `null` instead of throwing if the type could not be proxied.
* The fact of failing to create a type proxy is now cached and will exit early if repeated, instead of trying again.

## 2.3.0
Released 15 June 2023.

* Implemented Nullable value type proxies.
* Implemented Tuple and ValueTuple proxies.
* Fixed delegate proxies.

## 2.2.3
Released 29 May 2023.

* Fixed reconstructing proxies logic.

## 2.2.2
Released 8 January 2023.

* Fixed backwards type checking for nested proxies, causing issues when proxying types with methods going 3+ levels deep.

## 2.2.1
Released 17 July 2022.

* Fixed an issue with proxying methods with `in` parameters.

## 2.2.0
Released 4 June 2022.

* Implemented basic method overload resolution.
* Made generated qualified names carry the full assembly name (which fixes some duplicate type exception problems).

## 2.1.0
Released 1 May 2022.

* New `ShortNameIDGeneratingTypeNameProvider` (which is now the default).
* Obsoleted `ShortNameTypeNameProvider`.

## 2.0.1
Released 17 April 2022.

* Fixed proxying of methods with `out`/`ref` parameters which don't require proxying.

## 2.0.0
Released 24 February 2022.

* Renamed most of the types to drop the `Default` prefix.
* General performance improvements.

## 1.6.0
Released 20 February 2022.

* Added support for generic type constraints on proxied methods.

## 1.5.1
Released 19 February 2022.

* Changed the license from Apache 2.0 to MIT.

## 1.5.0
Released 19 February 2022.

* Implemented support for complex generic types/arguments.
* Added a `ProxyPrepareBehavior.[Eager/Lazy]` option for the proxy manager.
* Added some missing XML code docs.

## 1.4.0
Released 18 February 2022.

* Implemented support for multi-dimensional arrays.
* Fixed proxying of array paramaters.
* Configured Nuget to include the XML code docs.

## 1.3.1
Released 17 February 2022.

* Fixed type matching for delegate proxies.

## 1.3.0
Released 17 February 2022.

* Implemented delegate proxying.

## 1.2.1
Released 16 February 2022.

* Fixed `TypeInfo.Equals` not behaving correctly, causing some issues later down the line.

## 1.2.0
Released 16 February 2022.

* Implemented enum type handling.
* Implemented array type handling.
* Implemented (de/re)constructable type handling.

## 1.1.0
Released 14 February 2022.

* Early implementation of array handling.
* Early implementation of enum mapping.
* A `DefaultProxyManagerEnumMappingBehavior.ThrowAtRuntime` option.

## 1.0.0
Released 13 February 2022.

* Initial release.