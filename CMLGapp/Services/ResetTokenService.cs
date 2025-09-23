using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMLGapp.Services
{
    public static class ResetTokenService
    {
        private static Dictionary<string, (string Code, DateTime Expiry)> emailToToken = new();

        public static string GenerateCode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email), "Email cannot be null or empty.");

            var code = new Random().Next(100000, 999999).ToString();
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>Code generated for {email}>>>>>>>>>>>>:<<<<<<< {code}<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            // 10-minute expiry
            emailToToken[email] = (code, DateTime.UtcNow.AddMinutes(10));
            return code;
        }


        public static bool VerifyCode(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return false;

            if (emailToToken.TryGetValue(email, out var tokenData))
            {
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>Code generated for {email}>>>>>>>>>>>>:<<<<<<< {code}<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
                return tokenData.Code == code && DateTime.UtcNow <= tokenData.Expiry;
            }
            return false;
        }

        public static void RemoveCode(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;

            if (emailToToken.ContainsKey(email))
                emailToToken.Remove(email);
        }

    }
}
