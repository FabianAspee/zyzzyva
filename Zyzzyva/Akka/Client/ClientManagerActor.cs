using Akka.Actor;
using Akka.Routing;
using System.Linq;
using Zyzzyva.Akka.Client.Children;
using Zyzzyva.Akka.Client.Messages;
using Zyzzyva.Akka.Client.Messages.gRPCCreation;

namespace Zyzzyva.Akka.Client
{
    /// <include file="Docs/Akka/Client/ClientManagerActor.xml" path='docs/members[@name="clientmanageractor"]/ClientManagerActor/*'/>
    public class ClientManagerActor : ReceiveActor
    {
        private readonly IActorRef _clientRouter;
        ///<inheritdoc/>
        protected override void PreStart() => Enumerable.Range(0, 3).ToList().ForEach(x => Context.ActorOf(ClientActor.MyProps, $"client{x}"));
        /// <include file="Docs/Akka/Client/ClientManagerActor.xml" path='docs/members[@name="clientmanageractor"]/ClientManagerActorC/*'/>
        public ClientManagerActor()
        { 
            _clientRouter = Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "clientRouter");

            Receive<PersonaClient>(msg => _clientRouter.Forward(msg));
        
            Receive<RemoveClient>(msg => Context.Stop(msg.Actor));
        }
        /// <include file="Docs/Akka/Client/ClientManagerActor.xml" path='docs/members[@name="clientmanageractor"]/MyProps/*'/>
        public static Props MyProps => Props.Create(() => new ClientManagerActor());

    }
}
