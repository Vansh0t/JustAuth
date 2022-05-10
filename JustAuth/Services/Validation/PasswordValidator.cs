using System.Text.RegularExpressions;

namespace JustAuth.Services.Validation
{
    public class PasswordValidator:IPasswordValidator
    {
        const int MIN_LENGTH = 6;
        const int MAX_LENGTH = 32;
        const string DIGIT_REGEX = @"\d";
        const string LETTER_REGEX = @"[A-Za-z]";
        public IServiceResult Validate(string password) {
            List<string> errors = new();
            if(password is null) {
                return ServiceResult.Fail(400, "Invalid password.");
            } 
            if(password.Length < MIN_LENGTH) {
                errors.Add($"Minimum password length should be {MIN_LENGTH} characters.");
            } 
            else if(password.Length>MAX_LENGTH) {
                errors.Add($"Maximum password length should be {MAX_LENGTH} characters.");
            }
            if(!Regex.IsMatch(password, DIGIT_REGEX)) {
                errors.Add("Password should contain at least 1 digit.");
            }
            if(!Regex.IsMatch(password, LETTER_REGEX)) {
                errors.Add("Password should contain at least 1 letter.");
            }
            if(errors.Count == 0) {
                return ServiceResult.Success();
            }
            else {
                return ServiceResult.Fail(400, string.Join(' ', errors));
            }
        }       
    }
}