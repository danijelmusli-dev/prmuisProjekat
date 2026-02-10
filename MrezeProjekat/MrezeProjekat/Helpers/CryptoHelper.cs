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
            if (Key == null || (Key.Length != 16 && Key.Length != 24 && Key.Length != 32))
                throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(Key));
            if (IV == null || IV.Length != 16)
                throw new ArgumentException("IV must be 128 bits (16 bytes).", nameof(IV));
            #endregion

            #region Encryption
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(bytes, 0, bytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
            #endregion
        }

        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            #region CheckParameters
            if (Key == null || (Key.Length != 16 && Key.Length != 24 && Key.Length != 32))
                throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(Key));
            if (IV == null || IV.Length != 16)
                throw new ArgumentException("IV must be 128 bits (16 bytes).", nameof(IV));
            #endregion

            #region Decryption
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
            #endregion
        }

        public static Message CryptNTimes(string original, int times, Instructions instructions, bool clientServer)
        {
            string cryptString = original;

            if(clientServer)
            {
                // Encrypt in reverse order for client->server communication
                // message ( encrypted: key3 -> key2 -> key1 ) 
                for (int i = times - 1; i >= 0; i--)
                {
                    cryptString = Convert.ToBase64String(CryptoHelper.EncryptStringToBytes(cryptString, instructions[i].Key, instructions[i].IV));
                    //Console.WriteLine($"{times - i} Layer of encryption: {message.Content}");
                }
            }
            else 
            {
                // Encrypt in normal order for server->client communication
                // message ( encrypted: key1 -> key2 -> key3 ) 
                for (int i = 0; i < times; i++)
                {
                    cryptString = Convert.ToBase64String(CryptoHelper.EncryptStringToBytes(cryptString, instructions[i].Key, instructions[i].IV));
                    //Console.WriteLine($"{i + 1} Layer of encryption: {cryptString.Substring(0, 20)}...");
                }
            }

            return new Message(cryptString);
        }

    }
}