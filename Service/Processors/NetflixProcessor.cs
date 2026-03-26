using NuciWeb;
using NuciWeb.Automation;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public sealed class NetflixProcessor(IWebProcessor webProcessor) : INetflixProcessor
    {
        public void ConfirmHousehold(string confirmationUrl)
        {
            webProcessor.GoToUrl(confirmationUrl);

            string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");
            string locationDetailsSelector = Select.ByXPath(@"//div[@data-uia='location-details']");

            webProcessor.WaitForAnyElementToBeVisible(confirmButtonSelector, locationDetailsSelector);

            if (webProcessor.IsElementVisible(locationDetailsSelector))
            {
                return;
            }

            webProcessor.Click(confirmButtonSelector);
        }
    }
}