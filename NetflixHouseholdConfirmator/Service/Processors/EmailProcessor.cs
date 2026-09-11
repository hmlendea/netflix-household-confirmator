using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using MailKit;
using MailKit.Net.Imap;

using MimeKit;

using NuciLog.Core;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Logging;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public sealed class EmailProcessor(
        ImapSettings imapSettings,
        ILogger logger,
        IImapClient imapClient) : IEmailProcessor
    {
        private readonly ImapSettings imapSettings = imapSettings;
        private readonly ILogger logger = logger;
        private readonly IImapClient imapClient = imapClient;

        private DateTime lastConfirmationEmailDateTime = DateTime.Now;

        private static string ConfirmationUrlPattern
            => ".*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*";

        private static string ConfirmationUrlReplacement => "$1";

        private static string HouseholdUpdateEmailSubject
            => "How to update your Netflix Household";

        public EmailProcessor(ImapSettings imapSettings, ILogger logger)
            : this(imapSettings, logger, new ImapClient())
        {
        }

        public void LogIn()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Server, imapSettings.Server),
                new(MyLogInfoKey.Port, imapSettings.Port)
            ];

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.Started,
                "Connecting to the IMAP server.",
                logInfos);

            try
            {
                imapClient.Connect(imapSettings.Server, imapSettings.Port, true);
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.EmailLogIn,
                    OperationStatus.Failure,
                    "Failed to connect to the IMAP server.",
                    exception,
                    logInfos);

                throw;
            }

            logInfos = logInfos.Append(new(MyLogInfoKey.Username, imapSettings.Username));

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.InProgress,
                "Authenticating on the IMAP server.",
                logInfos);

            try
            {
                imapClient.Authenticate(imapSettings.Username, imapSettings.Password);
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.EmailLogIn,
                    OperationStatus.Failure,
                    "Failed to authenticate on the IMAP server.",
                    exception,
                    logInfos);

                throw;
            }

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.Success,
                "Logged into the IMAP server.",
                logInfos);
        }

        public void LogOut()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Server, imapSettings.Server),
                new(MyLogInfoKey.Port, imapSettings.Port),
                new(MyLogInfoKey.Username, imapSettings.Username)
            ];

            logger.Info(
                MyOperation.EmailLogOut,
                OperationStatus.Started,
                "Disconnecting from the IMAP server.",
                logInfos);

            try
            {
                imapClient.Disconnect(true);
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.EmailLogOut,
                    OperationStatus.Failure,
                    "Failed to disconnect from the IMAP server.",
                    exception,
                    logInfos);

                throw;
            }

            imapClient.Dispose();

            logger.Info(
                MyOperation.EmailLogOut,
                OperationStatus.Success,
                "Logged out of the IMAP server.",
                logInfos);
        }

        public string GetHouseholdConfirmationUrl()
        {
            IEnumerable<MimeMessage> emails = RetrieveRecentEmails();

            foreach (MimeMessage email in emails)
            {
                if (email.Subject.Contains(HouseholdUpdateEmailSubject))
                {
                    DateTime emailDateTime = DateTime.Parse(
                        email.Headers["DateReceived"],
                        CultureInfo.InvariantCulture);

                    if (emailDateTime > lastConfirmationEmailDateTime)
                    {
                        lastConfirmationEmailDateTime = emailDateTime;
                        return ExtractConfirmationUrlFromEmail(email);
                    }
                }
            }

            return null;
        }

        private static string ExtractConfirmationUrlFromEmail(MimeMessage email)
            => Regex.Replace(
                email.HtmlBody.Replace(Environment.NewLine, string.Empty),
                ConfirmationUrlPattern,
                ConfirmationUrlReplacement);

        private IEnumerable<MimeMessage> RetrieveRecentEmails()
        {
            IMailFolder inbox = imapClient.Inbox;
            inbox.Open(FolderAccess.ReadOnly);

            IList<MimeMessage> emails = [];

            for (int emailIndex = inbox.Count - 1; emailIndex >= 0; emailIndex -= 1)
            {
                MimeMessage email = inbox.GetMessage(emailIndex);

                if ((DateTime.Now - email.Date).TotalSeconds > imapSettings.MaxEmailAge)
                {
                    break;
                }

                emails.Add(email);
            }

            return emails;
        }
    }
}
