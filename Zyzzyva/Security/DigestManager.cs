using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Zyzzyva.Akka.Replica.Messages;

namespace Zyzzyva.Security
{
    /// <include file="Docs/Security/DigestManager.xml" path='docs/members[@name="digest_manager"]/DigestManager/*'/>  
    public static class DigestManager
    {
        /// <include file="Docs/Security/DigestManager.xml" path='docs/members[@name="digest_manager"]/GenerateFromString/*'/>  
        public static string GenerateSHA512String(string inputString)
        {
            SHA512 sha512 = SHA512.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            byte[] hash = sha512.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }
         
        /// <include file="Docs/Security/DigestManager.xml" path='docs/members[@name="digest_manager"]/DigestList/*'/>  
        public static string DigestList(string history, string request) => GenerateSHA512String(string.Concat(history,request));


        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }

      
    }
}
