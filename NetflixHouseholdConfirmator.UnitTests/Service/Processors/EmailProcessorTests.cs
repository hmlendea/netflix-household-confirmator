using System;
using System.Collections.Generic;
using System.Globalization;

using MailKit;
using MailKit.Net.Imap;

using MimeKit;

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
        private Mock<IImapClient> imapClientMock = null!;
        private Mock<IMailFolder> inboxMock = null!;
        private Mock<ILogger> loggerMock = null!;
        private EmailProcessor emailProcessor = null!;

        private static string ConfirmationEmailSubject
            => "How to update your Netflix Household";

        private static string ConfirmationUrl
            => "https://test.url.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA";

        private static string ConnectionFailureMessage
            => "Failed to connect to the IMAP server.";

        private static string DateReceivedHeaderName => "DateReceived";

        private static string DefaultTimestampFormat
            => "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";

        private static string ExceptionMessage
            => "All went well, until it didn't";

        private static int MaximumEmailAgeSeconds => 64;

        private static DateTime NewConfirmationDateTime => DateTime.Now.AddDays(1);

        private static DateTime OldConfirmationDateTime => DateTime.Now.AddDays(-1);

        private static DateTimeOffset OldEmailDateTime
            => DateTimeOffset.Now.AddSeconds(-128);

        private static DateTimeOffset RecentEmailDateTime => DateTimeOffset.Now;

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
            imapClientMock = new();
            inboxMock = new();
            loggerMock = new();
            imapClientMock
                .SetupGet(imapClient => imapClient.Inbox)
                .Returns(inboxMock.Object);
            emailProcessor = new(
                imapSettings,
                loggerMock.Object,
                imapClientMock.Object);
        }

        [Test]
        public void GivenSettingsAndALogger_WhenConstructingWithTheLegacyConstructor_ThenAnInstanceIsCreated()
            => Assert.That(
                new EmailProcessor(imapSettings, loggerMock.Object),
                Is.Not.Null);

        [Test]
        public void GivenValidImapSettings_WhenLoggingIn_ThenTheClientConnectsAndAuthenticates()
        {
            emailProcessor.LogIn();

            imapClientMock.Verify(
                imapClient => imapClient.Connect(
                    imapSettings.Server,
                    imapSettings.Port,
                    true,
                    default),
                Times.Once);
            imapClientMock.Verify(
                imapClient => imapClient.Authenticate(
                    imapSettings.Username,
                    imapSettings.Password,
                    default),
                Times.Once);
        }

        [Test]
        public void GivenAConnectionFailure_WhenLoggingIn_ThenTheFailureIsLoggedAndRethrown()
        {
            InvalidOperationException connectionException = new(ExceptionMessage);
            imapClientMock
                .Setup(imapClient => imapClient.Connect(
                    imapSettings.Server,
                    imapSettings.Port,
                    true,
                    default))
                .Throws(connectionException);

            Assert.That(
                () => emailProcessor.LogIn(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            VerifyErrorWasLogged(ConnectionFailureMessage, connectionException);
            imapClientMock.Verify(
                imapClient => imapClient.Authenticate(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    default),
                Times.Never);
        }

        [Test]
        public void GivenAnAuthenticationFailure_WhenLoggingIn_ThenTheFailureIsLoggedAndRethrown()
        {
            InvalidOperationException authenticationException = new(ExceptionMessage);
            imapClientMock
                .Setup(imapClient => imapClient.Authenticate(
                    imapSettings.Username,
                    imapSettings.Password,
                    default))
                .Throws(authenticationException);

            Assert.That(
                () => emailProcessor.LogIn(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            VerifyErrorWasLogged(
                "Failed to authenticate on the IMAP server.",
                authenticationException);
        }

        [Test]
        public void GivenSuccessfulAuthentication_WhenLoggingIn_ThenEveryProgressStateIsLogged()
        {
            emailProcessor.LogIn();

            VerifyInformationWasLogged(OperationStatus.Started, Times.Once());
            VerifyInformationWasLogged(OperationStatus.InProgress, Times.Once());
            VerifyInformationWasLogged(OperationStatus.Success, Times.Once());
        }

        [Test]
        public void GivenAnAuthenticatedClient_WhenLoggingOut_ThenTheClientDisconnectsAndIsDisposed()
        {
            emailProcessor.LogOut();

            imapClientMock.Verify(
                imapClient => imapClient.Disconnect(true, default),
                Times.Once);
            imapClientMock.Verify(
                imapClient => imapClient.Dispose(),
                Times.Once);
        }

        [Test]
        public void GivenADisconnectionFailure_WhenLoggingOut_ThenTheFailureIsLoggedAndRethrown()
        {
            InvalidOperationException disconnectionException = new(ExceptionMessage);
            imapClientMock
                .Setup(imapClient => imapClient.Disconnect(true, default))
                .Throws(disconnectionException);

            Assert.That(
                () => emailProcessor.LogOut(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            VerifyErrorWasLogged(
                "Failed to disconnect from the IMAP server.",
                disconnectionException);
            imapClientMock.Verify(
                imapClient => imapClient.Dispose(),
                Times.Never);
        }

        [Test]
        public void GivenASuccessfulDisconnection_WhenLoggingOut_ThenTheStartedAndSuccessStatesAreLogged()
        {
            emailProcessor.LogOut();

            VerifyInformationWasLogged(OperationStatus.Started, Times.Once());
            VerifyInformationWasLogged(OperationStatus.Success, Times.Once());
        }

        [Test]
        public void GivenAnEmptyInbox_WhenRetrievingAConfirmationUrl_ThenNoUrlIsReturned()
        {
            ConfigureInbox([]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.Null);
            inboxMock.Verify(
                inbox => inbox.Open(FolderAccess.ReadOnly, default),
                Times.Once);
        }

        [Test]
        public void GivenAnUnrelatedEmail_WhenRetrievingAConfirmationUrl_ThenNoUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                "Chuck Norris can divide by zero.",
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.Null);
        }

        [Test]
        public void GivenADifferentlyCasedSubject_WhenRetrievingAConfirmationUrl_ThenNoUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject.ToUpperInvariant(),
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.Null);
        }

        [Test]
        public void GivenANewConfirmationEmail_WhenRetrievingAConfirmationUrl_ThenItsUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.EqualTo(ConfirmationUrl));
        }

        [Test]
        public void GivenASubjectContainingTheExpectedText_WhenRetrievingAConfirmationUrl_ThenItsUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                $"404: Not Found {ConfirmationEmailSubject}",
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.EqualTo(ConfirmationUrl));
        }

        [Test]
        public void GivenAnOldConfirmationEmail_WhenRetrievingAConfirmationUrl_ThenNoUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                OldConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.Null);
        }

        [Test]
        public void GivenMultipleNewEmails_WhenRetrievingAConfirmationUrl_ThenTheNewestMatchIsReturned()
        {
            MimeMessage olderEmail = BuildEmail(
                ConfirmationEmailSubject,
                "https://test.url.ro/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA",
                NewConfirmationDateTime.AddMinutes(-1),
                RecentEmailDateTime.AddMinutes(-1));
            MimeMessage newerEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([olderEmail, newerEmail]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.EqualTo(ConfirmationUrl));
        }

        [Test]
        public void GivenAnEmailBeyondTheMaximumAge_WhenRetrievingAConfirmationUrl_ThenScanningStops()
        {
            MimeMessage recentEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            MimeMessage oldEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                OldEmailDateTime);
            ConfigureInbox([recentEmail, oldEmail]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.Null);
            inboxMock.Verify(
                inbox => inbox.GetMessage(0, default, null),
                Times.Never);
        }

        [Test]
        public void GivenRecentHtmlWithLineBreaks_WhenRetrievingAConfirmationUrl_ThenTheUrlIsExtracted()
        {
            string htmlBody = $"All roads lead to Rome{Environment.NewLine}{ConfirmationUrl} trailing";
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                htmlBody,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.EqualTo(ConfirmationUrl));
        }

        [Test]
        public void GivenHtmlWithoutAConfirmationUrl_WhenRetrievingAConfirmationUrl_ThenTheBodyIsReturned()
        {
            string htmlBody = "A day on Venus is longer than a year on Venus";
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                htmlBody,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(confirmationUrl, Is.EqualTo(htmlBody));
        }

        [Test]
        public void GivenAProcessedConfirmationEmail_WhenRetrievingAgain_ThenNoUrlIsReturned()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            ConfigureInbox([email]);

            string firstConfirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();
            string secondConfirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

            Assert.That(firstConfirmationUrl, Is.EqualTo(ConfirmationUrl));
            Assert.That(secondConfirmationUrl, Is.Null);
        }

        [Test]
        public void GivenAnInvalidReceivedDate_WhenRetrievingAConfirmationUrl_ThenAFormatExceptionIsThrown()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                NewConfirmationDateTime,
                RecentEmailDateTime);
            email.Headers.Remove(DateReceivedHeaderName);
            email.Headers.Add(DateReceivedHeaderName, "Aaaaaargghh");
            ConfigureInbox([email]);

            Assert.That(
                () => emailProcessor.GetHouseholdConfirmationUrl(),
                Throws.TypeOf<FormatException>());
        }

        private static MimeMessage BuildEmail(
            string subject,
            string htmlBody,
            DateTime dateReceived,
            DateTimeOffset emailDateTime)
        {
            MimeMessage email = new()
            {
                Subject = subject,
                Date = emailDateTime,
                Body = new TextPart("html")
                {
                    Text = htmlBody
                }
            };
            email.Headers.Add(
                DateReceivedHeaderName,
                dateReceived.ToString(DefaultTimestampFormat, CultureInfo.InvariantCulture));

            return email;
        }

        private void ConfigureInbox(IEnumerable<MimeMessage> emails)
        {
            MimeMessage[] emailArray = [.. emails];
            inboxMock
                .SetupGet(inbox => inbox.Count)
                .Returns(emailArray.Length);

            for (int emailIndex = 0; emailIndex < emailArray.Length; emailIndex += 1)
            {
                int configuredEmailIndex = emailIndex;
                inboxMock
                    .Setup(inbox => inbox.GetMessage(configuredEmailIndex, default, null))
                    .Returns(emailArray[configuredEmailIndex]);
            }
        }

        private void VerifyErrorWasLogged(string message, Exception exception)
            => loggerMock.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        OperationStatus.Failure.Name,
                        StringComparison.Ordinal)),
                    message,
                    exception,
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Once);

        private void VerifyInformationWasLogged(OperationStatus expectedStatus, Times times)
            => loggerMock.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(operationStatus => string.Equals(
                        operationStatus.Name,
                        expectedStatus.Name,
                        StringComparison.Ordinal)),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                times);
    }
}