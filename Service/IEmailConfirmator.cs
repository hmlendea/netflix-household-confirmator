namespace NetflixHouseholdConfirmator.Service
{
    public interface IEmailConfirmator
    {
        void LogIn();

        void LogOut();

        bool HasPendingConfirmations();
    }
}
