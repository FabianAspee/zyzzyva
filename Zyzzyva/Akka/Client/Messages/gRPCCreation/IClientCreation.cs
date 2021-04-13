using Akka.Actor;

namespace Zyzzyva.Akka.Client.Messages.gRPCCreation
{
    /// <include file="Docs/Akka/Client/Messages/gRPCCreation/IClientCreation.xml" path='docs/members[@name="iclientcreation"]/IClientCreation/*'/>
    public interface IClientCreation
    {
        /// <include file="Docs/Akka/Client/Messages/gRPCCreation/IClientCreation.xml" path='docs/members[@name="iclientcreation"]/Actor/*'/>
        public IActorRef Actor {get; }
    }
}
