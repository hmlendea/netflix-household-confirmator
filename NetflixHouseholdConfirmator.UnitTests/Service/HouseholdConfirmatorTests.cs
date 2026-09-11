using System;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Service;
using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.UnitTests.Service
{
    [TestFixture]
    public sealed class HouseholdConfirmatorTests
    {
        private Mock<IEmailProcessor> emailProcessorMock = null!;
        private Mock<ILogger> loggerMock = null!;
        private Mock<INetflixProcessor> netflixProcessorMock = null!;
        private HouseholdConfirmator householdConfirmator = null!;

        private static string ConfirmationUrl
            => "https://test.url.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string SecondConfirmationUrl
            => "https://test.url.ro/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string ExceptionMessage
            => "All went well, until it didn't";

        private static string ListeningMessage
            => "Listening for incoming household update requests.";

        [SetUp]
        public void SetUp()
        {
            emailProcessorMock = new();
            loggerMock = new();
            netflixProcessorMock = new();
            householdConfirmator = new(
                emailProcessorMock.Object,
                netflixProcessorMock.Object,
                loggerMock.Object);
        }

        [Test]
        public void GivenAConfirmationUrl_WhenListeningForRequests_ThenTheHouseholdIsConfirmed()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            netflixProcessorMock.Verify(
                netflixProcessor => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Times.Once);
        }

        [Test]
        public void GivenNoConfirmationUrl_WhenListeningForRequests_ThenNoHouseholdIsConfirmed()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns((string)null!)
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            netflixProcessorMock.Verify(
                netflixProcessor => netflixProcessor.ConfirmHousehold(It.IsAny<string>()),
                Times.Never);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void GivenANonNullConfirmationUrl_WhenListeningForRequests_ThenTheValueIsForwarded(
            string confirmationUrl)
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(confirmationUrl)
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            netflixProcessorMock.Verify(
                netflixProcessor => netflixProcessor.ConfirmHousehold(confirmationUrl),
                Times.Once);
        }

        [Test]
        public void GivenMultipleConfirmationUrls_WhenListeningForRequests_ThenEveryHouseholdIsConfirmed()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .SetupSequence(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl)
                .Returns(SecondConfirmationUrl)
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            netflixProcessorMock.Verify(
                netflixProcessor => netflixProcessor.ConfirmHousehold(ConfirmationUrl),
                Times.Once);
            netflixProcessorMock.Verify(
                netflixProcessor => netflixProcessor.ConfirmHousehold(SecondConfirmationUrl),
                Times.Once);
        }

        [Test]
        public void GivenAnEmailProcessor_WhenListeningForRequests_ThenTheMailboxIsLoggedIntoOnce()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogIn(),
                Times.Once);
        }

        [Test]
        public void GivenAStartedListener_WhenListeningForRequests_ThenTheStartedStateIsLogged()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>());
            loggerMock.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Started.Name,
                        StringComparison.Ordinal)),
                    ListeningMessage),
                Times.Once);
        }

        [Test]
        public void GivenEmailPollingFails_WhenListeningForRequests_ThenTheFailureIsLoggedAndRethrown()
        {
            InvalidOperationException pollingException = new(ExceptionMessage);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(pollingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Failure.Name,
                        StringComparison.Ordinal)),
                    pollingException),
                Times.Once);
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogOut(),
                Times.Once);
        }

        [Test]
        public void GivenHouseholdConfirmationFails_WhenListeningForRequests_ThenTheFailureIsLoggedAndRethrown()
        {
            InvalidOperationException confirmationException = new(ExceptionMessage);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Returns(ConfirmationUrl);
            netflixProcessorMock
                .Setup(netflixProcessor => netflixProcessor.ConfirmHousehold(ConfirmationUrl))
                .Throws(confirmationException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Failure.Name,
                        StringComparison.Ordinal)),
                    confirmationException),
                Times.Once);
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogOut(),
                Times.Once);
        }

        [Test]
        public void GivenMailboxLoginFails_WhenListeningForRequests_ThenProcessingDoesNotStart()
        {
            InvalidOperationException loginException = new(ExceptionMessage);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.LogIn())
                .Throws(loginException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.GetHouseholdConfirmationUrl(),
                Times.Never);
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogOut(),
                Times.Never);
            loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<Exception>()),
                Times.Never);
        }

        [Test]
        public void GivenStartedStateLoggingFails_WhenListeningForRequests_ThenProcessingDoesNotStart()
        {
            InvalidOperationException loggingException = new(ExceptionMessage);
            loggerMock
                .Setup(logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    ListeningMessage))
                .Throws(loggingException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogIn(),
                Times.Once);
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.GetHouseholdConfirmationUrl(),
                Times.Never);
            emailProcessorMock.Verify(
                emailProcessor => emailProcessor.LogOut(),
                Times.Never);
        }

        [Test]
        public void GivenMailboxLogoutFails_WhenProcessingAlsoFails_ThenTheLogoutFailureIsRethrown()
        {
            ArgumentException pollingException = new(ExceptionMessage);
            InvalidOperationException logoutException = new("Aaaaaargghh");
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.GetHouseholdConfirmationUrl())
                .Throws(pollingException);
            emailProcessorMock
                .Setup(emailProcessor => emailProcessor.LogOut())
                .Throws(logoutException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Aaaaaargghh"));
            loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Failure.Name,
                        StringComparison.Ordinal)),
                    pollingException),
                Times.Once);
        }
    }
}