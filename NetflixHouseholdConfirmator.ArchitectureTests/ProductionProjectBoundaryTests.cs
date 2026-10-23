using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using NUnit.Framework;

namespace NetflixHouseholdConfirmator.ArchitectureTests
{
    [TestFixture]
    public sealed class ProductionProjectBoundaryTests
    {
        private static readonly Assembly productionAssembly = typeof(Program).Assembly;

        [Test]
        public void GivenProductionAssemblies_WhenInspectingReferences_ThenTestDependenciesAreAbsent()
        {
            AssemblyName[] references = productionAssembly.GetReferencedAssemblies();
            Assert.That(references, Is.Not.Empty);

            Assert.That(
                references.Select(reference => reference.Name).Where(IsTestDependency),
                Is.Empty,
                "The executable must not reference test assemblies or test frameworks.");
        }

        [Test]
        public void GivenProductionAssemblies_WhenInspectingVisibility_ThenInternalsAreNotExposedToTests()
            => Assert.That(
                productionAssembly.GetCustomAttributes<InternalsVisibleToAttribute>(),
                Is.Empty,
                "Production assemblies must not expose internals through InternalsVisibleTo.");

        [Test]
        public void GivenProductionProjects_WhenInspectingReferences_ThenTestDependenciesAreAbsent()
        {
            string[] projectPaths = GetProductionProjectPaths();

            foreach (string projectPath in projectPaths)
            {
                XDocument project = XDocument.Load(projectPath);
                string[] forbiddenReferences = project.Descendants()
                    .Where(element => IsReferenceElement(element.Name.LocalName))
                    .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                    .Where(IsTestDependency)
                    .ToArray();

                Assert.That(
                    forbiddenReferences,
                    Is.Empty,
                    $"Production project {projectPath} must not declare dependencies on test code.");
            }
        }

        [Test]
        public void GivenProductionProjects_WhenInspectingVisibility_ThenInternalsVisibleToIsNotDeclared()
        {
            foreach (string projectPath in GetProductionProjectPaths())
            {
                XDocument project = XDocument.Load(projectPath);
                XElement[] visibilityDeclarations = project.Descendants()
                    .Where(element =>
                        string.Equals(element.Name.LocalName, "InternalsVisibleTo") ||
                        string.Equals(
                            element.Attribute("Include")?.Value,
                            typeof(InternalsVisibleToAttribute).FullName))
                    .ToArray();

                Assert.That(
                    visibilityDeclarations,
                    Is.Empty,
                    $"Production project {projectPath} must not declare InternalsVisibleTo.");
            }
        }

        private static string[] GetProductionProjectPaths()
        {
            string projectDirectory = Path.Combine(AppContext.BaseDirectory, "ProductionProjects");
            string[] projectPaths = Directory.GetFiles(projectDirectory, "*.csproj");
            Assert.That(projectPaths, Has.Length.EqualTo(1), "Every production project must be inspected.");

            return projectPaths;
        }

        private static bool IsReferenceElement(string elementName)
            => elementName is "ProjectReference" or "PackageReference" or "Reference";

        private static bool IsTestDependency(string? referenceName)
            => referenceName is not null &&
                (referenceName.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                referenceName.Contains("coverlet", StringComparison.OrdinalIgnoreCase));
    }
}