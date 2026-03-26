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
    }
}