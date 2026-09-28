namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ICryptoService
    {
        string Encrypt(string plainText, bool useHashing);
        string Decrypt(string cipherText, bool useHashing);
        string GetSha256Hash(string input);
    }
}
