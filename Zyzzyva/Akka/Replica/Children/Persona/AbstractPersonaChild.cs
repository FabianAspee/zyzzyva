using Akka.Actor;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database;
using Zyzzyva.Database.Tables;
using Zyzzyva.Security;

namespace Zyzzyva.Akka.Replica.Children.Persona
{
    /// <include file="Docs/Akka/Replica/Children/Persona/AbstractPersonaChild.xml" path='docs/members[@name="abstractpersona"]/AbstractPersonaChild/*'/>
    public class AbstractPersonaChild : ReceiveActor
    {
        /// <include file="Docs/Akka/Replica/Children/Persona/AbstractPersonaChild.xml" path='docs/members[@name="abstractpersona"]/_crud/*'/>
        protected readonly IPersonaCRUD _crud = new PersonaCRUDdb();

        /// <include file="Docs/Akka/Replica/Children/Persona/AbstractPersonaChild.xml" path='docs/members[@name="abstractpersona"]/SendToFather/*'/>
        protected void SendToFather<T>(IPersonaResponse response, OrderReq<T> msg) =>
            Sender.Tell(new SpecResponse<IPersonaResponse>(response, msg.OrderReqSigned,
                new SpecResponseSigned(msg.OrderReqSigned.View, msg.OrderReqSigned.SequenceNumber,
                    DigestManager.GenerateSHA512String(response.ToString()), msg.Request.Client, msg.Request.Timestamp), msg.Signature));
    }
}
