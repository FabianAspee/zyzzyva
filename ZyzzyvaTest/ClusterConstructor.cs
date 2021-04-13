using Akka.Actor.Dsl;
using Zyzzyva.Akka.Replica;
using Zyzzyva.Akka.ZyzzyvaManager.Messages;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using System;
using System.Threading.Tasks;
using Zyzzyva.Akka.Client.Children;
using Zyzzyva.Security;
using Akka.TestKit.Xunit2;
using System.IO;
using Akka.TestKit;
using Akka.Routing;

namespace ZyzzyvaTest
{
    public static class ClusterConstructor
    { 
        public static List<IActorRef> ConstructReplicas(TestKit testkit, int replicaNumber, int numberOfReplicaS) => Enumerable.Range(numberOfReplicaS, replicaNumber)
            .Select(x=>
            {
                foreach (string dirPath in Directory.GetDirectories($"{Directory.GetCurrentDirectory()}\\Database", "*",
                           SearchOption.AllDirectories).Where(x => x.EndsWith("DB") || x.EndsWith("Settings"))) 
                    Directory.CreateDirectory(dirPath.Replace($"{Directory.GetCurrentDirectory()}\\Database", $"{Directory.GetCurrentDirectory()}\\Database{x}"));
                 
                Directory.GetFiles($"{Directory.GetCurrentDirectory()}\\Database", "*.*",
                     SearchOption.AllDirectories).Where(x => x.Contains("\\DB\\") || x.Contains("\\Settings\\")).ToList()
                     .ForEach(newPath => {
                         var result = newPath.Replace($"{Directory.GetCurrentDirectory()}\\Database", $"{Directory.GetCurrentDirectory()}\\Database{x}");
                         
                         File.Copy(newPath, result, true);
                         if(newPath.EndsWith("dbconfig.hocon"))
                         {
                             var hocon = File.ReadAllText(newPath);
                             var config = hocon.Replace("path: DB/databaseZyzzyva.json", $"path: Database{x}/DB/databaseZyzzyva.json");
                            
                              File.WriteAllText(newPath, config.ToString());
                         }
                     });

                var replica = testkit.Sys.ActorOf(ReplicaManager.MyProps);
                Task.Delay(1500).Wait();
                var hocon = File.ReadAllText($"{Directory.GetCurrentDirectory()}\\Database\\Settings\\dbconfig.hocon");
                var config = hocon.Replace($"path: Database{x}/DB/databaseZyzzyva.json", "path: DB/databaseZyzzyva.json");

                File.WriteAllText($"{Directory.GetCurrentDirectory()}\\Database\\Settings\\dbconfig.hocon", config.ToString());
                return replica;
            }).ToList();

       
        public async static Task<IActorRef> ConstructClient(TestKit testkit,  int replicaNumber, int maxFailures)
        {
            IActorRef client = testkit.Sys.ActorOf(ClientActor.MyProps);
            TaskCompletionSource<bool> agnagna = new();
            var replicas = await ConstructAllReplicas(testkit,replicaNumber,maxFailures);
            Action<IActorDsl> juanito = d => 
            {
                d.Receive<ClientInitMessage>(async (msg,sender) => 
                {
                    SendKey(msg.PublicKey,replicas.Select(x => x.Item1).ToList(),client);
                    client.Tell( new ReplicasListMessage(replicas.ToImmutableList(), maxFailures));
                    await Task.Delay(2500);
                    agnagna.SetResult(true);
                });

                d.Receive<string>((msg,sender) => 
                {
                    client.Tell(new ClusterReady(),sender.Self);
                });
            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            agnagna.Task.Wait();
            return client;
        }

        public async static Task<((IActorRef,RSAParameters),List<(TestProbe,int,RSAParameters)>)> ConstructClientAndReplicasControl(TestKit testkit, int replicaNumber, int maxFailures)
        {
            IActorRef client = testkit.Sys.ActorOf(ClientActor.MyProps);
            TaskCompletionSource<RSAParameters> publicKeyClientTask = new();
            var replicas = Enumerable.Range(0, replicaNumber).Select(x => (testkit.CreateTestProbe(),x,EncryptionManager.GeneratePrivateKey())).ToList();  
            
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<ClientInitMessage>(async (msg, sender) =>
                {
                    client.Tell(new ReplicasListMessage(replicas.Select(x=>(x.Item1.Ref,x.x,x.Item3)).ToImmutableList(), maxFailures));
                    await Task.Delay(2500);
                    publicKeyClientTask.SetResult(msg.PublicKey);
                });

                d.Receive<string>((msg, sender) =>
                {
                    client.Tell(new ClusterReady(), sender.Self);
                });
            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            var publicKeyClient = await publicKeyClientTask.Task;
            return ((client,publicKeyClient),replicas);
        }
        public async static Task<(IActorRef, List<(IActorRef, int, RSAParameters)>,RSAParameters)> ConstructClientWithPrimaryWithoutKeyOfClient(TestKit testkit, int replicaNumber, int maxFailures)
        {
            IActorRef client = testkit.Sys.ActorOf(ClientActor.MyProps);
            TaskCompletionSource<RSAParameters> agnagna = new();
            var replicas = await ConstructAllReplicas(testkit, replicaNumber, maxFailures);
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<ClientInitMessage>(async (msg, sender) =>
                { 
                    client.Tell(new ReplicasListMessage(replicas.ToImmutableList(), maxFailures));
                    await Task.Delay(2500);
                    agnagna.SetResult(msg.PublicKey);
                });

                d.Receive<string>((msg, sender) =>
                {
                    client.Tell(new ClusterReady(), sender.Self);
                });
            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            agnagna.Task.Wait();
            return (client,replicas.ToList(), agnagna.Task.Result);
        }

        public async static Task<List<(IActorRef,int,RSAParameters)>> ConstructAllReplicas(TestKit testkit, int replicaNumber, int maxFailures)
        {
            var replicas =  ConstructReplicas(testkit, replicaNumber,0);
            TaskCompletionSource<bool> agnagna = new();
            List<(IActorRef,int,RSAParameters)> actors = new();
            int arrived = 0;
            Action<IActorDsl> juanito = d => 
            {
                d.Receive<ReplicaInitMessage>(async (msg, sender) =>
                {
                    actors.Add((sender.Sender, arrived, msg.PublicKey));
                    sender.Sender.Tell(new ReplicaNumberMessage(arrived, 0)); 
                    arrived++;
                    if (arrived == replicaNumber)
                    {
                        replicas.ForEach(x => x.Tell(new ReplicasListMessage(actors.ToImmutableList(), maxFailures)));
                        await Task.Delay(2500);
                        agnagna.SetResult(true);
                    }
                });

                d.Receive<string>((msg, sender) =>
                {
                    replicas.ForEach(x => x.Tell(new ClusterReady(), sender.Self));
                });

            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            await agnagna.Task;
           
            return actors.ToList();
        }
        public async static Task<(List<(IActorRef, int, RSAParameters)>, Action<IActorDsl>)> ConstructAllReplicasWithManager(TestKit testkit, int replicaNumber, int maxFailures, TaskCompletionSource<List<FinalNewView>> finalTask)
        {
            var replicas = ConstructReplicas(testkit, replicaNumber, 0);
            TaskCompletionSource<bool> agnagna = new();
            List<(IActorRef, int, RSAParameters)> actors = new();
            int arrived = 0;
            var newView = new List<FinalNewView>();
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<ReplicaInitMessage>(async (msg, sender) =>
                {
                    actors.Add((sender.Sender, arrived, msg.PublicKey));
                    sender.Sender.Tell(new ReplicaNumberMessage(arrived, 0)); 
                    arrived++;
                    if (arrived == replicaNumber)
                    {
                        replicas.ForEach(x => x.Tell(new ReplicasListMessage(actors.ToImmutableList(), maxFailures)));
                        await Task.Delay(2500);
                        agnagna.SetResult(true);
                    }
                });
                d.Receive<FinalNewView>((msg, sender) =>
                {
                    newView.Add(msg);
                    if (newView.Count == 4)
                    {
                        finalTask.SetResult(newView);
                    }
                });
                d.Receive<string>((msg, sender) =>
                {
                    replicas.ForEach(x => x.Tell(new ClusterReady(), sender.Self));
                });
            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            await agnagna.Task;
             
            return (actors.ToList(), juanito);
        }

        public static void ConstructManagerRouter(TestKit testkit, TaskCompletionSource<IActorRef> managerRouter,string MANAGER_NAME)
        {
           
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<string>((msg, sender) =>
                {
                   var _clientRouter = sender.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "clientRouter");
                    managerRouter.SetResult(_clientRouter);
                });
                
            };
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)),MANAGER_NAME);
            parent.Tell("start");  
        }
         
        public async static Task<(List<(IActorRef, int, RSAParameters)>, IActorRef)> InitializeNReplicas(TestKit testkit, List<IActorRef> replicas,TaskCompletionSource<(IActorRef, int, RSAParameters)> newReplica)
        {
            TaskCompletionSource<bool> agnagna = new();
            List<(IActorRef, int, RSAParameters)> actors = new();
            int arrived = 0;
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<ReplicaInitMessage>(async (msg, sender) =>
                {
                    actors.Add((sender.Sender, arrived, msg.PublicKey));
                    sender.Sender.Tell(new ReplicaNumberMessage(arrived, 0)); 
                    arrived++;
                    if (arrived == replicas.Count)
                    {
                        replicas.ForEach(x => x.Tell(new ReplicasListMessage(actors.ToImmutableList(), 1)));
                        await Task.Delay(2500);
                        agnagna.SetResult(true);
                    }
                    else if (arrived > replicas.Count)
                    {
                        sender.Sender.Tell(new ReplicasListMessage(actors.ToImmutableList(), 1));
                        actors.Where(x => x.Item1 != sender.Sender).ToList().ForEach(x => x.Item1.Tell(new ReplicaAdd(actors.Last())));
                        await Task.Delay(2500);   
                        newReplica.SetResult(actors.Last());
                    }
                });

                d.Receive<string>((msg, sender) =>
                {
                    replicas.ForEach(x => x.Tell(new ClusterReady(), sender.Self));
                });
                d.Receive<IActorRef>((msg, sender) =>
                {
                    msg.Tell(new ClusterReady(), sender.Self);
                });


            }; 
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            await agnagna.Task;
             
            return (actors.ToList(),parent);
        }

         public async static Task<List<(IActorRef, int, RSAParameters)>> InitializeNReplicasNoPrimary(TestKit testkit, List<IActorRef> replicas, (IActorRef, int, RSAParameters) primary, TaskCompletionSource<List<FinalNewView>> newViewTask,int maxFailures)
        {
            TaskCompletionSource<bool> agnagna = new();
            List<(IActorRef, int, RSAParameters)> actors = new();
            actors.Add(primary);
            int arrived = 1;
            var newView = new List<FinalNewView>();
            Action<IActorDsl> juanito = d =>
            {
                d.Receive<ReplicaInitMessage>(async (msg, sender) =>
                {
                    actors.Add((sender.Sender, arrived, msg.PublicKey));
                    sender.Sender.Tell(new ReplicaNumberMessage(arrived, 0)); 
                    arrived++;
                    if (arrived == replicas.Count + 1)
                    {
                        replicas.ForEach(x => x.Tell(new ReplicasListMessage(actors.ToImmutableList(), maxFailures)));
                        await Task.Delay(2500);
                        agnagna.SetResult(true);
                    }
                });
                d.Receive<FinalNewView>((msg, sender) =>
                {
                    newView.Add(msg);
                    if (newView.Count == 3)
                    {
                        newViewTask.SetResult(newView);
                    }
                });
                d.Receive<string>((msg, sender) =>
                {
                    replicas.ForEach(x => x.Tell(new ClusterReady(), sender.Self));
                });
            }; 
            var parent = testkit.Sys.ActorOf(Props.Create(() => new Act(juanito)));
            parent.Tell("start");
            await agnagna.Task;
             
            return actors.Where(x=>x.Item2!=0).ToList();
        }


        public static void SendKey(RSAParameters key, List<IActorRef> replicas, IActorRef client)
        {
            replicas.ForEach(x => x.Tell(new ClientAndKey(EncryptionManager.GetPublicKey(key),client),client));
        }
        public static void SendPublicKey(RSAParameters key, List<IActorRef> replicas, IActorRef client)
        {
            replicas.ForEach(x => x.Tell(new ClientAndKey(key, client), client));
        }
    }
}