using System.Xml.Linq;

namespace Spendly.UnitTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void UnitTestsProject_ShouldReferenceOnlyDomainAndApplication()
    {
        AssertProjectReferences(
            "tests/Spendly.UnitTests/Spendly.UnitTests.csproj",
            "../../src/Spendly.Application/Spendly.Application.csproj",
            "../../src/Spendly.Domain/Spendly.Domain.csproj");
    }

    [Fact]
    public void ApplicationProject_ShouldReferenceOnlyDomain()
    {
        AssertProjectReferences(
            "src/Spendly.Application/Spendly.Application.csproj",
            "../Spendly.Domain/Spendly.Domain.csproj");
    }

    [Fact]
    public void InfrastructureProject_ShouldReferenceApplicationAndDomain()
    {
        AssertProjectReferences(
            "src/Spendly.Infrastructure/Spendly.Infrastructure.csproj",
            "../Spendly.Application/Spendly.Application.csproj",
            "../Spendly.Domain/Spendly.Domain.csproj");
    }

    [Fact]
    public void ApiProject_ShouldReferenceApplicationAndInfrastructure()
    {
        AssertProjectReferences(
            "src/Spendly.Api/Spendly.Api.csproj",
            "../Spendly.Application/Spendly.Application.csproj",
            "../Spendly.Infrastructure/Spendly.Infrastructure.csproj");
    }

    [Fact]
    public void WorkerProject_ShouldReferenceApplicationAndInfrastructure()
    {
        AssertProjectReferences(
            "src/Spendly.Worker/Spendly.Worker.csproj",
            "../Spendly.Application/Spendly.Application.csproj",
            "../Spendly.Infrastructure/Spendly.Infrastructure.csproj");
    }

    private static void AssertProjectReferences(
        string projectPathRelativeToBackend,
        params string[] expectedReferences)
    {
        var projectFilePath = Path.Combine(
            GetBackendDirectory().FullName,
            projectPathRelativeToBackend.Replace('/', Path.DirectorySeparatorChar));

        var document = XDocument.Load(projectFilePath);

        var actualReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty)
                .Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var sortedExpectedReferences = expectedReferences
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(sortedExpectedReferences, actualReferences);
    }

    private static DirectoryInfo GetBackendDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Spendly.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the backend directory containing Spendly.sln.");
    }
}
