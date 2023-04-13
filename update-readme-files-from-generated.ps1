dotnet test .\Benday.SolutionUtil.UnitTests

Copy-Item -Path .\generated-readme-files\README-for-nuget.md -Destination .
Copy-Item -Path .\generated-readme-files\README.md -Destination .

dotnet build