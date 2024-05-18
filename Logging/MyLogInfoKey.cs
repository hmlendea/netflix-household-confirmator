using NuciLog.Core;

namespace NetflixHouseholdConfirmator.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name)
            : base(name)
        {

        }

        public static LogInfoKey Server => new MyLogInfoKey(nameof(Server));

        public static LogInfoKey Port => new MyLogInfoKey(nameof(Port));

        public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));

        public static LogInfoKey Password => new MyLogInfoKey(nameof(Password));

        public static LogInfoKey MaxAge => new MyLogInfoKey(nameof(MaxAge));

        public static LogInfoKey EmailsCount => new MyLogInfoKey(nameof(EmailsCount));
    }
}