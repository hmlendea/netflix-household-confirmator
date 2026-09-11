using NUnit.Framework;

using NetflixHouseholdConfirmator.Logging;

namespace NetflixHouseholdConfirmator.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyLogInfoKeyTests
    {
        [Test]
        public void GivenTheServerKey_WhenRetrievingIt_ThenItsNameIsServer()
            => Assert.That(
                MyLogInfoKey.Server.Name,
                Is.EqualTo(nameof(MyLogInfoKey.Server)));

        [Test]
        public void GivenThePortKey_WhenRetrievingIt_ThenItsNameIsPort()
            => Assert.That(
                MyLogInfoKey.Port.Name,
                Is.EqualTo(nameof(MyLogInfoKey.Port)));

        [Test]
        public void GivenTheUsernameKey_WhenRetrievingIt_ThenItsNameIsUsername()
            => Assert.That(
                MyLogInfoKey.Username.Name,
                Is.EqualTo(nameof(MyLogInfoKey.Username)));

        [Test]
        public void GivenThePasswordKey_WhenRetrievingIt_ThenItsNameIsPassword()
            => Assert.That(
                MyLogInfoKey.Password.Name,
                Is.EqualTo(nameof(MyLogInfoKey.Password)));

        [Test]
        public void GivenTheMaximumAgeKey_WhenRetrievingIt_ThenItsNameIsMaxAge()
            => Assert.That(
                MyLogInfoKey.MaxAge.Name,
                Is.EqualTo(nameof(MyLogInfoKey.MaxAge)));
    }
}