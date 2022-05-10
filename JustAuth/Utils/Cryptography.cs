using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;

namespace JustAuth.Utils
{
    public static class Cryptography
    {
        const int HASH_SALT_LENGTH = 128/8;
        const int HASH_ITERATIONS = 10000;
        const int HASH_KEY_LENGTH = 256/8;
        public static string HashPassword(string password, byte[] salt = null) {
            if (salt is null)
                salt = RandomNumberGenerator.GetBytes(HASH_SALT_LENGTH);
            var outArray = new byte[HASH_SALT_LENGTH+HASH_KEY_LENGTH];
            byte[] hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: HASH_ITERATIONS,
            numBytesRequested: HASH_KEY_LENGTH);
            salt.CopyTo(outArray, 0);
            hashed.CopyTo(outArray, salt.Length);
            return Convert.ToBase64String(outArray);
        }
        public static bool ValidatePasswordHash(string hash, string password) {
            byte[] bHash = Convert.FromBase64String(hash);
            byte[] salt = bHash[..HASH_SALT_LENGTH];
            string doHash = HashPassword(password, salt);
            //byte[] pwdHash = bHash[HASH_SALT_LENGTH..];
            return doHash == hash;
        }
        public static string GetGuidToken() {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
        public static SymmetricSecurityKey GetJwtSigningKey(string seed) {
            byte[] b = Encoding.ASCII.GetBytes(seed);
            return new SymmetricSecurityKey(b);
        }
    }
}