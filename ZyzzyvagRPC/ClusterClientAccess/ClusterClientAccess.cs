using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using System;
using System.Collections.Immutable;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Zyzzyva.Akka.Client.Messages.gRPCCreation;

namespace ZyzzyvaRPC.ClusterClientAccess
{
    /// <include file="../Docs/ClusterClientAccess/ClusterClientAccess.xml" path='docs/members[@name="clusterclientccess"]/ClusterClientAccess/*'/>
    public sealed class ClusterClientAccess
    {

        private static readonly Lazy<ClusterClientAccess> instance = new(() => new ClusterClientAccess());
        private static readonly ActorSystem system = Create();
        private static readonly IActorRef clusterClient = CreateClusterClient(); 
        private static ActorSystem Create()
        {
            var hocon = File.ReadAllText(ConfigurationManager.AppSettings["configpath"]);
            var section = ConfigurationFactory.ParseString(hocon);
            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + Dns.GetHostEntry(Dns.GetHostName()).AddressList.First().ToString())
               .WithFallback(section);
            return ActorSystem.Create("cluster", config);
        }

        private ClusterClientAccess() { }

        private static IActorRef CreateClusterClient()
        {
            var t = ImmutableHashSet.Create(ActorPath.Parse("akka.tcp://cluster-playground@zyzzyva:9090/system/receptionist"));
            return system.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(system).WithInitialContacts(t)), "client");

        }
        /// <include file="../Docs/ClusterClientAccess/ClusterClientAccess.xml" path='docs/members[@name="clusterclientccess"]/Instance/*'/>
        public static ClusterClientAccess Instance
        {
            get { return instance.Value; }
        }
        /// <include file="../Docs/ClusterClientAccess/ClusterClientAccess.xml" path='docs/members[@name="clusterclientccess"]/CreateActor/*'/>
        public static IActorRef CreateActor(Props props)
        {
            var actor = system.ActorOf(props);
            var msg = new PersonaClient(actor); 
            clusterClient.Tell(new ClusterClient.Send("/user/client_manager", msg, localAffinity: true));

            return actor;
        }
        /// <include file="../Docs/ClusterClientAccess/ClusterClientAccess.xml" path='docs/members[@name="clusterclientccess"]/KillActor/*'/>
        public static void KillActor(IActorRef actor)
        { 
            system.Stop(actor);
        }

    }
}
