using JustAuth.Services;
using JustAuth.Services.Validation;
using Xunit;
namespace JustAuth.Tests.Unit
{
    public class ValidatorsTest
    {
        private readonly IEmailValidator _emailValidator;

        private readonly IPasswordValidator _passwordValidator;

        private readonly IUsernameValidator _uNameValidator;

        public ValidatorsTest() {
            _emailValidator = new EmailValidator();
            _passwordValidator = new PasswordValidator();
            _uNameValidator = new UsernameValidator();
        }
        [Theory]
        [InlineData("wrong")]
        [InlineData("25@**")]
        [InlineData(null)]
        public void TestEmailFail(string email)
        {
            IServiceResult result = _emailValidator.Validate(email);
            Assert.True(result.IsError);
        }
        [Fact]
        public void TestEmailSuccess()
        {
            IServiceResult result = _emailValidator.Validate("test@email.com");
            Assert.False(result.IsError);
        }
        [Theory]
        //too short
        [InlineData("short")]
        //too long
        [InlineData("longgggggggggggggggggggggggggggggggggg")]
        //has no digits
        [InlineData("hasnodigits")]
        //has no letters
        [InlineData("123456789")]
        //null
        [InlineData(null)]
        public void TestPasswordFail(string password)
        {
            IServiceResult result = _passwordValidator.Validate(password);
            Assert.True(result.IsError);
        }
        [Fact]
        public void TestPasswordSuccess()
        {
            IServiceResult result = _passwordValidator.Validate("validpwd111");
            Assert.False(result.IsError);
        }
        [Theory]
        //no letters
        [InlineData("1")]
        //too short
        [InlineData("s")]
        //too long
        [InlineData("longgggggggggggggggggggggggggggggggggg")]
        //null
        [InlineData(null)]
        //has invalid characters
        [InlineData("**AA2!")]
        public void TestUsernameFail(string username)
        {
            IServiceResult result = _uNameValidator.Validate(username);
            Assert.True(result.IsError);
        }
        [Fact]
        public void TestUsernameSuccess()
        {
            IServiceResult result = _uNameValidator.Validate("V4");
            Assert.False(result.IsError);
        }
    }
}