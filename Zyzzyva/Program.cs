using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Zyzzyva.Akka.Client;
using Zyzzyva.Akka.Replica;
using Zyzzyva.Akka.ZyzzyvaManager;

namespace Zyzzyva
{
   ///<inheritdoc/>
    public class Program
    {

        private static readonly string REPLICA_NAME = "replica_manager";
        private static readonly string CLIENT_NAME = "client_manager";
        private static readonly string MANAGER_NAME = "zyzzyva_manager";

        ///<inheritdoc/>
        public static void Main(string[] args)
        {
            StartUp(args.Length == 1 ? args[0] : "0");
        }

        private static void StartUp(string port)
        {
            var (finalIp, finalPort) = GetElement(port);
            var hocon = File.ReadAllText(ConfigurationManager.AppSettings["configpath"]);
            var section = ConfigurationFactory.ParseString(hocon);
            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + finalIp)
                .WithFallback(section);

            var config2 =
                ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + finalPort)
                    .WithFallback(config);

            var system = ActorSystem.Create("cluster-playground", config2);
            if (int.Parse(finalPort) == 9091)
            {
                system.ActorOf(ZyzzyvaManager.MyProps(int.Parse(Environment.GetEnvironmentVariable("MAX_FAILURES"))), MANAGER_NAME);
            }
            system.ActorOf(ReplicaManager.MyProps, REPLICA_NAME);
            var clientManager = system.ActorOf(ClientManagerActor.MyProps, CLIENT_NAME);
            ClusterClientReceptionist.Get(system).RegisterService(clientManager);

            system.WhenTerminated.Wait();
        }

        private static (string, string) GetElement(string port)
        {
            var finalport = Environment.GetEnvironmentVariable("CLUSTER_PORT") ?? port;
            var finalIp = Environment.GetEnvironmentVariable("CLUSTER_IP") == null || Environment.GetEnvironmentVariable("CLUSTER_IP").Length == 0 ? Dns.GetHostEntry(Dns.GetHostName()).AddressList.First().ToString() : Environment.GetEnvironmentVariable("CLUSTER_IP");
            return (finalIp, finalport);
        }
    }
}
