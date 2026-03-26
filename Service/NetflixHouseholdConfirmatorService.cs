using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.Service
{
    public class NetflixHouseholdConfirmatorService(
        IEmailProcessor emailProcessor,
        INetflixProcessor netflixProcessor)
        : INetflixHouseholdConfirmatorService
    {
        public string ConfirmIncomingHouseholdUpdateRequests()
        {
            emailProcessor.LogIn();

            while(true)
            {
                string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

                if (confirmationUrl is not null)
                {
                    netflixProcessor.ConfirmHousehold(confirmationUrl);
                }
            }
        }
    }
}
