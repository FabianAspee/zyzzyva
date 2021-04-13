using Akka.Actor;
using System.Security.Cryptography;

namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaAdd.xml" path='docs/members[@name="replicaadd"]/ReplicaAdd/*'/>
    public class ReplicaAdd
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaAdd.xml" path='docs/members[@name="replicaadd"]/ActorRef/*'/>
        public readonly (IActorRef, int, RSAParameters) ActorRef;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaAdd.xml" path='docs/members[@name="replicaadd"]/ReplicaAdd/*'/>
        public ReplicaAdd((IActorRef, int, RSAParameters) actorRef) => ActorRef = actorRef;
    }
}
