using Akka.Actor;
using System.Collections.Immutable;
using System.Security.Cryptography;


namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicasListMessage.xml" path='docs/members[@name="replicaslistmessage"]/ReplicasListMessage/*'/>
    public class ReplicasListMessage
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicasListMessage.xml" path='docs/members[@name="replicaslistmessage"]/Replicas/*'/>
        public readonly ImmutableList<(IActorRef, int, RSAParameters)> Replicas;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicasListMessage.xml" path='docs/members[@name="replicaslistmessage"]/MaxFailures/*'/>
        public readonly int MaxFailures;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicasListMessage.xml" path='docs/members[@name="replicaslistmessage"]/ReplicasListMessageC/*'/>
        public ReplicasListMessage(ImmutableList<(IActorRef, int, RSAParameters)> replicas, int maxFailures) => (Replicas, MaxFailures) = ( replicas, maxFailures);
    }
}
