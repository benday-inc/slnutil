[CmdletBinding()]

param([Parameter(HelpMessage = 'Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall)

if ($reinstall -eq $true) {
    &.\uninstall.ps1
}

dotnet build

$path = ".\Benday.SolutionUtil.ConsoleUi\bin\Debug"

# find the first .nupkg file in the output directory
$nupkgFile = Get-ChildItem -Path $path -Filter *.nupkg | Select-Object -First 1 

# if a .nupkg file was found, print it out
if ($nupkgFile) {
    Write-Host "Found .nupkg file: $($nupkgFile.FullName)"
} else {
    Write-Host "No .nupkg file found in the directory: $path"
    exit 1
}

dotnet tool install --global --add-source $path slnutil
