using System.Text.RegularExpressions;

namespace JustAuth.Services.Validation
{
    public class UsernameValidator:IUsernameValidator
    {
        const int MIN_LENGTH = 2;

        const int MAX_LENGTH = 24;
        const string REGEX = @"[^A-Za-z\d]";
        const string LETTER_REGEX = @"[A-Za-z]";
        public IServiceResult Validate(string username) {
            if(username is null || username.Length<MIN_LENGTH || username.Length>MAX_LENGTH || Regex.IsMatch(username, REGEX)) {
                return ServiceResult.Fail(400, $"Username must contain {MIN_LENGTH}-{MAX_LENGTH} characters and consist only of letters and digits.");
            }
            if(!Regex.IsMatch(username, LETTER_REGEX)) {
                return ServiceResult.Fail(400, "Username should have at least 1 letter.");
            }
            return ServiceResult.Success();
        }
    }
}