using Akka.Actor;

namespace Zyzzyva.Akka.Client.Messages
{
    /// <include file="Docs/Akka/Client/Messages/CreateClientResponse.xml" path='docs/members[@name="createclientresponse"]/CreateClientResponse/*'/>
    public class CreateClientResponse
    {
        /// <include file="Docs/Akka/Client/Messages/CreateClientResponse.xml" path='docs/members[@name="createclientresponse"]/_actor/*'/>
        public readonly IActorRef _actor;
        /// <include file="Docs/Akka/Client/Messages/CreateClientResponse.xml" path='docs/members[@name="createclientresponse"]/CreateClientResponseC/*'/>
        public CreateClientResponse(IActorRef actor) => _actor = actor;
    }
}
