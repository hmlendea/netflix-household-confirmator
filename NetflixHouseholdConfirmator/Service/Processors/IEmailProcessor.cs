namespace NetflixHouseholdConfirmator.Service.Processors
{
    public interface IEmailProcessor
    {
        void LogIn();

        void LogOut();

        string GetHouseholdConfirmationUrl();
    }
}
