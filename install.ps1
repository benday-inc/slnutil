[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall)

if ($reinstall -eq $true)
{
    &.\uninstall.ps1
}

dotnet build

$packageName = "slnutil"
$relativePath = 'Benday.SolutionUtil.ConsoleUi\bin\Debug'
$pathToDebugFolder = Join-Path $PSScriptRoot $relativePath

Write-Host "Installing $packageName from $pathToDebugFolder"

dotnet tool install --global --add-source "$pathToDebugFolder" $packageName