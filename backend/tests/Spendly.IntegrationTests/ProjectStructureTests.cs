using System.Xml.Linq;

namespace Spendly.IntegrationTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void ApiProject_ShouldReferenceApplicationAndInfrastructureProjects()
    {
        var references = GetProjectReferences("src", "Spendly.Api", "Spendly.Api.csproj");

        Assert.Contains("Spendly.Application", references);
        Assert.Contains("Spendly.Infrastructure", references);
        Assert.DoesNotContain("Spendly.Worker", references);
    }

    [Fact]
    public void ApiProject_ShouldNotContainWeatherForecastTemplateCode()
    {
        var apiRoot = Path.Combine(GetBackendRoot(), "src", "Spendly.Api");

        var sourceFiles = Directory.EnumerateFiles(
            apiRoot,
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("WeatherForecast", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("weatherforecast", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string?[] GetProjectReferences(params string[] projectPathParts)
    {
        var projectPath = Path.Combine([GetBackendRoot(), .. projectPathParts]);

        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Replace('\\', '/'))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(projectName => !string.IsNullOrWhiteSpace(projectName)).ToArray();
    }

    private static string GetBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Spendly.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate backend root directory containing Spendly.sln.");
    }
}
