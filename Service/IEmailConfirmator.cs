namespace NetflixHouseholdConfirmator.Service
{
    public interface IEmailConfirmator
    {
        void LogIn();

        void LogOut();

        string GetHouseholdConfirmationUrl();
    }
}
