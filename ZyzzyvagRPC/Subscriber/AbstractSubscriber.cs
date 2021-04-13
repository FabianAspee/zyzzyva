using Akka.Actor;
using System.Threading;
using ZyzzyvaRPC.ClusterClientAccess;

namespace ZyzzyvagRPC.Subscriber
{
    /// <include file="../Docs/Subscriber/AbstractSubscriber.xml" path='docs/members[@name="abstractsubscriber"]/AbstractSubscriber/*'/>
    public abstract class AbstractSubscriber : ISubscriber
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        /// <include file="../Docs/Subscriber/AbstractSubscriber.xml" path='docs/members[@name="abstractsubscriber"]/IActorRef/*'/>
        protected IActorRef _actor;

        /// <inheritdoc/>
        public abstract void CreateActor();

        /// <inheritdoc/>
        public AbstractSubscriber()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        /// <include file="../Docs/Subscriber/AbstractSubscriber.xml" path='docs/members[@name="abstractsubscriber"]/Dispose/*'/>
        public void Dispose()
        {
            ClusterClientAccess.KillActor(_actor);
            _cancellationTokenSource.Cancel();
        }
    }

}
