using System.Xml.Linq;

namespace Spendly.UnitTests.Domain;

public sealed class DomainProjectStructureTests
{
    private static readonly string[] ExpectedDomainDirectories =
    [
        "Common",
        "Errors",
        "ValueObjects",
        "Wallets",
        "Categories",
        "Transactions"
    ];

    [Fact]
    public void DomainProject_ShouldContainExpectedDirectories()
    {
        var domainProjectDirectory = GetDomainProjectDirectory();

        foreach (var directoryName in ExpectedDomainDirectories)
        {
            var directoryPath = Path.Combine(domainProjectDirectory.FullName, directoryName);

            Assert.True(
                Directory.Exists(directoryPath),
                $"The Spendly.Domain project should contain the '{directoryName}' directory.");
        }
    }

    [Fact]
    public void DomainDirectories_ShouldContainReadmeFiles()
    {
        var domainProjectDirectory = GetDomainProjectDirectory();

        foreach (var directoryName in ExpectedDomainDirectories)
        {
            var readmePath = Path.Combine(domainProjectDirectory.FullName, directoryName, "README.md");

            Assert.True(
                File.Exists(readmePath),
                $"The Spendly.Domain/{directoryName} directory should contain a README.md file describing its purpose.");
        }
    }

    [Fact]
    public void DomainProject_ShouldNotReferenceOtherSpendlyProjects()
    {
        var domainProjectDirectory = GetDomainProjectDirectory();
        var projectFilePath = Path.Combine(domainProjectDirectory.FullName, "Spendly.Domain.csproj");

        var document = XDocument.Load(projectFilePath);

        var projectReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(projectReferences);
    }

    [Fact]
    public void DomainProject_ShouldNotReferencePersistencePackages()
    {
        var domainProjectDirectory = GetDomainProjectDirectory();
        var projectFilePath = Path.Combine(
            domainProjectDirectory.FullName,
            "Spendly.Domain.csproj");

        var document = XDocument.Load(projectFilePath);

        var persistencePackageReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .Where(packageName =>
                packageName.StartsWith(
                    "Microsoft.EntityFrameworkCore",
                    StringComparison.OrdinalIgnoreCase)
                || packageName.StartsWith(
                    "Npgsql",
                    StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(persistencePackageReferences);
    }

    [Fact]
    public void DomainAssembly_ShouldNotReferenceEntityFrameworkCore()
    {
        var entityFrameworkReferences = typeof(Spendly.Domain.Common.Entity<>)
            .Assembly
            .GetReferencedAssemblies()
            .Where(reference =>
                reference.Name?.StartsWith(
                    "Microsoft.EntityFrameworkCore",
                    StringComparison.Ordinal) is true)
            .Select(reference => reference.FullName)
            .ToArray();

        Assert.Empty(entityFrameworkReferences);
    }

    private static DirectoryInfo GetDomainProjectDirectory()
    {
        var unitTestsProjectDirectory = GetUnitTestsProjectDirectory();

        var domainProjectDirectoryPath = Path.GetFullPath(
            Path.Combine(
                unitTestsProjectDirectory.FullName,
                "..",
                "..",
                "src",
                "Spendly.Domain"));

        if (!Directory.Exists(domainProjectDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Could not find the Spendly.Domain project directory at '{domainProjectDirectoryPath}'.");
        }

        return new DirectoryInfo(domainProjectDirectoryPath);
    }

    private static DirectoryInfo GetUnitTestsProjectDirectory()
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
