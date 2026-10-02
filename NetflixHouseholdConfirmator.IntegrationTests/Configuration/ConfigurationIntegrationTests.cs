using System.IO;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Configuration;

namespace NetflixHouseholdConfirmator.IntegrationTests.Configuration
{
    [TestFixture]
    public sealed class ConfigurationIntegrationTests
    {
        [Test]
        public void GivenTheShippedConfiguration_WhenBindingSettings_ThenEveryValueIsLoaded()
        {
            string configurationPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "appsettings.json");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configurationPath, false)
                .Build();
            BotSettings botSettings = new();
            DebugSettings debugSettings = new();
            ImapSettings imapSettings = new();

            configuration.Bind(nameof(BotSettings), botSettings);
            configuration.Bind(nameof(DebugSettings), debugSettings);
            configuration.Bind(nameof(ImapSettings), imapSettings);

            Assert.Multiple(() =>
            {
                Assert.That(botSettings.PageLoadTimeout, Is.EqualTo(90));
                Assert.That(debugSettings.CrashScreenshotFileName, Is.EqualTo("crash.png"));
                Assert.That(debugSettings.IsDebugMode, Is.False);
                Assert.That(debugSettings.IsCrashScreenshotEnabled, Is.True);
                Assert.That(imapSettings.Server, Is.EqualTo("[[IMAP_SERVER]]"));
                Assert.That(imapSettings.Port, Is.EqualTo(993));
                Assert.That(imapSettings.Username, Is.EqualTo("[[IMAP_USERNAME]]"));
                Assert.That(imapSettings.Password, Is.EqualTo("[[IMAP_PASSWORD]]"));
                Assert.That(imapSettings.MaxEmailAge, Is.EqualTo(1800));
            });
        }
    }
}