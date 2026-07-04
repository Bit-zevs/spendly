using System.Xml.Linq;

namespace Spendly.UnitTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void UnitTestsProject_ShouldHaveExpectedTestFolders()
    {
        var projectDirectory = GetProjectDirectory();

        Assert.True(
            Directory.Exists(Path.Combine(projectDirectory.FullName, "Domain")),
            "The UnitTests project should contain the 'Domain' folder for future domain unit tests.");

        Assert.True(
            Directory.Exists(Path.Combine(projectDirectory.FullName, "Application")),
            "The UnitTests project should contain the 'Application' folder for future application unit tests.");

        Assert.True(
            Directory.Exists(Path.Combine(projectDirectory.FullName, "TestUtilities")),
            "The UnitTests project should contain the 'TestUtilities' folder for shared unit test helpers.");
    }

    [Fact]
    public void UnitTestsProject_ShouldReferenceOnlyDomainAndApplicationProjects()
    {
        var projectDirectory = GetProjectDirectory();
        var projectFilePath = Path.Combine(projectDirectory.FullName, "Spendly.UnitTests.csproj");

        var document = XDocument.Load(projectFilePath);

        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expectedProjectReferences = new[]
        {
            "../../src/Spendly.Application/Spendly.Application.csproj",
            "../../src/Spendly.Domain/Spendly.Domain.csproj"
        };

        Assert.Equal(expectedProjectReferences, projectReferences);
    }

    private static DirectoryInfo GetProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var projectFilePath = Path.Combine(directory.FullName, "Spendly.UnitTests.csproj");

            if (File.Exists(projectFilePath))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the Spendly.UnitTests project directory.");
    }
}
