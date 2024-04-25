namespace NetflixHouseholdConfirmator.Service
{
    public interface IEmailConfirmator
    {
        void LogIn();

        void LogOut();

        void ConfirmHousehold();
    }
}
