namespace NetflixHouseholdConfirmator.Configuration
{
    public sealed class DebugSettings
    {
        public string CrashScreenshotFileName { get; set; }

        public bool IsDebugMode { get; set; }

        public bool IsHeadless => !IsDebugMode;

        public bool IsCrashScreenshotEnabled => !string.IsNullOrWhiteSpace(CrashScreenshotFileName);
    }
}
