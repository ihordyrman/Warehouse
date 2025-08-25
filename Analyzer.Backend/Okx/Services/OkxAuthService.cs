using System.Security.Cryptography;
using System.Text;
using Analyzer.Backend.Okx.Configurations;

namespace Analyzer.Backend.Okx.Services;

public class OkxAuthService
{
    public static object CreateAuthRequest(OkxAuthConfiguration config)
    {
        string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString();
        string sign = GenerateSignature(timestamp, config.SecretKey);

        return new
        {
            op = "login",
            args = new[]
            {
                new
                {
                    apiKey = config.ApiKey,
                    passphrase = config.Passphrase,
                    timestamp,
                    sign
                }
            }
        };
    }

    public static string GenerateSignature(string timestamp, string secretKey, string method = "GET", string path = "/users/self/verify")
    {
        byte[] sign = Encoding.UTF8.GetBytes($"{timestamp}{method}{path}");
        byte[] key = Encoding.UTF8.GetBytes(secretKey);

        using var hmac = new HMACSHA256(key);
        byte[] hash = hmac.ComputeHash(sign);
        return Convert.ToBase64String(hash);
    }
}
