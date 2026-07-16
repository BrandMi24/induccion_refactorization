using System;
using System.Security.Cryptography;

namespace induccion_refactorization.Helpers
{
    /// <summary>
    /// PBKDF2 (Rfc2898) password hashing. No external dependency (BCrypt.Net) required.
    /// Stored format: PBKDF2$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 key&gt;
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "PBKDF2$";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            using (var rfc = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                var salt = rfc.Salt;
                var key = rfc.GetBytes(KeySize);
                return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
            }
        }

        public static bool IsHashed(string storedValue)
        {
            return !string.IsNullOrEmpty(storedValue) && storedValue.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public static bool Verify(string password, string storedHash)
        {
            if (!IsHashed(storedHash))
            {
                return false;
            }

            var parts = storedHash.Substring(Prefix.Length).Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            {
                return false;
            }

            byte[] salt, expectedKey;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedKey = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var rfc = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var actualKey = rfc.GetBytes(expectedKey.Length);
                return FixedTimeEquals(actualKey, expectedKey);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
