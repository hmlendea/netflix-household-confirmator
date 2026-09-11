namespace NetflixHouseholdConfirmator.Service.Processors
{
    public interface INetflixProcessor
    {
        void ConfirmHousehold(string confirmationUrl);
    }
}
