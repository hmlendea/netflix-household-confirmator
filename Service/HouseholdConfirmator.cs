using System;
using NetflixHouseholdConfirmator.Logging;
using NetflixHouseholdConfirmator.Service.Processors;
using NuciLog.Core;

namespace NetflixHouseholdConfirmator.Service
{
    public class HouseholdConfirmator(
        IEmailProcessor emailProcessor,
        INetflixProcessor netflixProcessor,
        ILogger logger)
        : IHouseholdConfirmator
    {
        public void ConfirmIncomingHouseholdUpdateRequests()
        {
            emailProcessor.LogIn();

            logger.Info(
                MyOperation.ListenForConfirmationRequests,
                OperationStatus.Started,
                "Listening for incoming household update requests.");

            try
            {
                while(true)
                {
                    string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

                    if (confirmationUrl is not null)
                    {
                        netflixProcessor.ConfirmHousehold(confirmationUrl);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.ListenForConfirmationRequests,
                    OperationStatus.Failure,
                    exception);

                throw;
            }
            finally
            {
                emailProcessor.LogOut();
            }
        }
    }
}
