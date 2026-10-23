using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NetflixHouseholdConfirmator.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyBoundaryTests
    {
        private static readonly Architecture architecture = new ArchLoader()
            .LoadAssemblies(typeof(Program).Assembly)
            .Build();

        [TestCase("MailKit.")]
        [TestCase("MimeKit.")]
        [TestCase("OpenQA.Selenium.")]
        [TestCase("NuciWeb.")]
        [TestCase("Microsoft.Extensions.DependencyInjection.")]
        [TestCase("Microsoft.Extensions.Configuration.")]
        public void GivenOrchestration_WhenInspectingDependencies_ThenIntegrationDetailsRemainOutside(
            string forbiddenPrefix)
            => AssertNoDependency(
                "NetflixHouseholdConfirmator.Service.HouseholdConfirmator",
                forbiddenPrefix,
                "Orchestration must delegate protocol and composition details to their owners.");

        [TestCase("NetflixHouseholdConfirmator.Service.Processors.EmailProcessor")]
        [TestCase("NetflixHouseholdConfirmator.Service.Processors.NetflixProcessor")]
        public void GivenOrchestration_WhenInspectingProcessors_ThenOnlyContractsAreConsumed(
            string concreteProcessor)
            => AssertNoDependency(
                "NetflixHouseholdConfirmator.Service.HouseholdConfirmator",
                concreteProcessor,
                "The orchestrator must consume processor interfaces, not concrete adapters.");

        [TestCase("EmailProcessor", "NuciWeb.")]
        [TestCase("EmailProcessor", "OpenQA.Selenium.")]
        [TestCase("EmailProcessor", "NetflixProcessor")]
        [TestCase("EmailProcessor", "INetflixProcessor")]
        [TestCase("NetflixProcessor", "MailKit.")]
        [TestCase("NetflixProcessor", "MimeKit.")]
        [TestCase("NetflixProcessor", "EmailProcessor")]
        [TestCase("NetflixProcessor", "IEmailProcessor")]
        public void GivenProcessors_WhenInspectingDependencies_ThenIntegrationBoundariesRemainIndependent(
            string processorName,
            string forbiddenName)
            => AssertNoDependency(
                $"NetflixHouseholdConfirmator.Service.Processors.{processorName}",
                forbiddenName,
                "Email and browser processors must remain independent integration boundaries.");

        [TestCase("NetflixHouseholdConfirmator.Configuration")]
        [TestCase("NetflixHouseholdConfirmator.Logging")]
        [TestCase("NetflixHouseholdConfirmator.Service")]
        [TestCase("NetflixHouseholdConfirmator.Service.Processors")]
        public void GivenRuntimeAreas_WhenInspectingDependencies_ThenTheyDoNotDependOnTheHost(
            string sourceNamespace)
        {
            IObjectProvider<IType> source = Types().That().ResideInNamespace(sourceNamespace);
            Assert.That(source.GetObjects(architecture), Is.Not.Empty);

            Types().That().Are(source).Should().NotDependOnAny(Types().That().Are(typeof(Program)))
                .Because("Program is the composition root and must not be consumed by runtime areas.")
                .Check(architecture);
        }

        [TestCase("NetflixHouseholdConfirmator.Configuration", "NetflixHouseholdConfirmator.Service")]
        [TestCase("NetflixHouseholdConfirmator.Configuration", "NetflixHouseholdConfirmator.Logging")]
        [TestCase("NetflixHouseholdConfirmator.Logging", "NetflixHouseholdConfirmator.Service")]
        [TestCase("NetflixHouseholdConfirmator.Logging", "NetflixHouseholdConfirmator.Configuration")]
        [TestCase(
            "NetflixHouseholdConfirmator.Service.Processors",
            "NetflixHouseholdConfirmator.Service.HouseholdConfirmator")]
        public void GivenSupportingAreas_WhenInspectingDependencies_ThenDependencyCyclesArePrevented(
            string sourceNamespace,
            string forbiddenName)
        {
            IObjectProvider<IType> source = Types().That().ResideInNamespace(sourceNamespace);
            Assert.That(source.GetObjects(architecture), Is.Not.Empty);

            Types().That().Are(source).Should()
                .NotDependOnAny(Types().That().HaveFullNameContaining(forbiddenName))
                .Because("Supporting areas and processors must not depend on their consumers.")
                .Check(architecture);
        }

        [TestCase("NUnit.")]
        [TestCase("Moq.")]
        [TestCase("ArchUnitNET.")]
        [TestCase("NetflixHouseholdConfirmator.UnitTests.")]
        [TestCase("NetflixHouseholdConfirmator.IntegrationTests.")]
        [TestCase("NetflixHouseholdConfirmator.ArchitectureTests.")]
        public void GivenProductionTypes_WhenInspectingDependencies_ThenTestCodeIsExcluded(
            string forbiddenPrefix)
        {
            IObjectProvider<IType> source = Types().That().ResideInAssembly(typeof(Program).Assembly);
            Assert.That(source.GetObjects(architecture), Is.Not.Empty);

            Types().That().Are(source).Should()
                .NotDependOnAny(Types().That().HaveFullNameContaining(forbiddenPrefix))
                .Because("Production assemblies must not depend on test frameworks or test projects.")
                .Check(architecture);
        }

        private static void AssertNoDependency(
            string sourceName,
            string forbiddenName,
            string reason)
        {
            IObjectProvider<IType> source = Types().That().HaveFullName(sourceName);
            Assert.That(source.GetObjects(architecture), Is.Not.Empty, $"Absent rule subject: {sourceName}.");

            Types().That().Are(source).Should()
                .NotDependOnAny(Types().That().HaveFullNameContaining(forbiddenName))
                .Because(reason)
                .Check(architecture);
        }
    }
}