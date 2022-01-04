# Overview
This repository contains NuGet packages that I have published to a local server. This helps with code maintainability by allowing me to store and retrieve .NET code from a single location.

# How to use
There are two ways to use the packages: installing them as a NuGet package, or adding them as a project reference. Both ways are documented below.

## Install as NuGet Package
Currently, these packages are not publicly available. However, if you want to install them as a NuGet package you can publish the packages you want to use to a local feed, and then install the package from there. Note that before publishing to a local feed, you must create a NuGet package. 

1. Create a .nuspec file using `nuget spec *.csproj`
2. Create a .nupkg file using `nuget pack *.csproj -Build -Properties Configuration=Release` (change Release to Debug if you want to package the Debug version)
3. Publish to a local feed using:
	* `nuget add new_package.1.0.0.nupkg -source DriveLetter:\path\to\package` if publishing to your local machine
	* `nuget add new_package.1.0.0.nupkg -source \\MACHINE-NAME\path\to\package` if publishing to a network folder

Microsoft has documentation on creating packages and publishing them to local feeds if you need more information.
|Topic|Link|
|:-|:-|
|Creating a package using nuget.exe CLI|https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package
|Publishing to local feed|https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds

## Add a project reference
These projects can be added to an existing Visual Studio solution and used as a reference. Documentation on adding a project reference in Visual Studio is here: https://docs.microsoft.com/en-us/visualstudio/ide/managing-references-in-a-project?view=vs-2022.

### Known Issues
#### GenericDao
There is a known issue with adding the GenericDao project as a reference in Visual Studio. The issue causes the following 
exceptions to be thrown: `Exception has been thrown by the target of an invocation` and `Unable to load DLL 'SQLite.Interop.dll': The specified module could not be found. (Exception from HRESULT: 0x8007007E)`.
These exceptions are thrown because the `GenericDao` class is using [Activator.CreateInstance()](https://docs.microsoft.com/en-us/dotnet/api/system.activator.createinstance?view=netframework-4.7.2) to create instances of the objects it needs to interact with SQL
and SQLite databases.

To resolve this, you must install the NuGet package [System.Data.Sqlite](https://www.nuget.org/packages/System.Data.SQLite/) manually in the project
that references GenericDao. For example, if ProjectA references GenericDao, then ProjectA must install 
[System.Data.Sqlite](https://www.nuget.org/packages/System.Data.SQLite/). 

# Packages
## AppConfigurationManager
Provides a static class that can be used to create, retrieve, and save a [System.Configuration.ConfigurationSection](https://docs.microsoft.com/en-us/dotnet/api/system.configuration.configurationsection?view=netframework-4.7.2). ConfigurationSection is a .NET class that provides a way to manage an App.config file using type-safe properties.   

## PrismMvvmBase
Expands the Prism class heirarchy to include model and view model base classes. These classes inherit from a class called DataErrorBindableBase, which implements IDataErrorInfo. This allows classes that derive from ModelBase, ViewModelBase, or DataErrorBindableBase to implement custom validation logic and display custom validation results (e.g. error messages or highlighting a TextBox control in red).

## GenericDao
Generic data access object (DAO) for SQLite and SQL.

**Important note:** There is a known issue with this package when adding it as a reference. Refer to [Known Issues](#genericdao) for information about resolving it.

This package is not currently complete. The features and improvements that still need to be implemented for this package are listed below:
- [ ] Support for SQL operators BETWEEN and IN.
- [ ] Support for OR between statements in a where clause. Currently all where statements are compared with AND (e.g. statement1 AND statement2 AND statement3).
- [ ] Update function for multiple records. The current update function only supports updating a single record at a time.
- [ ] More efficient way to create WHERE and SET statements when they both need to be used in a query.
- [ ] Better return values for CRUD functions.
