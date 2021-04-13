using Akka.Actor;
using System;
using Zyzzyva.Akka.Replica.Children.Persona;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;

namespace Zyzzyva.Akka.Replica.Children
{
    /// <include file="Docs/Akka/Replica/Children/DatabaseActor.xml" path='docs/members[@name="database"]/DatabaseActor/*'/>
    public class DatabaseActor : ReceiveActor
    {
        private readonly IActorRef _personaActor;

        /// <include file="Docs/Akka/Replica/Children/DatabaseActor.xml" path='docs/members[@name="database"]/DatabaseActorC/*'/>
        public DatabaseActor()
        {
            _personaActor = Context.ActorOf(PersonaActor.MyProps());
            ReceiveRequest();
        }


        private void ReceiveRequest()
        {
            Receive<OrderReq<IPersonaRequest>>(msg =>
            {
                Console.WriteLine($"ARRIVATA IN DATABASE, + + + + {msg.OrderReqSigned.View} - - - {msg.OrderReqSigned.SequenceNumber}");
                _personaActor.Forward(msg);
            });
            Receive<Checkpoint>(msg => _personaActor.Forward(msg)); 
            Receive<RevertAction>(msg => _personaActor.Forward(msg));
            Receive<AskSnapshot>(msg1 => _personaActor.Forward(msg1));
            Receive<SnapshotReply>(msg1 => _personaActor.Forward(msg1));
        }

        /// <include file="Docs/Akka/Replica/Children/DatabaseActor.xml" path='docs/members[@name="database"]/MyProps/*'/>
        public static Props MyProps() => Props.Create(() => new DatabaseActor());
    }
}
