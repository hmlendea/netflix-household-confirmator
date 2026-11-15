using System;
using System.IO;
using System.Net.Sockets;

using MailKit;

namespace NetflixHouseholdConfirmator.Service.Processors
{
    public static class ImapFailurePolicy
    {
        public static bool IsRecoverable(Exception exception)
            => exception is IOException or
                SocketException or
                TimeoutException or
                ProtocolException or
                ServiceNotConnectedException or
                ServiceNotAuthenticatedException;
    }
}
