#!/bin/bash

dotnet test ./Benday.SolutionUtil.UnitTests

cp ./generated-readme-files/README-for-nuget.md .
cp ./generated-readme-files/README.md .

dotnet build
