using System;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.UnitTests.Service.Processors
{
    [TestFixture]
    public sealed class EmailProcessorTests
    {
        private ImapSettings imapSettings = null!;
        private Mock<ILogger> loggerMock = null!;
        private EmailProcessor emailProcessor = null!;

        private static int MaximumEmailAgeSeconds => 64;

        [SetUp]
        public void SetUp()
        {
            imapSettings = new()
            {
                Server = "test.nucilandia.ro",
                Port = 613,
                Username = "ilarion.pintilie@nucilandia.ro",
                Password = "NucileRullz!",
                MaxEmailAge = MaximumEmailAgeSeconds
            };
            loggerMock = new();
            emailProcessor = new(imapSettings, loggerMock.Object);
        }

        [Test]
        public void GivenSettingsAndALogger_WhenConstructingWithTheLegacyConstructor_ThenAnInstanceIsCreated()
            => Assert.That(
                new EmailProcessor(imapSettings, loggerMock.Object),
                Is.Not.Null);
    }
}