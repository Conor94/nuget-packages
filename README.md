# Overview
This repository contains NuGet packages that I have published to a local server. This helps with code maintainability by allowing me to store and retrieve .NET code from a single location.  
<br/>
# Packages
### AppConfigurationManager
Provides a static class that can be used to create, retrieve, and save a [System.Configuration.ConfigurationSection](https://docs.microsoft.com/en-us/dotnet/api/system.configuration.configurationsection?view=netframework-4.7.2). ConfigurationSection is a .NET class that provides a way to manage an App.config file using type-safe properties.   
<br/>

### MvvmBase
Expands the Prism class heirarchy to include model and view model base classes. These classes inherit from a class called DataErrorBindableBase, which implements IDataErrorInfo. This allows classes that derive from ModelBase, ViewModelBase, or DataErrorBindableBase to implement custom validation logic and display custom validation results (e.g. error messages or highlighting a TextBox control in red).
