using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Core.Helpers
{
   
public static class PasswordGenerator
    {
        public static string GenerateStrongPassword(int length)
        {
            if (length < 4)
                throw new ArgumentException("Password length must be at least 4.");

            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // removed O, I
            const string lower = "abcdefghijkmnopqrstuvwxyz"; // removed l
            const string digits = "123456789"; // removed 0
            const string symbols = "!@#$%^&*()-_=+<>?";

            string allChars = upper + lower + digits + symbols;

            char[] password = new char[length];

            using var rng = RandomNumberGenerator.Create();

            // Ensure at least one from each category
            password[0] = GetRandomChar(upper, rng);
            password[1] = GetRandomChar(lower, rng);
            password[2] = GetRandomChar(digits, rng);
            password[3] = GetRandomChar(symbols, rng);

            // Fill remaining characters
            for (int i = 4; i < length; i++)
            {
                password[i] = GetRandomChar(allChars, rng);
            }

            // Shuffle password to avoid predictable positions
            return new string(password.OrderBy(_ => GetRandomInt(rng)).ToArray());
        }

        private static char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            byte[] buffer = new byte[1];
            rng.GetBytes(buffer);
            return chars[buffer[0] % chars.Length];
        }

        private static int GetRandomInt(RandomNumberGenerator rng)
        {
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }
    }

}
