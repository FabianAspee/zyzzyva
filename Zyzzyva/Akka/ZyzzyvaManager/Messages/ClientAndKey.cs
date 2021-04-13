using Akka.Actor;
using System.Security.Cryptography;
namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientAndKey.xml" path='docs/members[@name="clientandkey"]/ClientAndKey/*'/>
    public class ClientAndKey
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientAndKey.xml" path='docs/members[@name="clientandkey"]/PublicKey/*'/>
        public readonly RSAParameters PublicKey;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientAndKey.xml" path='docs/members[@name="clientandkey"]/ActorRef/*'/>
        public readonly IActorRef ActorRef;
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientAndKey.xml" path='docs/members[@name="clientandkey"]/ClientAndKeyC/*'/>
        public ClientAndKey(RSAParameters key, IActorRef actor) => (ActorRef, PublicKey) = (actor, key);
    }
}
