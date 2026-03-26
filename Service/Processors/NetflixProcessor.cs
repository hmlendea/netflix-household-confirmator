using NuciWeb;
using NuciWeb.Automation;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public sealed class NetflixProcessor : INetflixProcessor
    {
        readonly IWebProcessor webProcessor;

        public NetflixProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public void ConfirmHousehold(string confirmationUrl)
        {
            webProcessor.GoToUrl(confirmationUrl);

            string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");

            webProcessor.Click(confirmButtonSelector);
        }
    }
}