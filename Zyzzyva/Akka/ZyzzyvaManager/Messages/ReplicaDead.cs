using Akka.Actor;
using System.Security.Cryptography;

namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaDead.xml" path='docs/members[@name="replicadead"]/ReplicaDead/*'/>
    public class ReplicaDead
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaDead.xml" path='docs/members[@name="replicadead"]/ReplicaDead/*'/>
        public readonly (IActorRef, int, RSAParameters) ActorRef;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaDead.xml" path='docs/members[@name="replicadead"]/ReplicaDead/*'/>
        public ReplicaDead((IActorRef, int, RSAParameters) actorRef) => ActorRef = actorRef;
    }
}
