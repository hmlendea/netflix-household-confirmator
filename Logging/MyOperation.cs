using NuciLog.Core;

namespace NetflixHouseholdConfirmator.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {

        }

        public static Operation EmailLogIn => new MyOperation(nameof(EmailLogIn));

        public static Operation EmailLogOut => new MyOperation(nameof(EmailLogOut));

        public static Operation HouseholdConfirmation => new MyOperation(nameof(HouseholdConfirmation));

        public static Operation ListenForConfirmationRequests => new MyOperation(nameof(ListenForConfirmationRequests));
    }
}