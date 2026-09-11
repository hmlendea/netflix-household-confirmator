using System;

using Moq;

using NuciLog.Core;

using NuciWeb;
using NuciWeb.Automation;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.UnitTests.Service.Processors
{
    [TestFixture]
    public sealed class NetflixProcessorTests
    {
        private Mock<ILogger> loggerMock = null!;
        private Mock<IWebProcessor> webProcessorMock = null!;
        private NetflixProcessor netflixProcessor = null!;

        private static int ConfirmationWaitMilliseconds => 5000;

        private static string ConfirmationUrl
            => "https://test.url.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string ConfirmButtonSelector
            => Select.ByXPath("//button[@data-uia='set-primary-location-action']");

        private static string ErrorMessage
            => "All went well, until it didn't";

        private static string FailureLogMessage
            => "An error has occurred while confirming the household.";

        private static string LocationDetailsSelector
            => Select.ByXPath("//div[@data-uia='location-details']");

        private static string StartedLogMessage
            => "Starting the household confirmation process.";

        private static string SuccessLogMessage
            => "The household was successfully confirmed.";

        [SetUp]
        public void SetUp()
        {
            loggerMock = new();
            webProcessorMock = new();
            netflixProcessor = new(webProcessorMock.Object, loggerMock.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("https://test.url.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA")]
        public void GivenAConfirmationUrl_WhenConfirmingAHousehold_ThenTheUrlIsOpened(
            string? confirmationUrl)
        {
            Assert.That(
                () => netflixProcessor.ConfirmHousehold(confirmationUrl!),
                Throws.Nothing);
            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(confirmationUrl!),
                Times.Once);
        }

        [Test]
        public void GivenTheConfirmationPage_WhenConfirmingAHousehold_ThenEitherResultElementIsAwaited()
        {
            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            webProcessorMock.Verify(
                webProcessor => webProcessor.WaitForAnyElementToBeVisible(
                    ConfirmButtonSelector,
                    LocationDetailsSelector),
                Times.Once);
        }

        [Test]
        public void GivenLocationDetailsAreVisible_WhenConfirmingAHousehold_ThenTheConfirmButtonIsNotClicked()
        {
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            webProcessorMock.Verify(
                webProcessor => webProcessor.Click(It.IsAny<string>()),
                Times.Never);
            webProcessorMock.Verify(
                webProcessor => webProcessor.Wait(It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public void GivenLocationDetailsAreNotVisible_WhenConfirmingAHousehold_ThenTheConfirmButtonIsClicked()
        {
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(false);

            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            webProcessorMock.Verify(
                webProcessor => webProcessor.Click(ConfirmButtonSelector),
                Times.Once);
            webProcessorMock.Verify(
                webProcessor => webProcessor.Wait(ConfirmationWaitMilliseconds),
                Times.Once);
        }

        [Test]
        public void GivenUrlNavigationFails_WhenConfirmingAHousehold_ThenTheFailureIsLoggedAndSwallowed()
        {
            InvalidOperationException navigationException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.GoToUrl(ConfirmationUrl))
                .Throws(navigationException);

            Assert.That(
                () => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Throws.Nothing);
            VerifyFailureWasLogged(navigationException);
            webProcessorMock.Verify(
                webProcessor => webProcessor.WaitForAnyElementToBeVisible(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public void GivenElementWaitingFails_WhenConfirmingAHousehold_ThenTheFailureIsLoggedAndSwallowed()
        {
            InvalidOperationException waitingException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.WaitForAnyElementToBeVisible(
                    ConfirmButtonSelector,
                    LocationDetailsSelector))
                .Throws(waitingException);

            Assert.That(
                () => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Throws.Nothing);
            VerifyFailureWasLogged(waitingException);
            webProcessorMock.Verify(
                webProcessor => webProcessor.IsElementVisible(It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public void GivenVisibilityDetectionFails_WhenConfirmingAHousehold_ThenTheFailureIsLoggedAndSwallowed()
        {
            InvalidOperationException visibilityException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Throws(visibilityException);

            Assert.That(
                () => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Throws.Nothing);
            VerifyFailureWasLogged(visibilityException);
            webProcessorMock.Verify(
                webProcessor => webProcessor.Click(It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public void GivenButtonClickingFails_WhenConfirmingAHousehold_ThenTheFailureIsLoggedAndSwallowed()
        {
            InvalidOperationException clickingException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.Click(ConfirmButtonSelector))
                .Throws(clickingException);

            Assert.That(
                () => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Throws.Nothing);
            VerifyFailureWasLogged(clickingException);
            webProcessorMock.Verify(
                webProcessor => webProcessor.Wait(It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public void GivenPostConfirmationWaitingFails_WhenConfirmingAHousehold_ThenTheFailureIsLoggedAndSwallowed()
        {
            InvalidOperationException waitingException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.Wait(ConfirmationWaitMilliseconds))
                .Throws(waitingException);

            Assert.That(
                () => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Throws.Nothing);
            VerifyFailureWasLogged(waitingException);
        }

        [Test]
        public void GivenConfirmationStarts_WhenConfirmingAHousehold_ThenTheStartedStateIsLogged()
        {
            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            loggerMock.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Started.Name,
                        StringComparison.Ordinal)),
                    StartedLogMessage),
                Times.Once);
        }

        [Test]
        public void GivenConfirmationSucceeds_WhenConfirmingAHousehold_ThenTheSuccessStateIsLogged()
        {
            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            loggerMock.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Success.Name,
                        StringComparison.Ordinal)),
                    SuccessLogMessage),
                Times.Once);
            loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()),
                Times.Never);
        }

        [Test]
        public void GivenConfirmationFails_WhenConfirmingAHousehold_ThenTheSuccessStateIsStillLogged()
        {
            InvalidOperationException navigationException = new(ErrorMessage);
            webProcessorMock
                .Setup(webProcessor => webProcessor.GoToUrl(ConfirmationUrl))
                .Throws(navigationException);

            netflixProcessor.ConfirmHousehold(ConfirmationUrl);

            loggerMock.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Success.Name,
                        StringComparison.Ordinal)),
                    SuccessLogMessage),
                Times.Once);
        }

        private void VerifyFailureWasLogged(Exception exception)
            => loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Failure.Name,
                        StringComparison.Ordinal)),
                    FailureLogMessage,
                    exception),
                Times.Once);
    }
}