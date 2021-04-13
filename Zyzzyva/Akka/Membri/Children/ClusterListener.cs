using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Zyzzyva.Akka.Membri.Messages;
using static Akka.Cluster.ClusterEvent;

namespace Zyzzyva.Akka.Membri.Children
{
    /// <include file="Docs/Akka/Membri/Children/ClusterListener.xml" path='docs/members[@name="clusterlistener"]/ClusterListener/*'/>
    public class ClusterListener : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly Cluster _cluster;
        private readonly IActorRef _zyzzyvaManager;

        /// <include file="Docs/Akka/Membri/Children/ClusterListener.xml" path='docs/members[@name="clusterlistener"]/PreStart/*'/>
        protected override void PreStart() => _cluster.Subscribe(Self, InitialStateAsEvents,
               new[] { typeof(IMemberEvent), typeof(UnreachableMember) });

        /// <include file="Docs/Akka/Membri/Children/ClusterListener.xml" path='docs/members[@name="clusterlistener"]/PostStop/*'/>
        protected override void PostStop() => _cluster.Unsubscribe(Self);
        /// <include file="Docs/Akka/Membri/Children/ClusterListener.xml" path='docs/members[@name="clusterlistener"]/ClusterListenerC/*'/>
        public ClusterListener(IActorRef zyzzyvaManager, Cluster cluster)
        {

            _cluster = cluster;
            _zyzzyvaManager = zyzzyvaManager;

            Receive<MemberUp>(member =>
            {
                _zyzzyvaManager.Tell(new NodeArrived()); 
                _log.Info($"Node {member.Member.Status} - Member is Up: {member.Member.Address}");
            });
            Receive<UnreachableMember>(member => _log.Info($"Node {member.Member.UniqueAddress} - Member detected as unreachable: {member.Member.Address}"));
            Receive<MemberRemoved>(member =>
            { 
                _log.Info($"Node {member.Member.UniqueAddress} - Member is Removed: {member.Member.Address} after {member.PreviousStatus }");
            });

        }


        /// <include file="Docs/Akka/Membri/Children/ClusterListener.xml" path='docs/members[@name="clusterlistener"]/MyProps/*'/>
        public static Props MyProps(IActorRef zyzzyvaManager, Cluster cluster) => Props.Create(() => new ClusterListener(zyzzyvaManager, cluster));
    }
}