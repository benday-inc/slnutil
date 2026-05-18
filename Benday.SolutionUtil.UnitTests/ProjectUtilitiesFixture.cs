using Benday.Common.Testing;
using Benday.SolutionUtil.Api;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class ProjectUtilitiesFixture : TestClassBase
{
    public ProjectUtilitiesFixture(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.7.2", "net472")]
    [InlineData("v4.7.1", "net471")]
    [InlineData("v4.6.2", "net462")]
    [InlineData("v4.5", "net45")]
    [InlineData("v4.0", "net40")]
    [InlineData("v3.5", "net35")]
    [InlineData("V4.8", "net48")]
    [InlineData("  v4.8  ", "net48")]
    [InlineData("net8.0", "net8.0")]
    [InlineData("net10.0", "net10.0")]
    [InlineData("netstandard2.1", "netstandard2.1")]
    [InlineData("netcoreapp3.1", "netcoreapp3.1")]
    [InlineData("", "")]
    public void NormalizeFrameworkVersionToShortForm(string input, string expected)
    {
        var actual = ProjectUtilities.NormalizeFrameworkVersionToShortForm(input);

        AssertThat.AreEqual(expected, actual, $"NormalizeFrameworkVersionToShortForm('{input}') returned wrong value.");
    }

    [Theory]
    [InlineData(@"..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll", true)]
    [InlineData(@"..\..\packages\Foo.1.0.0\lib\net48\Foo.dll", true)]
    [InlineData(@"packages\Foo.1.0.0\lib\Foo.dll", true)]
    [InlineData(@"../packages/Foo.1.0.0/lib/Foo.dll", true)]
    [InlineData(@"..\PACKAGES\Foo.1.0.0\lib\Foo.dll", true)]
    [InlineData(@"..\..\binaries\Foo.dll", false)]
    [InlineData(@"C:\external\libs\Foo.dll", false)]
    [InlineData(@"..\MyPackages\Foo.dll", false)]
    [InlineData(@"packages-staging\Foo.dll", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HintPathPointsIntoPackagesFolder(string? hintPath, bool expected)
    {
        var actual = ProjectUtilities.HintPathPointsIntoPackagesFolder(hintPath);

        AssertThat.AreEqual(expected, actual, $"HintPathPointsIntoPackagesFolder('{hintPath}') returned wrong value.");
    }

    [Fact]
    public void ProjectUsesPackagesConfig_FileExists_ReturnsTrue()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj", SdkStyleNet8Project);
        temp.WriteFile("packages.config", "<packages />");

        var actual = ProjectUtilities.ProjectUsesPackagesConfig(projectPath);

        AssertThat.AreEqual(true, actual, "Expected packages.config to be detected.");
    }

    [Fact]
    public void ProjectUsesPackagesConfig_FileMissing_ReturnsFalse()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj", SdkStyleNet8Project);

        var actual = ProjectUtilities.ProjectUsesPackagesConfig(projectPath);

        AssertThat.AreEqual(false, actual, "Expected packages.config detection to return false when file missing.");
    }

    [Fact]
    public void GetProjectTargetFrameworkShortForm_SdkStyleSingle()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj", SdkStyleNet8Project);

        var actual = ProjectUtilities.GetProjectTargetFrameworkShortForm(projectPath);

        AssertThat.AreEqual("net8.0", actual, "SDK-style single TFM not read correctly.");
    }

    [Fact]
    public void GetProjectTargetFrameworkShortForm_SdkStyleMulti()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
              </PropertyGroup>
            </Project>
            """);

        var actual = ProjectUtilities.GetProjectTargetFrameworkShortForm(projectPath);

        AssertThat.AreEqual("net8.0;net10.0", actual, "SDK-style multi-TFM not read correctly.");
    }

    [Fact]
    public void GetProjectTargetFrameworkShortForm_OldStyleNormalized()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj", OldStyleNet48Project);

        var actual = ProjectUtilities.GetProjectTargetFrameworkShortForm(projectPath);

        AssertThat.AreEqual("net48", actual, "Old-style TargetFrameworkVersion not normalized to short form.");
    }

    [Fact]
    public void GetReferenceForProjectFile_OldStyleClassifiesAllFiveTypes()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj",
            """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="System" />
                <Reference Include="System.Net.Http" />
                <Reference Include="Newtonsoft.Json">
                  <HintPath>..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll</HintPath>
                </Reference>
                <Reference Include="Legacy">
                  <HintPath>..\..\binaries\Legacy.dll</HintPath>
                </Reference>
                <PackageReference Include="Serilog" Version="3.0.0" />
                <ProjectReference Include="..\Other\Other.csproj" />
              </ItemGroup>
            </Project>
            """);

        var refs = ProjectUtilities.GetReferenceForProjectFile(projectPath);

        AssertThat.AreEqual(2, refs.Count(r => r.ReferenceType == "framework-ref"), "Wrong framework-ref count.");
        AssertThat.AreEqual(1, refs.Count(r => r.ReferenceType == "nuget-via-packages-config"), "Wrong nuget-via-packages-config count.");
        AssertThat.AreEqual(1, refs.Count(r => r.ReferenceType == "binary-ref"), "Wrong binary-ref count.");
        AssertThat.AreEqual(1, refs.Count(r => r.ReferenceType == "package-ref"), "Wrong package-ref count.");
        AssertThat.AreEqual(1, refs.Count(r => r.ReferenceType == "project-ref"), "Wrong project-ref count.");
    }

    [Fact]
    public void GetReferenceForProjectFile_SdkStyleDetectsPackageReference()
    {
        using var temp = new TempProjectDir();
        var projectPath = temp.WriteProject("Sample.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Serilog" Version="3.0.0" />
              </ItemGroup>
            </Project>
            """);

        var refs = ProjectUtilities.GetReferenceForProjectFile(projectPath);

        AssertThat.AreEqual(2, refs.Count, "Expected exactly two refs.");
        AssertThat.AreEqual(2, refs.Count(r => r.ReferenceType == "package-ref"), "Expected both refs to be package-ref.");
    }

    private const string SdkStyleNet8Project =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private const string OldStyleNet48Project =
        """
        <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
          </PropertyGroup>
        </Project>
        """;

    private sealed class TempProjectDir : IDisposable
    {
        public string Path { get; }

        public TempProjectDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"slnutil-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string WriteProject(string fileName, string contents)
        {
            var full = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(full, contents);
            return full;
        }

        public void WriteFile(string fileName, string contents)
        {
            File.WriteAllText(System.IO.Path.Combine(Path, fileName), contents);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
