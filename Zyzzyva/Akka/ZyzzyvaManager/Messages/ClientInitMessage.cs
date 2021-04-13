using System.Security.Cryptography;
namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientInitMessage.xml" path='docs/members[@name="clientinitmessage"]/ClientInitMessage/*'/>
    public class ClientInitMessage
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientInitMessage.xml" path='docs/members[@name="clientinitmessage"]/PublicKey/*'/>
        public readonly RSAParameters PublicKey;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientInitMessage.xml" path='docs/members[@name="clientinitmessage"]/ClientInitMessageC/*'/>
        public ClientInitMessage(RSAParameters key) => PublicKey = key;
    }
}
