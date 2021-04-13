using Akka.Actor;

namespace Zyzzyva.Akka.Client.Messages.gRPCCreation
{
    /// <include file="Docs/Akka/Client/Messages/gRPCCreation/PersonaClient.xml" path='docs/members[@name="personaclient"]/PersonaClient/*'/>
    public class PersonaClient : IClientCreation
    {
        /// <include file="Docs/Akka/Client/Messages/gRPCCreation/PersonaClient.xml" path='docs/members[@name="personaclient"]/PersonaClientC/*'/>
        public PersonaClient(IActorRef actor)=> Actor = actor;
        

        ///<inheritdoc/>
        public IActorRef Actor { get;}
        

    }
}
