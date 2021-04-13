using Akka.Actor;
using Akka.Cluster;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Zyzzyva.Akka.Membri.Children;
using Zyzzyva.Akka.Membri.Messages;
using Zyzzyva.Akka.ZyzzyvaManager.Messages;


namespace Zyzzyva.Akka.ZyzzyvaManager
{
    /// <include file="Docs/Akka/ZyzzyvaManager/ZyzzyvaManager.xml" path='docs/members[@name="zyzzyvamanager"]/ZyzzyvaManager/*'/>
    public class ZyzzyvaManager : ReceiveActor, IWithUnboundedStash, IWithTimers
    {
        private readonly IActorRef _replicaRouter;
        private readonly IActorRef _clientRouter;
        private int actualView = 0;
        private readonly int _maxFailures;
        private readonly Dictionary<string, (IActorRef, int, RSAParameters)> _replicas = new();
        private readonly Dictionary<IActorRef, RSAParameters> _clients = new();
        private ImmutableQueue<int> replicaNumber = ImmutableQueue<int>.Empty;
        ///<inheritdoc/>
        public IStash Stash { get; set; }
        ///<inheritdoc/>
        public ITimerScheduler Timers { get; set; }

        private int _replicaCount = 0;
        /// <include file="Docs/Akka/ZyzzyvaManager/ZyzzyvaManager.xml" path='docs/members[@name="zyzzyvamanager"]/ZyzzyvaManagerC/*'/>
        public ZyzzyvaManager(int f)
        {
            _maxFailures = f;
            Context.ActorOf(ClusterListener.MyProps(Self, Cluster.Get(Context.System)), "clusterListener");
            _replicaRouter = Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "replicaRouter");
            _clientRouter = Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "clientRouter");
         
            Receive<NodeArrived>(_ =>
            {
                if (_replicaCount < 3 * _maxFailures + 1)
                {
                    replicaNumber = replicaNumber.Enqueue(_replicaCount);
                    _replicaCount++;
                }
                if (_replicaCount == 3 * _maxFailures + 1)
                {

                    Timers.StartSingleTimer("call_register_replica", new NodeArrived(), TimeSpan.FromSeconds(20));
                    _replicaRouter.Tell(new ClusterReady(), Self);
                }
            });

            Receive<ReplicaInitMessage>(msg => InitReplica(Sender, msg.PublicKey));

            ReceiveAny(_ => Stash.Stash());
        }

        /// <include file="Docs/Akka/ZyzzyvaManager/ZyzzyvaManager.xml" path='docs/members[@name="zyzzyvamanager"]/MyProps/*'/>
        public static Props MyProps(int f) => Props.Create(() => new ZyzzyvaManager(f));

        private void ReplicasArrived()
        {
            Timers.StartPeriodicTimer("call_register_client_ok", new ClusterOkClient(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
            Receive<ReplicaInitMessage>(msg =>
            {

                if (!replicaNumber.IsEmpty)
                {
                    Timers.Cancel("call_register_replica");
                    var id = replicaNumber.Peek();
                    Sender.Tell(new ReplicaNumberMessage(id, actualView));
                    Sender.Tell(new ClientListMessage(_clients.ToDictionary(x => x.Key, x => x.Value)));
                    replicaNumber = replicaNumber.Dequeue();
                    _replicaRouter.Tell(new ReplicaAdd((Sender, id, msg.PublicKey)));
                    _replicas.TryAdd(Sender.Path.Address.HostPort(), (Sender, id, msg.PublicKey));

                    _clientRouter.Tell(new ReplicaAdd((Sender, id, msg.PublicKey)));
                }
            });
            Receive<GetAnotherReplicas>(_clientRouter => Sender.Tell(new ReplicasListMessage(_replicas.Values.ToImmutableList(), _maxFailures)));
            Receive<NodeArrived>(_ =>
            {

                Timers.StartSingleTimer("call_register_replica", new NodeArrived(), TimeSpan.FromSeconds(20));
                _replicaRouter.Tell(new ClusterReady(), Self);

            });

            Receive<FinalNewView>(view => actualView = view.View > actualView ? view.View : actualView);
            Receive<ClusterOkClient>(_ => _clientRouter.Tell(new ClusterReady(), Self));

            Receive<ClientInitMessage>(msg =>
            {
                _clients.TryAdd(Sender, msg.PublicKey);
                Sender.Tell(new ReplicasListMessage(_replicas.Values.ToImmutableList(), _maxFailures));
                _replicaRouter.Tell(new ClientAndKey(msg.PublicKey, Sender));
            });
            Receive<POSTMORTEM>(msg =>
            {
                if (_replicas.Remove(Sender.Path.Address.HostPort(), out (IActorRef, int, RSAParameters) actorRef))
                {
                    replicaNumber = replicaNumber.Enqueue(actorRef.Item2);
                    _replicaRouter.Tell(new ReplicaDead(actorRef));
                    _clientRouter.Tell(new ReplicaDead(actorRef));

                }
            });
            ReceiveAny(_ => { });
        }


        private void InitReplica(IActorRef replica, RSAParameters publicKey)
        {
            var id = replicaNumber.Peek();
            replica.Tell(new ReplicaNumberMessage(id, actualView));
            replicaNumber = replicaNumber.Dequeue();
            _replicas.TryAdd(replica.Path.Address.HostPort(), (replica, id, publicKey));
            if (replicaNumber.IsEmpty)
            {
                Timers.Cancel("call_register_replica");
                _replicaRouter.Tell(new ReplicasListMessage(_replicas.Values.ToImmutableList(), _maxFailures)); 
                Stash.UnstashAll();
                Become(ReplicasArrived);
            }
        }
    }
}
