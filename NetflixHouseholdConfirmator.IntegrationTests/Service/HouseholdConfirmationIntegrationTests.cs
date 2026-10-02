using System;
using System.Collections.Generic;
using System.Globalization;

using MailKit;
using MailKit.Net.Imap;

using MimeKit;

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
        private ImapSettings imapSettings = null!;
        private Mock<IImapClient> imapClientMock = null!;
        private Mock<IMailFolder> inboxMock = null!;
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
            webProcessorMock = new();
            imapClientMock
                .SetupGet(imapClient => imapClient.Inbox)
                .Returns(inboxMock.Object);

            EmailProcessor emailProcessor = new(
                imapSettings,
                loggerMock.Object,
                imapClientMock.Object);
            NetflixProcessor netflixProcessor = new(
                webProcessorMock.Object,
                loggerMock.Object);
            householdConfirmator = new(
                emailProcessor,
                netflixProcessor,
                loggerMock.Object);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState(
            bool areLocationDetailsVisible)
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                $"Leading text{Environment.NewLine}{ConfirmationUrl} trailing text",
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            ConfigureSinglePoll(email);
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

            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenSuccessiveConfirmationEmails_WhenListening_ThenEveryNewRequestIsOpened()
        {
            MimeMessage firstEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            MimeMessage secondEmail = BuildEmail(
                ConfirmationEmailSubject,
                SecondConfirmationUrl,
                DateTime.Now.AddMinutes(2),
                DateTimeOffset.Now);
            inboxMock
                .SetupSequence(inbox => inbox.Count)
                .Returns(1)
                .Returns(1)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            inboxMock
                .SetupSequence(inbox => inbox.GetMessage(0, default, null))
                .Returns(firstEmail)
                .Returns(secondEmail);
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
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenTheSameConfirmationEmailTwice_WhenListening_ThenItIsOpenedOnce()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            inboxMock
                .SetupSequence(inbox => inbox.Count)
                .Returns(1)
                .Returns(1)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            inboxMock
                .Setup(inbox => inbox.GetMessage(0, default, null))
                .Returns(email);
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenAnEmptyInbox_WhenListening_ThenTheBrowserIsNotOpened()
        {
            ConfigureSinglePoll();

            AssertPollingStops();

            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenOnlyNonQualifyingEmails_WhenListening_ThenTheBrowserIsNotOpened()
        {
            MimeMessage oldConfirmationEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(-1),
                DateTimeOffset.Now);
            MimeMessage differentlyCasedEmail = BuildEmail(
                ConfirmationEmailSubject.ToUpperInvariant(),
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            MimeMessage unrelatedEmail = BuildEmail(
                "Chuck Norris can divide by zero.",
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            ConfigureSinglePoll(
                oldConfirmationEmail,
                differentlyCasedEmail,
                unrelatedEmail);

            AssertPollingStops();

            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenAStaleNewestEmail_WhenListening_ThenOlderEmailsAreNotInspected()
        {
            MimeMessage validEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            MimeMessage staleEmail = BuildEmail(
                "Stale email",
                string.Empty,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now.AddSeconds(-128));
            ConfigureSinglePoll(validEmail, staleEmail);

            AssertPollingStops();

            inboxMock.Verify(
                inbox => inbox.GetMessage(0, default, null),
                Times.Never);
            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenMatchingHtmlWithoutAUrl_WhenListening_ThenTheRawBodyIsOpened()
        {
            string htmlBody = "A day on Venus is longer than a year on Venus";
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                htmlBody,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            ConfigureSinglePoll(email);
            webProcessorMock
                .Setup(webProcessor => webProcessor.IsElementVisible(LocationDetailsSelector))
                .Returns(true);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(htmlBody),
                Times.Once);
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenAMatchingPlainTextEmail_WhenListening_ThenPollingFailsAndTheMailboxIsClosed()
        {
            MimeMessage email = BuildPlainTextEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            ConfigureInbox([email]);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<NullReferenceException>());
            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenAnInvalidReceivedDate_WhenListening_ThenPollingFailsAndTheMailboxIsClosed()
        {
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            email.Headers.Remove(DateReceivedHeaderName);
            email.Headers.Add(DateReceivedHeaderName, "Aaaaaargghh");
            ConfigureInbox([email]);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<FormatException>());
            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenInboxRetrievalFails_WhenListening_ThenTheFailurePropagatesAndTheMailboxIsClosed()
        {
            InvalidOperationException retrievalException = new(ExceptionMessage);
            inboxMock
                .Setup(inbox => inbox.Open(FolderAccess.ReadOnly, default))
                .Throws(retrievalException);

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            VerifyBrowserWasNotOpened();
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenBrowserNavigationFails_WhenListening_ThenTheNextRequestIsStillOpened()
        {
            MimeMessage firstEmail = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            MimeMessage secondEmail = BuildEmail(
                ConfirmationEmailSubject,
                SecondConfirmationUrl,
                DateTime.Now.AddMinutes(2),
                DateTimeOffset.Now);
            inboxMock
                .SetupSequence(inbox => inbox.Count)
                .Returns(1)
                .Returns(1)
                .Throws(new InvalidOperationException(PollingStoppedMessage));
            inboxMock
                .SetupSequence(inbox => inbox.GetMessage(0, default, null))
                .Returns(firstEmail)
                .Returns(secondEmail);
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
            VerifyMailboxLifecycleCompleted();
        }

        [TestCase(BrowserFailureStage.ElementWaiting)]
        [TestCase(BrowserFailureStage.VisibilityDetection)]
        [TestCase(BrowserFailureStage.ButtonClicking)]
        [TestCase(BrowserFailureStage.PostConfirmationWaiting)]
        public void GivenABrowserOperationFails_WhenListening_ThenTheFailureIsSwallowedAndTheMailboxIsClosed(
            BrowserFailureStage failureStage)
        {
            InvalidOperationException browserException = new(ExceptionMessage);
            MimeMessage email = BuildEmail(
                ConfirmationEmailSubject,
                ConfirmationUrl,
                DateTime.Now.AddMinutes(1),
                DateTimeOffset.Now);
            ConfigureSinglePoll(email);
            ConfigureBrowserFailure(failureStage, browserException);

            AssertPollingStops();

            webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(ConfirmationUrl),
                Times.Once);
            VerifyMailboxLifecycleCompleted();
        }

        [Test]
        public void GivenImapConnectionFails_WhenListening_ThenAuthenticationAndLogoutDoNotRun()
        {
            imapClientMock
                .Setup(imapClient => imapClient.Connect(
                    imapSettings.Server,
                    imapSettings.Port,
                    true,
                    default))
                .Throws(new InvalidOperationException(ExceptionMessage));

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            imapClientMock.Verify(
                imapClient => imapClient.Authenticate(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    default),
                Times.Never);
            imapClientMock.Verify(
                imapClient => imapClient.Disconnect(true, default),
                Times.Never);
            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenImapAuthenticationFails_WhenListening_ThenPollingAndLogoutDoNotRun()
        {
            imapClientMock
                .Setup(imapClient => imapClient.Authenticate(
                    imapSettings.Username,
                    imapSettings.Password,
                    default))
                .Throws(new InvalidOperationException(ExceptionMessage));

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ExceptionMessage));
            inboxMock.Verify(
                inbox => inbox.Open(It.IsAny<FolderAccess>(), default),
                Times.Never);
            imapClientMock.Verify(
                imapClient => imapClient.Disconnect(true, default),
                Times.Never);
            VerifyBrowserWasNotOpened();
        }

        [Test]
        public void GivenPollingAndLogoutFail_WhenListening_ThenTheLogoutFailureIsPropagated()
        {
            inboxMock
                .SetupGet(inbox => inbox.Count)
                .Throws(new ArgumentException(ExceptionMessage));
            imapClientMock
                .Setup(imapClient => imapClient.Disconnect(true, default))
                .Throws(new InvalidOperationException("Aaaaaargghh"));

            Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Aaaaaargghh"));
            imapClientMock.Verify(
                imapClient => imapClient.Dispose(),
                Times.Never);
            VerifyBrowserWasNotOpened();
        }

        private void AssertPollingStops()
            => Assert.That(
                () => householdConfirmator.ConfirmIncomingHouseholdUpdateRequests(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(PollingStoppedMessage));

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

        private static MimeMessage BuildPlainTextEmail(
            string subject,
            string textBody,
            DateTime dateReceived,
            DateTimeOffset emailDateTime)
        {
            MimeMessage email = new()
            {
                Subject = subject,
                Date = emailDateTime,
                Body = new TextPart("plain")
                {
                    Text = textBody
                }
            };
            email.Headers.Add(
                DateReceivedHeaderName,
                dateReceived.ToString(DefaultTimestampFormat, CultureInfo.InvariantCulture));

            return email;
        }

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

        private void ConfigureInbox(IReadOnlyList<MimeMessage> emails)
        {
            inboxMock
                .SetupGet(inbox => inbox.Count)
                .Returns(emails.Count);

            for (int emailIndex = 0; emailIndex < emails.Count; emailIndex += 1)
            {
                int configuredEmailIndex = emailIndex;
                inboxMock
                    .Setup(inbox => inbox.GetMessage(configuredEmailIndex, default, null))
                    .Returns(emails[configuredEmailIndex]);
            }
        }

        private void ConfigureSinglePoll(params MimeMessage[] emails)
        {
            inboxMock
                .SetupSequence(inbox => inbox.Count)
                .Returns(emails.Length)
                .Throws(new InvalidOperationException(PollingStoppedMessage));

            for (int emailIndex = 0; emailIndex < emails.Length; emailIndex += 1)
            {
                int configuredEmailIndex = emailIndex;
                inboxMock
                    .Setup(inbox => inbox.GetMessage(configuredEmailIndex, default, null))
                    .Returns(emails[configuredEmailIndex]);
            }
        }

        private void VerifyBrowserWasNotOpened()
            => webProcessorMock.Verify(
                webProcessor => webProcessor.GoToUrl(It.IsAny<string>()),
                Times.Never);

        private void VerifyMailboxLifecycleCompleted()
        {
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
            imapClientMock.Verify(
                imapClient => imapClient.Disconnect(true, default),
                Times.Once);
            imapClientMock.Verify(
                imapClient => imapClient.Dispose(),
                Times.Once);
        }
    }
}