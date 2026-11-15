using System;
using System.IO;
using System.Net.Sockets;

using MailKit;
using MailKit.Net.Imap;

using NUnit.Framework;

using NetflixHouseholdConfirmator.Service.Processors;

namespace NetflixHouseholdConfirmator.UnitTests.Service.Processors
{
    [TestFixture]
    public sealed class EmailRecoveryPolicyTests
    {
        [Test]
        public void GivenARecoverableConnectionFailure_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new IOException("Connection timed out"));

        [Test]
        public void GivenARecoverableSocketFailure_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new SocketException((int)SocketError.TimedOut));

        [Test]
        public void GivenARecoverableTimeout_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new TimeoutException("The operation has timed out"));

        [Test]
        public void GivenADroppedServerConnection_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new ImapProtocolException("The IMAP server has unexpectedly disconnected."));

        [Test]
        public void GivenAStaleSession_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new ServiceNotConnectedException());

        [Test]
        public void GivenAnExpiredAuthentication_WhenInspectingIt_ThenItIsRecoverable()
            => AssertRecoverable(new ServiceNotAuthenticatedException());

        [Test]
        public void GivenAnUnrelatedFailure_WhenInspectingIt_ThenItIsNotRecoverable()
            => AssertNotRecoverable(new InvalidOperationException("All went well, until it didn't"));

        [Test]
        public void GivenAMalformedEmailValue_WhenInspectingIt_ThenItIsNotRecoverable()
            => AssertNotRecoverable(new FormatException("The date is not a date"));

        private static void AssertRecoverable(Exception exception)
            => Assert.That(
                ImapFailurePolicy.IsRecoverable(exception),
                Is.True);

        private static void AssertNotRecoverable(Exception exception)
            => Assert.That(
                ImapFailurePolicy.IsRecoverable(exception),
                Is.False);
    }
}
