using Oblak.Services.MNE;
using System.Security.Cryptography;
using System.Text;

namespace Oblak.Services
{
    public class SelfRegisterService
    {
        private const string _alg = "HmacSHA256";
        private const string _salt = "BQZeURs6kC2j1LK8n9Vf"; // Generated at https://www.random.org/strings
        private const string _password = "uUSUFjn7srCmTopr5GOd";
        private const int _expirationMinutes = 200;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SelfRegisterService> _logger;


        public SelfRegisterService(IConfiguration configuration, ILogger<SelfRegisterService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }


        public string GenerateToken(string guid, string objekat, string jedinica, string lang, string ticks)
        {
            string hash = string.Join(":", new string[] { guid, objekat, jedinica, lang, ticks });
            string hashLeft = "";
            string hashRight = "";

            using (HMAC hmac = HMAC.Create(_alg))
            {
                hmac.Key = Encoding.UTF8.GetBytes(GetHashedPassword());
                hmac.ComputeHash(Encoding.UTF8.GetBytes(hash));

                hashLeft = Convert.ToBase64String(hmac.Hash);
                hashRight = string.Join(":", new string[] { guid, objekat, jedinica, lang, ticks });
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(":", hashLeft, hashRight)));
        }


        public string GetHashedPassword()
        {
            string key = string.Join(":", new string[] { _password, _salt });

            using (HMAC hmac = HMAC.Create(_alg))
            {
                // Hash the key.
                hmac.Key = Encoding.UTF8.GetBytes(_salt);
                hmac.ComputeHash(Encoding.UTF8.GetBytes(key));

                return Convert.ToBase64String(hmac.Hash);
            }
        }

        public (int, int, string, string, bool) IsTokenValid(string token)
        {
            bool result = false;
            string objekat = null;
            string jedinica = null;
            string lang = null;
            string guid = null;
            bool expired = false;

            try
            {
                // Base64 decode the string, obtaining the token:username:timeStamp.
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                // Split the parts.
                string[] parts = key.Split(new char[] { ':' });
                if (parts.Length == 6)
                {
                    // Get the hash message, username, and timestamp.
                    string hash = parts[0];
                    guid = parts[1];
                    objekat = parts[2];
                    jedinica = parts[3];
                    lang = parts[4];
                    long ticks = long.Parse(parts[5]);
                    DateTime timeStamp = new DateTime(ticks);

                    // Ensure the timestamp is valid.
                    expired = Math.Abs((DateTime.UtcNow - timeStamp).TotalMinutes) > _expirationMinutes;
                    if (!expired)
                    {
                        // Hash the message with the key to generate a token.
                        string computedToken = GenerateToken(guid, objekat, jedinica, lang, ticks.ToString());

                        // Compare the computed token with the one supplied and ensure they match.
                        result = token == computedToken;
                    }
                }
            }
            catch
            {
            }

            if (result) return (int.Parse(objekat), int.Parse(jedinica), lang, guid, expired); else return (0, 0, "ENG", "0", expired);
        }
    }
}