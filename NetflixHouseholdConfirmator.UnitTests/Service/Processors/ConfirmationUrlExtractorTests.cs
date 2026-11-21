using System;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.UnitTests.Service.Processors
{
    [TestFixture]
    public sealed class ConfirmationUrlExtractorTests
    {
        private static string ConfirmationUrl
            => "https://www.netflix.com/account/update-primary-location?nftoken=Token%2BValue%3D%3D" +
                "&g=Identifier&lnktrk=EVO&operation=update" +
                "&lkid=UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string EscapedConfirmationUrl
            => ConfirmationUrl.Replace("&", "&amp;");

        private static string EscapedEmailBody
            => $"<a href=\"{EscapedConfirmationUrl}\">Yes, This Was Me</a>";

        private static string UnmatchedEmailBody
            => "A day on Venus is longer than a year on Venus";

        [Test]
        public void GivenAnHtmlEscapedConfirmationUrl_WhenExtracting_ThenTheUrlIsDecoded()
            => Assert.That(
                ConfirmationUrlExtractor.Extract(EscapedEmailBody),
                Is.EqualTo(ConfirmationUrl));

        [Test]
        public void GivenAPlainConfirmationUrl_WhenExtracting_ThenTheUrlIsReturnedUnchanged()
            => Assert.That(
                ConfirmationUrlExtractor.Extract($"<a href=\"{ConfirmationUrl}\">Confirm</a>"),
                Is.EqualTo(ConfirmationUrl));

        [Test]
        public void GivenAnEscapedUrl_WhenExtracting_ThenTheOperationParameterSurvives()
            => Assert.That(
                ConfirmationUrlExtractor.Extract(EscapedEmailBody),
                Does.Contain("&operation=update"));

        [Test]
        public void GivenAHtmlBodyWithoutAConfirmationUrl_WhenExtracting_ThenTheBodyIsReturnedUnchanged()
            => Assert.That(
                ConfirmationUrlExtractor.Extract(UnmatchedEmailBody),
                Is.EqualTo(UnmatchedEmailBody));

        [Test]
        public void GivenAHtmlBodyWithNewLines_WhenExtracting_ThenTheNewLinesAreRemoved()
            => Assert.That(
                ConfirmationUrlExtractor.Extract($"<a href=\"{ConfirmationUrl}\">Confirm</a>{Environment.NewLine}"),
                Is.EqualTo(ConfirmationUrl));
    }
}
