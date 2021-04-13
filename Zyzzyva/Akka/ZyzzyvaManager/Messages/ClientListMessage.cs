using Akka.Actor;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientListMessage.xml" path='docs/members[@name="clientlistmessage"]/ClientListMessage/*'/>
    public class ClientListMessage
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientListMessage.xml" path='docs/members[@name="clientlistmessage"]/Clients/*'/>
        public readonly Dictionary<IActorRef, RSAParameters> Clients;


        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ClientListMessage.xml" path='docs/members[@name="clientlistmessage"]/ClientListMessageC/*'/>
        public ClientListMessage(Dictionary<IActorRef, RSAParameters> clients) => Clients = clients;
    }
}
