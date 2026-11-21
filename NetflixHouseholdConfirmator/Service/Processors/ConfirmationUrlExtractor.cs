using System;
using System.Net;
using System.Text.RegularExpressions;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public static class ConfirmationUrlExtractor
    {
        private static string ConfirmationUrlPattern
            => ".*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*";

        private static string ConfirmationUrlReplacement => "$1";

        public static string Extract(string htmlBody)
        {
            string normalisedBody = htmlBody.Replace(Environment.NewLine, string.Empty);
            string extractedUrl = Regex.Replace(
                normalisedBody,
                ConfirmationUrlPattern,
                ConfirmationUrlReplacement);

            if (string.Equals(extractedUrl, normalisedBody, StringComparison.Ordinal))
            {
                return normalisedBody;
            }

            return WebUtility.HtmlDecode(extractedUrl);
        }
    }
}
