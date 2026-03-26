using System;
using NetflixHouseholdConfirmator.Logging;
using NuciLog.Core;
using NuciWeb;
using NuciWeb.Automation;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public sealed class NetflixProcessor(
        IWebProcessor webProcessor,
        ILogger logger) : INetflixProcessor
    {
        public void ConfirmHousehold(string confirmationUrl)
        {
            logger.Info(
                MyOperation.HouseholdConfirmation,
                OperationStatus.Started,
                "Starting the household confirmation process.");

            try
            {
                webProcessor.GoToUrl(confirmationUrl);

                string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");
                string locationDetailsSelector = Select.ByXPath(@"//div[@data-uia='location-details']");

                webProcessor.WaitForAnyElementToBeVisible(confirmButtonSelector, locationDetailsSelector);

                if (!webProcessor.IsElementVisible(locationDetailsSelector))
                {
                    webProcessor.Click(confirmButtonSelector);
                    webProcessor.Wait(5000);
                }
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.HouseholdConfirmation,
                    OperationStatus.Failure,
                    "An error has occurred while confirming the household.",
                    exception);

            logger.Info(
                MyOperation.HouseholdConfirmation,
                OperationStatus.Success,
                "The household was successfully confirmed.");
        }
    }
}