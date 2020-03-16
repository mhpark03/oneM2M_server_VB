using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace HttpServer
{
    class EncodingHelper
    {
        public static string Base64Encoding(string EncodingText, System.Text.Encoding oEncoding)
        {
            if (oEncoding == null)
                oEncoding = System.Text.Encoding.UTF8;

            byte[] arr = oEncoding.GetBytes(EncodingText);
            return System.Convert.ToBase64String(arr);
        }

        public static string Base64Decoding(string DecodingText, System.Text.Encoding oEncoding)
        {
            if (oEncoding == null)
                oEncoding = System.Text.Encoding.UTF8;

            byte[] arr = System.Convert.FromBase64String(DecodingText);
            return oEncoding.GetString(arr);
        }        

        // 키
        private static readonly string KEY = "01234567890123456789012345678901";
 
        //128bit (16자리)
        private static readonly string KEY_128 = KEY.Substring(0, 128 / 8);

        //AES 128 암호화.., CBC, PKCS7, 예외발생하면 null
        public static string encryptAES128(string plain)
        {
            try
            {
                //바이트로 변환 
                byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
 
                //레인달 알고리듬
                RijndaelManaged rm = new RijndaelManaged();
                //자바에서 사용한 운용모드와 패딩방법 일치시킴(AES/CBC/PKCS5Padding)
                rm.Mode = CipherMode.CBC;
                rm.Padding = PaddingMode.PKCS7;
                rm.KeySize = 128;
 
                //메모리스트림 생성
                MemoryStream memoryStream = new MemoryStream();
 
                //key, iv값 정의
                ICryptoTransform encryptor = rm.CreateEncryptor(Encoding.UTF8.GetBytes(KEY_128), Encoding.UTF8.GetBytes(KEY_128));
                //크립토스트림을 키와 IV값으로 메모리스트림을 이용하여 생성
                CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
 
                //크립트스트림에 바이트배열을 쓰고 플러시..
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
 
                //메모리스트림에 담겨있는 암호화된 바이트배열을 담음
                byte[] encryptBytes = memoryStream.ToArray();
 
                //베이스64로 변환
                string encryptString = Convert.ToBase64String(encryptBytes);
 
                //스트림 닫기.
                cryptoStream.Close();
                memoryStream.Close();
 
                return encryptString;
            }
            catch (Exception)
            {
                return null;
            }
        }
 
        //AES128 복호화.., CBC, PKCS7, 예외발생하면 null
        public static string decryptAES128(string encrypt)
        {
            try
            {
                //base64를 바이트로 변환 
                byte[] encryptBytes = Convert.FromBase64String(encrypt);
                //byte[] encryptBytes = Encoding.UTF8.GetBytes(encryptString);
 
                //레인달 알고리듬
                RijndaelManaged rm = new RijndaelManaged();
                //자바에서 사용한 운용모드와 패딩방법 일치시킴(AES/CBC/PKCS5Padding)
                rm.Mode = CipherMode.CBC;
                rm.Padding = PaddingMode.PKCS7;
                rm.KeySize = 128;
 
                //메모리스트림 생성
                MemoryStream memoryStream = new MemoryStream(encryptBytes);
 
                //key, iv값 정의
                ICryptoTransform decryptor = rm.CreateDecryptor(Encoding.UTF8.GetBytes(KEY_128), Encoding.UTF8.GetBytes(KEY_128));
                //크립토스트림을 키와 IV값으로 메모리스트림을 이용하여 생성
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
 
                //복호화된 데이터를 담을 바이트 배열을 선언한다. 
                byte[] plainBytes = new byte[encryptBytes.Length];
 
                int plainCount = cryptoStream.Read(plainBytes, 0, plainBytes.Length);
 
                //복호화된 바이트 배열을 string으로 변환
                string plainString = Encoding.UTF8.GetString(plainBytes, 0, plainCount);
 
                //스트림 닫기.
                cryptoStream.Close();
                memoryStream.Close();
 
                return plainString;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}
