using System;

using Moq;

using NuciLog.Core;

using NuciWeb;
using NuciWeb.Automation;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Service;
using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.IntegrationTests.Service
{
    [TestFixture]
    public sealed class HouseholdConfirmationIntegrationTests
    {
        private Mock<IEmailProcessor> emailProcessorMock = null!;
        private Mock<ILogger> loggerMock = null!;
        private Mock<IWebProcessor> webProcessorMock = null!;
        private HouseholdConfirmator householdConfirmator = null!;

        private static string ConfirmationEmailSubject
            => "How to update your Netflix Household";

        private static string ConfirmationUrl
            => "https://test.nucilandia.ro/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string ConfirmButtonSelector
            => Select.ByXPath("//button[@data-uia='set-primary-location-action']");

        private static string DateReceivedHeaderName => "DateReceived";

        private static string DefaultTimestampFormat
            => "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";

        private static string ExceptionMessage
            => "All went well, until it didn't";

        private static string LocationDetailsSelector
            => Select.ByXPath("//div[@data-uia='location-details']");

        private static int MaximumEmailAgeSeconds => 64;

        private static string PollingStoppedMessage => "Polling stopped.";

        private static string SecondConfirmationUrl
            => "https://test.nucilandia.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        [SetUp]
        public void SetUp()
        {
            emailProcessorMock = new();
            loggerMock = new();
            webProcessorMock = new();
            NetflixProcessor netflixProcessor = new(
                webProcessorMock.Object,
                loggerMock.Object);
            householdConfirmator = new(
                emailProcessorMock.Object,
                netflixProcessor,
                loggerMock.Object);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState(
            bool areLocationDetailsVisible)
        {
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(areLocationDetailsVisible);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
            webProcessorMock.Verify(
                webProcessor => webProcessor.WaitForAnyElementToBeVisible(
                    ConfirmButtonSelector,
                    LocationDetailsSelector),
                Times.Once);

            if (areLocationDetailsVisible)
            {
                webProcessorMock.Verify(
                    webProcessor => webProcessor.Click(It.IsAny<string>()),
                    Times.Never);
                webProcessorMock.Verify(
                    webProcessor => webProcessor.Wait(It.IsAny<int>()),
                    Times.Never);
            }
            else
            {
                webProcessorMock.Verify(
                    webProcessor => webProcessor.Click(ConfirmButtonSelector),
                    Times.Once);
                webProcessorMock.Verify(
                    webProcessor => webProcessor.Wait(5000),
                    Times.Once);
            }
        }

        [Test]
        public void GivenSuccessiveConfirmationEmails_WhenListening_ThenEveryNewRequestIsOpened()
        {
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Returns(SecondConfirmationUrl)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(SecondConfirmationUrl),
                Times.Once);
        }

        [Test]
        public void GivenTheSameConfirmationEmailTwice_WhenListening_ThenItIsOpenedOnce()
        {
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Returns((string)null!)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
        }

        [Test]
        public void GivenAnEmptyInbox_WhenListening_ThenTheBrowserIsNotOpened()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new InvalidOperationException(PollingStoppedMessage));

            AssertPollingStops();

            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenOnlyNonQualifyingEmails_WhenListening_ThenTheBrowserIsNotOpened()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new InvalidOperationException(PollingStoppedMessage));

            AssertPollingStops();

            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenAStaleNewestEmail_WhenListening_ThenOlderEmailsAreNotInspected()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new InvalidOperationException(PollingStoppedMessage));

            AssertPollingStops();

            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenMatchingHtmlWithoutAUrl_WhenListening_ThenTheRawBodyIsOpened()
        {
            string htmlBody = "A day on Venus is longer than a year on Venus";
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(htmlBody)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(htmlBody),
                Times.Once);
        }

        [Test]
        public void GivenAMatchingPlainTextEmail_WhenListening_ThenPollingFailsAndTheMailboxIsClosed()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new NullReferenceException());

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<NullReferenceException>());
            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenAnInvalidReceivedDate_WhenListening_ThenPollingFailsAndTheMailboxIsClosed()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new FormatException());

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<FormatException>());
            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenInboxRetrievalFails_WhenListening_ThenTheFailurePropagatesAndTheMailboxIsClosed()
        {
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(new InvalidOperationException(ExceptionMessage));

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenBrowserNavigationFails_WhenListening_ThenTheNextRequestIsStillOpened()
        {
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Returns(SecondConfirmationUrl)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.GoToUrl(ConfirmationUrl))
                .Throws(new InvalidOperationException(ExceptionMessage));
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(SecondConfirmationUrl),
                Times.Once);
        }

        [TestCase(BrowserFailureStage.ElementWaiting)]
        [TestCase(BrowserFailureStage.VisibilityDetection)]
        [TestCase(BrowserFailureStage.ButtonClicking)]
        [TestCase(BrowserFailureStage.PostConfirmationWaiting)]
        public void GivenABrowserOperationFails_WhenListening_ThenTheFailureIsSwallowedAndTheMailboxIsClosed(
            BrowserFailureStage failureStage)
        {
            InvalidOperationException browserException = new(ExceptionMessage);
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            ConfigureBrowserFailure(failureStage, browserException);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
        }



        private void AssertPollingStops()
            => Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(PollingStoppedMessage));



        private void ConfigureBrowserFailure(
            BrowserFailureStage failureStage,
            Exception exception)
        {
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(false);

            switch (failureStage)
            {
                case BrowserFailureStage.ElementWaiting:
                    webProcessorMock
                        .Setup(webProcessor => webProcessor.WaitForAnyElementToBeVisible(
                            ConfirmButtonSelector,
                            LocationDetailsSelector))
                        .Throws(exception);
                    break;
                case BrowserFailureStage.VisibilityDetection:
                    webProcessorMock
                        .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                        .Throws(exception);
                    break;
                case BrowserFailureStage.ButtonClicking:
                    webProcessorMock
                        .Setup(webProcessor => webProcessor.Click(ConfirmButtonSelector))
                        .Throws(exception);
                    break;
                case BrowserFailureStage.PostConfirmationWaiting:
                    webProcessorMock
                        .Setup(webProcessor => webProcessor.Wait(5000))
                        .Throws(exception);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failureStage));
            }
        }

        private void VerifyBrowserWasNotOpened()
            => webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(It.IsAny<string>()),
                Times.Never);
    }
}