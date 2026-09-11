using NUnit.Framework;

using NetflixHouseholdConfirmator.Configuration;

namespace NetflixHouseholdConfirmator.UnitTests.Configuration
{
    [TestFixture]
    public sealed class BotSettingsTests
    {
        private BotSettings botSettings = null!;

        [SetUp]
        public void SetUp()
            => botSettings = new();

        [TestCase(-4)]
        [TestCase(0)]
        [TestCase(4)]
        [TestCase(8192)]
        [TestCase(int.MaxValue)]
        public void GivenAPageLoadTimeout_WhenAssigningIt_ThenTheValueIsRetained(int pageLoadTimeout)
        {
            botSettings.PageLoadTimeout = pageLoadTimeout;

            Assert.That(botSettings.PageLoadTimeout, Is.EqualTo(pageLoadTimeout));
        }
    }
}