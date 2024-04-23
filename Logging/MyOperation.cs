using NuciLog.Core;

namespace NetflixHouseholdConfirmator.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {

        }

        public static Operation LogIn => new MyOperation(nameof(LogIn));

        public static Operation LogOut => new MyOperation(nameof(LogOut));

        public static Operation RetrieveRecentEmails => new MyOperation(nameof(RetrieveRecentEmails));

        public static Operation CheckForPendingConfirmations => new MyOperation(nameof(CheckForPendingConfirmations));
    }
}