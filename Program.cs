using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NetflixHouseholdConfirmator.Configuration;
using NetflixHouseholdConfirmator.Service;
using NetflixHouseholdConfirmator.Service.Processors;

using OpenQA.Selenium;
using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;
using NuciWeb;

namespace NetflixHouseholdConfirmator
{
    public sealed class Program
    {
        static BotSettings botSettings;
        static DebugSettings debugSettings;
        static ImapSettings imapSettings;
        static NuciLoggerSettings loggerSettings;

        static IWebDriver webDriver;
        static ILogger logger;

        static IServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            LoadConfiguration();
            webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode, botSettings.PageLoadTimeout);

            serviceProvider = CreateIOC();
            logger = serviceProvider.GetService<ILogger>();

            logger.Info(Operation.StartUp, "Application started");

            try
            {
                RunApplication();
            }
            catch (AggregateException ex)
            {
                LogInnerExceptions(ex);
                SaveCrashScreenshot();
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
                SaveCrashScreenshot();
            }
            finally
            {
                webDriver?.Quit();

                logger.Info(Operation.ShutDown, "Application stopped");
            }
        }

        static void RunApplication()
        {
            IEmailConfirmator email = serviceProvider.GetService<IEmailConfirmator>();
            INetflixProcessor netflix = serviceProvider.GetService<INetflixProcessor>();

            email.LogIn();

            while(true)
            {
                string confirmationUrl = email.GetHouseholdConfirmationUrl();

                if (confirmationUrl is not null)
                {
                    netflix.ConfirmHousehold(confirmationUrl);
                }
            }

            email.LogOut();
        }

        static IConfiguration LoadConfiguration()
        {
            botSettings = new BotSettings();
            debugSettings = new DebugSettings();
            imapSettings = new ImapSettings();
            loggerSettings = new NuciLoggerSettings();

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            config.Bind(nameof(BotSettings), botSettings);
            config.Bind(nameof(DebugSettings), debugSettings);
            config.Bind(nameof(ImapSettings), imapSettings);
            config.Bind(nameof(NuciLoggerSettings), loggerSettings);

            return config;
        }

        static IServiceProvider CreateIOC()
        {
            return new ServiceCollection()
                .AddSingleton(botSettings)
                .AddSingleton(debugSettings)
                .AddSingleton(imapSettings)
                .AddSingleton(loggerSettings)
                .AddSingleton<IEmailConfirmator, EmailConfirmator>()
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton<IWebDriver>(s => webDriver)
                .AddSingleton<IWebProcessor, WebProcessor>()
                .AddSingleton<INetflixProcessor, NetflixProcessor>()
                .BuildServiceProvider();
        }

        static void LogInnerExceptions(AggregateException exception)
        {
            foreach (Exception innerException in exception.InnerExceptions)
            {
                AggregateException innerAggregateException = innerException as AggregateException;

                if (innerAggregateException is null)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
                else
                {
                    LogInnerExceptions(innerException as AggregateException);
                }
            }
        }

        static void SaveCrashScreenshot()
        {
            if (!debugSettings.IsCrashScreenshotEnabled)
            {
                return;
            }

            string directory = Path.GetDirectoryName(loggerSettings.LogFilePath);
            string filePath = Path.Combine(directory, debugSettings.CrashScreenshotFileName);

            ((ITakesScreenshot)webDriver)
                .GetScreenshot()
                .SaveAsFile(filePath);
        }
    }
}
