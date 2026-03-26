using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using NuciLog.Core;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Logging;
using System.Linq;

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
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Server, imapSettings.Server),
                new(MyLogInfoKey.Port, imapSettings.Port)
            ];

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.Started,
                "Connecting to the IMAP server.",
                logInfos);

            try
            {
                imapClient.Connect(imapSettings.Server, imapSettings.Port, true);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.LogIn,
                    OperationStatus.Failure,
                    "Failed to connect to the IMAP server.",
                    ex,
                    logInfos);

                throw;
            }

            logInfos = logInfos.Append(new(MyLogInfoKey.Username, imapSettings.Username));

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.InProgress,
                "Authenticating on the IMAP server.",
                logInfos);

            try
            {
                imapClient.Authenticate(imapSettings.Username, imapSettings.Password);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.LogIn,
                    OperationStatus.Failure,
                    "Failed to authenticate on the IMAP server.",
                    ex,
                    logInfos);

                throw;
            }

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.Success,
                "Logged into the IMAP server.",
                logInfos);
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
