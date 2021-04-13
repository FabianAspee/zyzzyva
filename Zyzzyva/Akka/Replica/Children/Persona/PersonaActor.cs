using Akka.Actor;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Akka.Replica.Messages.Response;

namespace Zyzzyva.Akka.Replica.Children.Persona
{
    /// <include file="Docs/Akka/Replica/Children/Persona/PersonaActor.xml" path='docs/members[@name="persona"]/Persona/*'/>
    public class PersonaActor : AbstractPersonaChild
    {
        ///<inheritdoc/>
        protected override void PreStart() => _crud.ReadFilePersona();
        /// <include file="Docs/Akka/Replica/Children/Persona/PersonaActor.xml" path='docs/members[@name="persona"]/PersonaC/*'/>
        public PersonaActor()
        {

            Receive<OrderReq<IPersonaRequest>>(msg1 => msg1.Request.GetRequest() is ReadPersona, msg =>
            {
                var response = new ReadPersonaResponse(_crud.ReadPersona((msg.Request.GetRequest() as ReadPersona).Id));
                SendToFather(response, msg);
            });

            Receive<OrderReq<IPersonaRequest>>(msg1 => msg1.Request.GetRequest() is ReadAllPersona, msg =>
            {  //METTERE A POSTO TUTTI QUESTI COGLIONi :)
                var responseCrud=_crud.ReadAllPersone();
                var response = new ReadAllPersonaResponse(responseCrud,!responseCrud.IsEmpty);
                SendToFather(response, msg);
            });

            Receive<OrderReq<IPersonaRequest>>(msg1 => msg1.Request.GetRequest() is DeletePersona, msg =>
            {
                var responseCrud=_crud.DeletePersona((msg.Request.GetRequest() as DeletePersona).Id);
                var response = new DeletePersonaResponse(responseCrud.Item1, responseCrud.Item2);
                SendToFather(response, msg);
            });

            Receive<OrderReq<IPersonaRequest>>(msg1 => msg1.Request.GetRequest() is UpdatePersona, msg =>
            {
                var responseCrud=_crud.UpdatePersona((msg.Request.GetRequest() as UpdatePersona).Persona);
                var response = new UpdatePersonaResponse(responseCrud.Item1, responseCrud.Item2);
                SendToFather(response, msg);
            });

            Receive<OrderReq<IPersonaRequest>>(msg1 => msg1.Request.GetRequest() is InsertPersona, msg =>
            {
                var responseCrud=_crud.InsertPersona((msg.Request.GetRequest() as InsertPersona).Persona);
                var response = new InsertPersonaResponse(responseCrud.Item1,responseCrud.Item2);
                SendToFather(response, msg);
            });

            Receive<Checkpoint>(msg1 => _crud.WriteFilePersona());
            Receive<RevertAction>(msg1 => _crud.RollBackToLastAction());
            Receive<AskSnapshot>(msg1 => Sender.Tell(new SnapshotReply(_crud.GetSnapshot()), Self));
            Receive<SnapshotReply>(msg1 =>
            {
                _crud.SaveSnapshot(msg1.StreamReader);
                Sender.Tell(new SnapshotSave(), Self);
            });
        }

        /// <include file="Docs/Akka/Replica/Children/Persona/PersonaActor.xml" path='docs/members[@name="persona"]/MyProps/*'/>
        public static Props MyProps() => Props.Create(() => new PersonaActor());


    }
}
