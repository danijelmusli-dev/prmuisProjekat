using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using MrezeProjekat;
using MrezeProjekat.Models;

namespace MrezeProjekat.Helpers
{
    public class CryptoHelper
    {
        public static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            #region CheckParameters
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("Plain text cannot be null or empty");
            if (Key == null || (Key.Length != 16 && Key.Length != 24 && Key.Length != 32))
                throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(Key));
            if (IV == null || IV.Length != 16)
                throw new ArgumentException("IV must be 128 bits (16 bytes).", nameof(IV));
            #endregion

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            #region CheckParameters
            if (cipherText == null || cipherText.Length == 0)
                throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));
            if (Key == null || (Key.Length != 16 && Key.Length != 24 && Key.Length != 32))
                throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(Key));
            if (IV == null || IV.Length != 16)
                throw new ArgumentException("IV must be 128 bits (16 bytes).", nameof(IV));
            #endregion

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (MemoryStream ms = new MemoryStream(cipherText))
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static Message CryptNTimes(string original, int times, Instructions instructions)
        {
            Message message = new Message(original);
            for (int i = 0; i < times; i++)
            {
                message.Content = Convert.ToBase64String(CryptoHelper.EncryptStringToBytes(message.Content, instructions[i].Key, instructions[i].IV));
                Console.WriteLine($"{i+1} Layer of encryption: {message.Content}");
            }
            return message;
        }

    }
}
