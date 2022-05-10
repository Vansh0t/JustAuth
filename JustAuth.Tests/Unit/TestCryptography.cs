using JustAuth.Utils;
using Xunit;
namespace JustAuth.Tests.Unit
{
    public class TestCryptography
    {
        [Fact]
        public void TestHashPasswordSuccess() {
            var password = "somepassword";
            var hash = Cryptography.HashPassword(password);
            Assert.True(Cryptography.ValidatePasswordHash(hash, password));
        }
        [Fact]
        public void TestHashPasswordFail() {
            var password = "somepassword";
            var hash = Cryptography.HashPassword(password);
            Assert.False(Cryptography.ValidatePasswordHash(hash, "omepassword"));
        }
    }
}