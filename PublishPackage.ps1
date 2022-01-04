<# 
    FILE        : PublishPackage.ps1
    DESCRIPTION : This script packages and publishes a NuGet package. A path to a .csproj file must be provided
      as the first argument. The .csproj file is used to create the package.

      IMPORTANT NOTE - This script should be ran from the directory that it's stored in.
#>


if ($args[0] -ieq "help")
{
    Write-Output "Usage: PublishPackage <.csproj file> <destination> [-d | -D]"
    Write-Output ""
    Write-Output "       <.csproj file> - The project file used to create the NuGet package"
    Write-Output "       <destination>  - The folder where the NuGet package will be added"
    Write-Output "       [-d -D]        - Build the package in Debug mode. If this argument is not provided, the package will be built in Release mode."
    Write-Output ""

    Return
}


<# Make sure a .csproj file was provided #>
if ([System.IO.Path]::GetExtension($args[0]) -ne ".csproj")
{
    Write-Output "Must provide path to .csproj file."
    Return
}

if ([System.IO.Directory]::Exists($args[1]) -eq $false)
{
    Write-Output "The provided desintation folder or UNC share does not exist."
    Return
}

$mode = "Release" <# Default mode is Release, but if "-d" or "-D" is given as the third argument, then Debug mode is used #>
if ($args[2] -ieq "-d")
{
    $mode = "Debug"
}


<# Get absolute path to .csproj and use it to get the absolute path to the directory that has the .csproj #>
$dirName = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($args[0]))
<# Also need to get the file name now, since we are going to change into the directory that has the .csproj #>
$fileName = [System.IO.Path]::GetFileName($args[0])


<# Change to the directory that the .csproj is located in so that the .nupkg will be created in the same folder #>
Push-Location $dirName

<# Create a .nupkg #>
Write-Output "Building package in $mode mode"
$packOutput = nuget pack $fileName -Build -Properties Configuration=$mode

<# Get the the filename of the nuget package #>
$matchInfo = $packOutput | Select-String -Pattern "Successfully created package '(?<pkgPath>[A-Za-z:\\\s\-\.\d]+)'"

<# Publish the package to the local feed #>
nuget add $matchInfo.Matches[0].Groups['pkgPath'].Value -Source $args[1]
Pop-Location

