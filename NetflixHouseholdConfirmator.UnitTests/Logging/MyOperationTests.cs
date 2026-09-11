using NUnit.Framework;

using NetflixHouseholdConfirmator.Logging;

namespace NetflixHouseholdConfirmator.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyOperationTests
    {
        [Test]
        public void GivenTheEmailLoginOperation_WhenRetrievingIt_ThenItsNameIsEmailLogIn()
            => Assert.That(
                MyOperation.EmailLogIn.Name,
                Is.EqualTo(nameof(MyOperation.EmailLogIn)));

        [Test]
        public void GivenTheEmailLogoutOperation_WhenRetrievingIt_ThenItsNameIsEmailLogOut()
            => Assert.That(
                MyOperation.EmailLogOut.Name,
                Is.EqualTo(nameof(MyOperation.EmailLogOut)));

        [Test]
        public void GivenTheHouseholdConfirmationOperation_WhenRetrievingIt_ThenItsNameIsHouseholdConfirmation()
            => Assert.That(
                MyOperation.HouseholdConfirmation.Name,
                Is.EqualTo(nameof(MyOperation.HouseholdConfirmation)));

        [Test]
        public void GivenTheListenerOperation_WhenRetrievingIt_ThenItsNameIsListenForConfirmationRequests()
            => Assert.That(
                MyOperation.ListenForConfirmationRequests.Name,
                Is.EqualTo(nameof(MyOperation.ListenForConfirmationRequests)));
    }
}