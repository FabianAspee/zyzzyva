using Akka.Actor;

namespace Zyzzyva.Akka.Client.Messages
{
    /// <include file="Docs/Akka/Client/Messages/RemoveClient.xml" path='docs/members[@name="removeclient"]/RemoveClient/*'/>
    public class RemoveClient
    {
        /// <include file="Docs/Akka/Client/Messages/RemoveClient.xml" path='docs/members[@name="removeclient"]/Actor/*'/>
        public readonly IActorRef Actor;

        /// <include file="Docs/Akka/Client/Messages/RemoveClient.xml" path='docs/members[@name="removeclient"]/RemoveClientC/*'/>
        public RemoveClient(IActorRef actor) => Actor = actor;
    }
}
