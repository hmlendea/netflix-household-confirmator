using NUnit.Framework;

using NetflixHouseholdConfirmator.Configuration;

namespace NetflixHouseholdConfirmator.UnitTests.Configuration
{
    [TestFixture]
    public sealed class DebugSettingsTests
    {
        private DebugSettings debugSettings = null!;

        [SetUp]
        public void SetUp()
            => debugSettings = new();

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void GivenADebugModeValue_WhenCheckingWhetherExecutionIsHeadless_ThenTheInverseValueIsReturned(
            bool isDebugMode,
            bool expectedIsHeadless)
        {
            debugSettings.IsDebugMode = isDebugMode;

            Assert.That(debugSettings.IsHeadless, Is.EqualTo(expectedIsHeadless));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase("\t", false)]
        [TestCase("\r\n", false)]
        [TestCase("crash.png", true)]
        [TestCase(" crash.png ", true)]
        public void GivenACrashScreenshotFileName_WhenCheckingWhetherCaptureIsEnabled_ThenTheExpectedValueIsReturned(
            string? crashScreenshotFileName,
            bool expectedIsCrashScreenshotEnabled)
        {
            debugSettings.CrashScreenshotFileName = crashScreenshotFileName!;

            Assert.That(
                debugSettings.IsCrashScreenshotEnabled,
                Is.EqualTo(expectedIsCrashScreenshotEnabled));
        }
    }
}