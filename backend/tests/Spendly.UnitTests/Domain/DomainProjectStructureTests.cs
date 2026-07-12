using System.Xml.Linq;

namespace Spendly.UnitTests.Domain;

public sealed class DomainProjectStructureTests
{
    private static readonly string[] ForbiddenPackagePrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql"
    ];

    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql"
    ];

    [Fact]
    public void DomainProject_ShouldNotReferenceOtherProjects()
    {
        var projectFilePath = GetDomainProjectFilePath();
        var projectReferences = ReadProjectReferences(projectFilePath);

        Assert.Empty(projectReferences);
    }

    [Fact]
    public void DomainProject_ShouldNotReferenceFrameworkOrPersistencePackages()
    {
        var projectFilePath = GetDomainProjectFilePath();
        var document = XDocument.Load(projectFilePath);

        var forbiddenReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .Where(packageName => ForbiddenPackagePrefixes.Any(prefix =>
                packageName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void DomainAssembly_ShouldNotReferenceFrameworkOrPersistenceAssemblies()
    {
        var forbiddenReferences = typeof(Spendly.Domain.Common.Entity<>)
            .Assembly
            .GetReferencedAssemblies()
            .Where(reference => ForbiddenAssemblyPrefixes.Any(prefix =>
                reference.Name?.StartsWith(prefix, StringComparison.Ordinal) is true))
            .Select(reference => reference.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    private static string GetDomainProjectFilePath()
    {
        return Path.Combine(
            GetBackendDirectory().FullName,
            "src",
            "Spendly.Domain",
            "Spendly.Domain.csproj");
    }

    private static string[] ReadProjectReferences(string projectFilePath)
    {
        var document = XDocument.Load(projectFilePath);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty)
                .Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
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
