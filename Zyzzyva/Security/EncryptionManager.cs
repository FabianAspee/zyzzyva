using System;
using System.Security.Cryptography;
using System.Text;

namespace Zyzzyva.Security
{
    /// <include file="Docs/Security/EncryptionManager.xml" path='docs/members[@name="encryption_manager"]/EncryptionManager/*'/>  
    public static class EncryptionManager
    {
        /// <include file="Docs/Security/EncryptionManager.xml" path='docs/members[@name="encryption_manager"]/SignMsg/*'/>  
        public static byte[] SignMsg(string toSign, RSAParameters privateKey) => RSAEncrypt(DigestManager.GenerateSHA512String(toSign), privateKey);

        /// <include file="Docs/Security/EncryptionManager.xml" path='docs/members[@name="encryption_manager"]/VerifySignature/*'/>  
        public static bool VerifySignature(byte[] signature, string toVerify, RSAParameters key) => RSADecrypt(signature, toVerify, key);

        /// <include file="Docs/Security/EncryptionManager.xml" path='docs/members[@name="encryption_manager"]/GenKey_SaveInContainer/*'/>  
        public static RSAParameters GeneratePrivateKey()
        {
            
            using var rsa = RSA.Create(1024);  
            return rsa.ExportParameters(true);
        }

        /// <include file="Docs/Security/EncryptionManager.xml" path='docs/members[@name="encryption_manager"]/GetKeyFromContainer/*'/>  
        public static RSAParameters GetPublicKey(RSAParameters parameters)
        {
           
            using var rsa = RSA.Create(parameters); 
            return rsa.ExportParameters(false);
        }

        private readonly static UnicodeEncoding ByteConverter = new UnicodeEncoding();

        private static byte[] RSAEncrypt(string DataToEncrypt, RSAParameters RSAKeyInfo)
        {
            try
            {
                byte[] encryptedData;
                
                using (RSA RSA = RSA.Create(1024))
                { 
                    RSA.ImportParameters(RSAKeyInfo); 
                    encryptedData = RSA.SignData(ByteConverter.GetBytes(DataToEncrypt), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }
                return encryptedData;
            } 
            catch (CryptographicException)
            {
                return null;
            }
        }
        private static bool RSADecrypt(byte[] signature, string toVerify, RSAParameters RSAKeyInfo)
        {
            try
            {
                bool decryptedData; 
                using (RSA RSA = RSA.Create(1024))
                { 
                    RSA.ImportParameters(RSAKeyInfo);
                     
                    decryptedData = RSA.VerifyData(ByteConverter.GetBytes(toVerify), signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }
                return decryptedData;
            } 
            catch (CryptographicException)
            {
                return false;
            }
        }


    }
}
