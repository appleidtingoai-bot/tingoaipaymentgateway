using System.Security.Cryptography;
using System.Text;

namespace TingoAI.PaymentGateway.Infrastructure.Security;

public class WebhookDecryptionService
{
    public static string DecryptString(string cipherText, string keyString)
    {
        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.Key = Encoding.UTF8.GetBytes(keyString);

                // Extract IV from the beginning of the cipher text
                byte[] iv = new byte[aesAlg.IV.Length];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                string result;

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(aesAlg.Key, iv), CryptoStreamMode.Write))
                    {
                        // Skip the IV when decrypting
                        csDecrypt.Write(fullCipher, aesAlg.IV.Length, fullCipher.Length - aesAlg.IV.Length);
                    }

                    result = Encoding.UTF8.GetString(msDecrypt.ToArray());
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            return string.IsNullOrWhiteSpace(ex.Message) ? ex.InnerException?.Message ?? ex.Message : ex.Message;
        }
    }
}
