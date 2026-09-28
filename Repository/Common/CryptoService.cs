using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Security.Cryptography;
using System.Text;

namespace NexgenCosysReport.Services.Common
{
    public class CryptoService : ICryptoService
    {
        // Same key as the legacy CryptorEngine so existing encrypted data still decrypts.
        private const string LegacyKey = "Syed Moshiur Murshed";
        private readonly string _key;

        public CryptoService(IConfiguration configuration)
        {
            _key = configuration["CryptorSettings:Key"] ?? LegacyKey;
        }

        public string Encrypt(string plainText, bool useHashing)
        {
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(plainText);

            using var tdes = CreateTripleDes(useHashing);
            using var transform = tdes.CreateEncryptor();

            byte[] resultArray = transform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string Decrypt(string cipherText, bool useHashing)
        {
            byte[] toDecryptArray = Convert.FromBase64String(cipherText);

            using var tdes = CreateTripleDes(useHashing);
            using var transform = tdes.CreateDecryptor();

            byte[] resultArray = transform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }

        public string GetSha256Hash(string input)
        {
            byte[] result = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            var sb = new StringBuilder(result.Length * 2);
            foreach (byte b in result)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        private TripleDES CreateTripleDes(bool useHashing)
        {
            byte[] keyArray = useHashing
                ? MD5.HashData(Encoding.UTF8.GetBytes(_key))
                : Encoding.UTF8.GetBytes(_key);

            var tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            return tdes;
        }
    }
}