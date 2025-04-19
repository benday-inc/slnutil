[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall,
    [ValidateNotNullOrEmpty()]
    [switch]
    $skipBuild)

if ($reinstall -eq $true)
{
    &.\uninstall.ps1
}

if ($skipBuild -eq $false)
{
    dotnet build
}

dotnet tool install --global --add-source .\Benday.SolutionUtil.ConsoleUi\bin\Debug slnutil --ignore-failed-sources
