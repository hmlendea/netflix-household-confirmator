using System;
using System.Collections.Generic;
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
        ILogger logger) : IEmailProcessor
    {
        readonly ImapSettings imapSettings = imapSettings;
        readonly ILogger logger = logger;
        readonly ImapClient imapClient = new();

        DateTime lastConfirmationEmailDateTime = DateTime.Now;

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

        public string GetHouseholdConfirmationUrl()
        {
            IEnumerable<MimeMessage> emails = RetrieveRecentEmails();

            foreach (MimeMessage email in emails)
            {
                if (email.Subject.Contains("How to update your Netflix Household"))
                {
                    DateTime emailDateTime = DateTime.Parse(email.Headers["DateReceived"]);

                    if (emailDateTime > lastConfirmationEmailDateTime)
                    {
                        lastConfirmationEmailDateTime = emailDateTime;
                        return ExtractConfirmationUrlFromEmail(email);
                    }
                }
            }

            return null;
        }

        private string ExtractConfirmationUrlFromEmail(MimeMessage email)
        => Regex.Replace(
                email.HtmlBody.Replace(Environment.NewLine, string.Empty),
                ".*(https:\\/\\/[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*",
                "$1");

        private IEnumerable<MimeMessage> RetrieveRecentEmails()
        {
            var inbox = imapClient.Inbox;
            inbox.Open(FolderAccess.ReadOnly);

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

            return emails;
        }
    }
}
