// src/Services/CommonService/PasswordProvider.cs
using System.Security.Cryptography;
using System.Text;

namespace NexgenCosysReport.Services.CommonService;

/// <summary>
/// Exact port of legacy CoSys.Common.PasswordProvider / PasswordUtility.
/// Stored format: "{MD5-hash-as-raw-ASCII-string}:{salt}" where salt is a signed Int32.
/// IMPORTANT: the hash portion is NOT Base64 — it's ASCIIEncoding.GetString() applied
/// directly to raw MD5 hash bytes, so bytes >= 0x80 render as '?'. This is intentional
/// legacy behavior, not a bug — must be reproduced exactly for existing hashes to match.
/// </summary>
public class PasswordProvider
{
    public int CreateRandomSalt()
    {
        var saltBytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return (saltBytes[0] << 24) + (saltBytes[1] << 16) + (saltBytes[2] << 8) + saltBytes[3];
    }

    public string CreateRandomPassword(int passwordLength)
    {
        const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ23456789!@#$%&";
        var randomBytes = new byte[passwordLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var chars = new char[passwordLength];
        var allowedCharCount = allowedChars.Length;
        for (var i = 0; i < passwordLength; i++)
        {
            chars[i] = allowedChars[randomBytes[i] % allowedCharCount];
        }
        return new string(chars);
    }

    public string GetCipheredValue(string plain)
    {
        var ingredient = plain.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
        var pwdUtils = new PasswordUtility(ingredient[0], Convert.ToInt32(ingredient[1]));
        return pwdUtils.ComputeSaltedHash();
    }
}

public class PasswordUtility
{
    private readonly string _password;
    private readonly int _salt;

    public PasswordUtility(string password, int salt)
    {
        _password = password;
        _salt = salt;
    }

    public string ComputeSaltedHash()
    {
        var encoder = Encoding.ASCII;
        var secretBytes = encoder.GetBytes(_password);

        var saltBytes = new byte[4];
        saltBytes[0] = (byte)(_salt >> 24);
        saltBytes[1] = (byte)(_salt >> 16);
        saltBytes[2] = (byte)(_salt >> 8);
        saltBytes[3] = (byte)_salt;

        var toHash = new byte[secretBytes.Length + saltBytes.Length];
        Array.Copy(secretBytes, 0, toHash, 0, secretBytes.Length);
        Array.Copy(saltBytes, 0, toHash, secretBytes.Length, saltBytes.Length);

        using var md5 = MD5.Create();
        var computedHash = md5.ComputeHash(toHash);

        return encoder.GetString(computedHash) + ":" + _salt;
    }
}