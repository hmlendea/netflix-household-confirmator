using System;
using System.Collections.Generic;

using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using NuciLog.Core;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Logging;

namespace NetflixHouseholdConfirmator.Service
{
    public sealed class EmailConfirmator : IEmailConfirmator
    {
        readonly ImapSettings imapSettings;
        readonly ILogger logger;
        readonly ImapClient imapClient;

        public EmailConfirmator(
            ImapSettings imapSettings,
            ILogger logger)
        {
            this.imapSettings = imapSettings;
            imapClient = new ImapClient();

            this.logger = logger;
        }

        public void LogIn()
        {
            logger.Debug(
                MyOperation.LogIn,
                OperationStatus.Started,
                "Connecting to the IMAP server",
                new LogInfo(MyLogInfoKey.Server, imapSettings.Server),
                new LogInfo(MyLogInfoKey.Port, imapSettings.Port));

            imapClient.Connect(imapSettings.Server, imapSettings.Port, true);

            logger.Debug(
                MyOperation.LogIn,
                OperationStatus.InProgress,
                "Authenticating on the IMAP server",
                new LogInfo(MyLogInfoKey.Username, imapSettings.Username));

            imapClient.Authenticate(imapSettings.Username, imapSettings.Password);

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.Success,
                "Logged into the IMAP server",
                new LogInfo(MyLogInfoKey.Server, imapSettings.Server),
                new LogInfo(MyLogInfoKey.Port, imapSettings.Port),
                new LogInfo(MyLogInfoKey.Username, imapSettings.Username));
        }

        public void LogOut()
        {
            logger.Debug(
                MyOperation.LogOut,
                OperationStatus.Started,
                "Disconnecting from the IMAP server",
                new LogInfo(MyLogInfoKey.Server, imapSettings.Server),
                new LogInfo(MyLogInfoKey.Port, imapSettings.Port));

            imapClient.Disconnect(true);
            imapClient.Dispose();

            logger.Info(
                MyOperation.LogOut,
                OperationStatus.Success,
                "Logged out of the IMAP server",
                new LogInfo(MyLogInfoKey.Server, imapSettings.Server),
                new LogInfo(MyLogInfoKey.Port, imapSettings.Port),
                new LogInfo(MyLogInfoKey.Username, imapSettings.Username));
        }

        public bool HasPendingConfirmations()
        {
            IEnumerable<MimeMessage> emails = RetrieveRecentEmails();

            foreach (MimeMessage email in emails)
            {
                if (email.Subject.Contains("How to update your Netflix Household"))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<MimeMessage> RetrieveRecentEmails()
        {
            logger.Debug(
                MyOperation.RetrieveRecentEmails,
                OperationStatus.Started,
                "Retrieving the recent emails",
                new LogInfo(MyLogInfoKey.MaxAge, imapSettings.MaxEmailAge));

            var inbox = imapClient.Inbox;
            inbox.Open(FolderAccess.ReadOnly);

            logger.Debug(
                MyOperation.RetrieveRecentEmails,
                OperationStatus.InProgress,
                "Filtering recent emails from the inbox",
                new LogInfo(MyLogInfoKey.EmailsCount, inbox.Count),
                new LogInfo(MyLogInfoKey.MaxAge, imapSettings.MaxEmailAge));

            IList<MimeMessage> emails = [];

            for(int i = inbox.Count - 1; i >= 0; i--)
            {
                var email = inbox.GetMessage(i);

                if ((DateTime.Now - email.Date).TotalSeconds > imapSettings.MaxEmailAge)
                {
                    break;
                }

                emails.Add(email);
            }

            logger.Info(
                MyOperation.RetrieveRecentEmails,
                OperationStatus.Success,
                "Retrieved the recent emails",
                new LogInfo(MyLogInfoKey.EmailsCount, emails.Count));

            return emails;
        }
    }
}
