using System.Security.Cryptography;

namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaInitMessage.xml" path='docs/members[@name="replicainitmessage"]/ReplicaInitMessage/*'/>
    public class ReplicaInitMessage
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaInitMessage.xml" path='docs/members[@name="replicainitmessage"]/PublicKey/*'/>
        public readonly RSAParameters PublicKey;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaInitMessage.xml" path='docs/members[@name="replicainitmessage"]/ReplicaInitMessageC/*'/>
        public ReplicaInitMessage(RSAParameters key) => PublicKey = key;
    }
}
