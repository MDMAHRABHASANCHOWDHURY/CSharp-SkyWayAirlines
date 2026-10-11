using System;
using System.Security.Cryptography;

namespace AirlineReservationDemo.Services
{
    /// <summary>PBKDF2-SHA256 password hashing. Stored as PBKDF2$iterations$salt$hash.</summary>
    internal static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] hash = Derive(password, salt, Iterations);
            return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored) || password == null)
            return false;

            string[] parts = stored.Split('$');
            int iterations;
            if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out iterations))
            {
                return false;
            }

            if (iterations < 10000 || iterations > 1000000)
            return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                if (salt.Length != SaltSize || expected.Length != HashSize)
                return false;
                byte[] actual = Derive(password, salt, iterations);

                // constant-time comparison
                int diff = expected.Length ^ actual.Length;
                for (int i = 0; i < expected.Length && i < actual.Length; i++)
                {
                    diff |= expected[i] ^ actual[i];
                }
                return diff == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (Rfc2898DeriveBytes kdf =
            new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(HashSize);
            }
        }
    }
}
