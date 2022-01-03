# GenericDao
## How to use
### Install as NuGet Package
Currently, this package is not publicly available. However, if you want to install it as a NuGet package you can publish it to a local
feed, and then install the package from there. Before publishing to a local feed, you must create a NuGet package. 

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


### Add a project reference
GenericDao can be added to an existing Visual Studio solution and used as a reference. Documentation
on adding a project reference in Visual Studio is here: https://docs.microsoft.com/en-us/visualstudio/ide/managing-references-in-a-project?view=vs-2022.
#### Known Issue
There is a known issue with adding the GenericDao project as a reference in Visual Studio. The issue causes the following 
exceptions to be thrown: `Exception has been thrown by the target of an invocation` and `Unable to load DLL 'SQLite.Interop.dll': The specified module could not be found. (Exception from HRESULT: 0x8007007E)`.
These exceptions are thrown because the `GenericDao` class is using [Activator.CreateInstance()](https://docs.microsoft.com/en-us/dotnet/api/system.activator.createinstance?view=netframework-4.7.2) to create instances of the objects it needs to interact with SQL
and SQLite databases.

To resolve this, you must install the NuGet package [System.Data.Sqlite](https://www.nuget.org/packages/System.Data.SQLite/) manually in the project
that references GenericDao. For example, if ProjectA references GenericDao, then ProjectA must install 
[System.Data.Sqlite](https://www.nuget.org/packages/System.Data.SQLite/). 


