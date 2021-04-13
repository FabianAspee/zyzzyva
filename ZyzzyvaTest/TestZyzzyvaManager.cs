using Xunit;  
using Akka.Actor; 
using Zyzzyva.Akka.ZyzzyvaManager.Messages;
using System.Security.Cryptography;
using System; 
using Xunit.Abstractions;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Client;
using Zyzzyva.Akka.Client.Messages;
using Zyzzyva.Akka.Client.Messages.gRPCCreation;
using Zyzzyva.Security;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Akka.TestKit.Xunit2;
using System.Threading.Tasks;  
using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.Response;
using Zyzzyva.Database.Tables;

namespace ZyzzyvaTest 
{
    public class TestZyzzyvaManager : TestKit, IDisposable
    { 
        private int replicas;
        private int maxFailures;
        private int idPersona;
        private static readonly string CLIENT_NAME = "client_manager";
        private static readonly string MANAGER_NAME = "zyzzyva_manager"; 
        private readonly bool _ = SetDirectory();
        private int numberOfRequests;

       
        public TestZyzzyvaManager(ITestOutputHelper output) : base(File.ReadAllText("ActorHocon.hocon"))
        {  
            Directory.SetCurrentDirectory($"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}..\\Zyzzyva");
        }

        public void Dispose()
        {
            Enumerable.Range(0, replicas).Where(x=> Directory.Exists($"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}..\\Zyzzyva\\Database{x}")).ToList().ForEach(x=>Directory.Delete($"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}..\\Zyzzyva\\Database{x}",true));
        }
    
        [Fact]
        public void CostructReplicasTest()
        {
            replicas = 4;
            var manager = this.CreateTestProbe();
            ClusterConstructor.ConstructReplicas(this, replicas,0).ForEach(x => x.Tell(new ClusterReady(), manager)); 
            manager.ReceiveN(replicas); 
             
        }
        [Fact]
        public async void SendRequestToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey,replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor);
          
            ExpectMsg<SpecResponse<IPersonaResponse>>(); 
            ExpectMsg<SpecResponse<IPersonaResponse>>();
            ExpectMsg<SpecResponse<IPersonaResponse>>(); 
            ExpectMsg<SpecResponse<IPersonaResponse>>(); 
            ExpectNoMsg();
        } 

        [Fact]
        public async void SendRequestToPrimaryWithAllReplicasEqualsTest()
        {
            replicas = 4;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor);
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,4);
            Assert.True(resps.Count==4 && Grouping(resps)==4); 
        } 
        [Fact]
        public async void SendRequestToPrimaryWithoutOneReplicaTest()
        {
            replicas = 3;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,1);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor); 
            
            ExpectMsg<SpecResponse<IPersonaResponse>>(); 
            ExpectMsg<SpecResponse<IPersonaResponse>>();
            ExpectMsg<SpecResponse<IPersonaResponse>>(); 
            ExpectNoMsg();

        }

        [Fact]
        public async void CommitToReplicasTest()
        {
            replicas = 3;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor); 
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,3).ToList();
            List<(int,byte[])> listOfReplica = new();
            resps.Select(x => (x.ReplicaId,x.Signature)).ToList().ForEach(x => listOfReplica.Add(x));
            var commitCC = new Commit(TestActor, new CommitCertificate(listOfReplica, resps.First().SpecResponseSigned));
            commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateKey);

            replicasR.ForEach(x => x.Item1.Tell(commitCC, TestActor));
            ExpectMsg<LocalCommit>(); 
            ExpectMsg<LocalCommit>();
            ExpectMsg<LocalCommit>();
            ExpectNoMsg();
        }

        [Fact]
        public async void ResendRequestToPrimaryToGetSameCachedResponseTest()
        {
            replicas = 4;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor);
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,4);
            replicasR.Find(x => x.Item2 ==1).Item1.Tell(request, TestActor);
            var cachedResponse = ExpectMsg<SpecResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            ExpectNoMsg();
            Assert.Equal(resps.First(x => x.ReplicaId == cachedResponse.ReplicaId), cachedResponse); 
        }

        [Fact]
        public async void ResendFirstRequestAfterSendingTwoToGetSecondCachedResponseWithModifiedTimestampTest()
        {
            replicas = 4;
            maxFailures = 1;
            numberOfRequests = 2;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var primary = replicasR.Find(x => x.Item2 == 0);
            List<Request<IPersonaRequest>> reqs = SendRequest(numberOfRequests, primary, privateKey); 
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,8);
            replicasR.Find(x => x.Item2 ==1).Item1.Tell(reqs.First(), TestActor);
            var cachedResponse = ExpectMsg<SpecResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            ExpectNoMsg(); 
            Assert.Equal(resps.First(x => x.ReplicaId == cachedResponse.ReplicaId && x.SpecResponseSigned.Timestamp == 1), cachedResponse); 
        } 

        [Fact]
        public async void ResendFirstRequestAfterSendingTwoToGetSecondCachedResponseTest()
        {
            replicas = 4;
            maxFailures = 1;
            numberOfRequests = 2;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var primary = replicasR.Find(x => x.Item2 == 0);
            List<Request<IPersonaRequest>> reqs = SendRequest(numberOfRequests, primary, privateKey); 
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,8);
            replicasR.Find(x => x.Item2 ==1).Item1.Tell(reqs.First(), TestActor);
            var cachedResponse = ExpectMsg<SpecResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            ExpectNoMsg();
            var comparer = new CompareForCache();
            Assert.Equal(resps.First(x => x.ReplicaId == cachedResponse.ReplicaId && x.SpecResponseSigned.Timestamp == 1), cachedResponse, comparer); 
        } 

        [Fact]
        public async void ClientResendARequestToAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(), TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey);
            replicasR.Where(x => x.Item2 != 0).ToList().ForEach(x => x.Item1.Tell(request, TestActor));
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>), 4);
            Assert.True(resps.Count == 4 && Grouping(resps) == 4);

        }

        [Fact]
        public async void ClientSendRequestToWrongPrimaryTest()
        {
            replicas = 4;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 1);
            primary.Item1.Tell(request, TestActor);
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,4);
            Assert.True(resps.Count == 4 && Grouping(resps) == 4);
        }


        [Fact]
        public async void CommitToReplicas2Test()
        {
            replicas = 3;
            maxFailures = 1;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(),TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(),TestActor,0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(),privateKey);
            var primary = replicasR.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor); 
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>) ,3).ToList();
             
            List<(int,byte[])> listOfReplica = new();
            resps.Select(x => (x.ReplicaId,x.Signature)).ToList().ForEach(x => listOfReplica.Add(x));
            var commitCC = new Commit(TestActor, new CommitCertificate(listOfReplica, resps.First().SpecResponseSigned));
            commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateKey);

            replicasR.ForEach(x => x.Item1.Tell(commitCC, TestActor));
            var localcommit = ReceiveWhile(TimeSpan.FromSeconds(10),x=>x as LocalCommit,3);
            Assert.True(localcommit.Count==3 && GroupingLocalCommit(localcommit)==3); 
            
        }
        [Fact]
        public async void RequestToClientAllReplicasCorrectTest()
        {
            replicas = 4;
            maxFailures = 1;
            var client = await ClusterConstructor.ConstructClient(this,replicas,maxFailures);
            var request = new ReadAllPersona();
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4);
        }

        [Fact]
        public async void RequestToClientWithoutOneReplicaTest()
        {
            replicas = 3;
            maxFailures = 1;
            var client = await ClusterConstructor.ConstructClient(this,replicas,maxFailures);
            var request = new ReadAllPersona();
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 3);
        }

       
        [Fact]
        public async void CheckpointWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            numberOfRequests = 51;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures); 
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(), TestActor);
            var primary = replicasR.Find(x => x.Item2 == 0);
            SendRequest(numberOfRequests, primary, privateKey);
            
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 51);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey); 
            primary.Item1.Tell(request, TestActor);
            var result = ReceiveN(208,TimeSpan.FromSeconds(30));
            Assert.True(result.Select(x=>x as SpecResponse<IPersonaResponse>).ToList().Exists(x=>x.SpecResponseSigned.Timestamp== 51 && x.SpecResponseSigned.History.Count==2
            && x.SpecResponseSigned.History.First().OrderReqSigned.SequenceNumber == 50));
        }

        [Fact]
        public async void CheckpointWithoutOneReplicaTest()
        {
          
            replicas = 3;
            maxFailures = 1;
            numberOfRequests = 51;
            var replicasR = await ClusterConstructor.ConstructAllReplicas(this, replicas, maxFailures);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(), TestActor);
            var primary = replicasR.Find(x => x.Item2 == 0);
            SendRequest(numberOfRequests, primary, privateKey);
             
            var result = ReceiveN(153, TimeSpan.FromSeconds(30));

            List<(int, byte[])> listOfReplica = new();
            var resp=result.Select(x => (x as SpecResponse<IPersonaResponse>)).GroupBy(x => x.OrderReqSigned.SequenceNumber)
                .OrderByDescending(x => x.Key);

            resp.First().Select(x => (x.ReplicaId, x.Signature)).ToList().ForEach(x => listOfReplica.Add(x));

            var commitCC = new Commit(TestActor, new CommitCertificate(listOfReplica, resp.First().First().SpecResponseSigned));
            commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateKey);

            replicasR.ForEach(x => x.Item1.Tell(commitCC, TestActor));  
            var localcommit = ReceiveWhile(TimeSpan.FromSeconds(10), x => x as LocalCommit, 3);

            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 51);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey); 
            primary.Item1.Tell(request, TestActor);
            var resultF =  ReceiveWhile(TimeSpan.FromSeconds(10), x => x as SpecResponse<IPersonaResponse>, 3);
            Assert.True(resultF.ToList().Exists(x => x.SpecResponseSigned.Timestamp == 51 && x.SpecResponseSigned.History.Count == 2
              && x.SpecResponseSigned.History.First().OrderReqSigned.SequenceNumber == 50)); 

        }

        [Fact]
        public async void FillHoleReplicasTest()
        {
            replicas = 4;
            maxFailures = 1; 
            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var notInitialized = replicaActors.Last();
            replicaActors.RemoveAt(replicaActors.Count-1);
            var newReplicaTask = new TaskCompletionSource<(IActorRef, int, RSAParameters)>();
            var (replicaInitialized, manager) = await ClusterConstructor.InitializeNReplicas(this, replicaActors, newReplicaTask);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey);
            var primary = replicaInitialized.Find(x => x.Item2 == 0);
            primary.Item1.Tell(request, TestActor);
            var result = ReceiveN(3, TimeSpan.FromSeconds(30));        

            manager.Tell(notInitialized);

            await newReplicaTask.Task.ContinueWith(x=>
            { 
               ClusterConstructor.SendKey(privateKey, new List<IActorRef>() { notInitialized }, TestActor);
               replicaInitialized.Add(x.Result);
            }); 
             
            var request2 = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 1);
            request2.Signature = EncryptionManager.SignMsg(request2.ToString(), privateKey);
            
            primary.Item1.Tell(request2, TestActor);
            var Finalresult = ReceiveN(5, TimeSpan.FromSeconds(30));
            Assert.True(Finalresult.Select(x=>(x as SpecResponse<IPersonaResponse>)).Where(x=>x.ReplicaId==replicaInitialized.Last().Item2).ToList().Exists(x => x.SpecResponseSigned.Timestamp == 1 && x.SpecResponseSigned.History.Count == 2
              && x.SpecResponseSigned.History.First().OrderReqSigned.SequenceNumber == 0));
        }

        [Fact]
        public async void SendRequestReadToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 0;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new ReadPersona(idPersona);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.readPersona) 
                && response.ReplicaResponses.Values.All(x=>x.Equals(TestResponses.readPersona)));
        } 

        [Fact]
        public async void SendRequestReadWhenPersonNotExistToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 1;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new ReadPersona(idPersona);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.readPersonaNotExistId1)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.readPersonaNotExistId1)));
        }

        [Fact]
        public async void SendRequestReadAllToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1; 
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new ReadAllPersona();
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.readAllPersona)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.readAllPersona)));
        }

        [Fact]
        public async void SendRequestInsertToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new InsertPersona(TestResponses.persona);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10)); 
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.insertPersona)
                 && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.insertPersona)));
        }

        [Fact]
        public async void SendRequestInsertIdExistToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new InsertPersona(TestResponses.personaInsertId0);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.insertPersona)
                 && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.insertPersona)));
        }
        [Fact]
        public async void SendRequestUpdateToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new UpdatePersona(TestResponses.personaUpdateId0);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.updatePersona)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.updatePersona)));
        }

        [Fact]
        public async void SendRequestDeleteToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 0;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new DeletePersona(idPersona);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.deletePersona)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.deletePersona)));
        }
        [Fact]
        public async void SendRequestDeleteWhenPersonNotExistToPrimaryWithAllReplicasTest()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 1;
            var client = await ClusterConstructor.ConstructClient(this, replicas, maxFailures);
            var request = new DeletePersona(idPersona);
            client.Tell(request, TestActor);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.deletePersonaNotExist)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.deletePersonaNotExist)));
        }
        [Fact]
        public async void SendRequestClientWithTimerEnd2Test()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 1;
            var (client, replicasR, publicKey) = await ClusterConstructor.ConstructClientWithPrimaryWithoutKeyOfClient(this, replicas, maxFailures);
            var request = new ReadAllPersona();
            client.Tell(request, TestActor);
            await Task.Delay(6000);
            ClusterConstructor.SendPublicKey(publicKey, replicasR.Select(x=>x.Item1).ToList(), client);
            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(40));
            Assert.True(response.ReplicaResponses.Count == 4 && response.Response.Equals(TestResponses.readAllPersona)
                && response.ReplicaResponses.Values.All(x => x.Equals(TestResponses.readAllPersona)));
        }

        [Fact]
        public async void ProofOfMisbehaviourCausesViewchangeTest()
        {
            replicas = 3;
            maxFailures = 1;
            numberOfRequests = 2;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors = ClusterConstructor.ConstructReplicas(this, replicas, 0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var primaryHistory = new List<OrderReq>();

            Enumerable.Range(0, numberOfRequests).ToList().ForEach(x =>
            {
                var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
                request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
                var orderReqSigned = new OrderReqSigned(0, x, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
                var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
                primaryHistory.Add(orderReq);
                replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            }); 
            
            var resps = ReceiveN(numberOfRequests*replicas, TimeSpan.FromSeconds(20)).Select(x => x as SpecResponse<IPersonaResponse>).ToList();  

            var orderReq = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary, 1); 
            var orderReq2 = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary, 2);

            var proof = new ProofOfMisbehaviour(orderReq.OrderReqSigned.View, (orderReq, orderReq2)); proof.Signature = EncryptionManager.SignMsg(proof.ToString(), privateClient);
            replicaInitialized.Where(x => x.Item2 != proof.View).ToList().ForEach(x => x.Item1.Tell(proof, TestActor));
            var newView = await taskFinal.Task; 

            Assert.True(newView.First().View == 1); 
            
        }

        [Fact]
        public async void ViewchangeAfterFailedFillHoleTest()
        {
            replicas = 3;
            maxFailures = 1;

            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
            var orderReqSigned = new OrderReqSigned(0, 1, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
            var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
            replicaInitialized.ToList().ForEach(x => x.Item1.Tell(orderReq, primary));

            primary.ReceiveWhile(TimeSpan.FromSeconds(10), x => x as FillHole, 3); 
            primary.ReceiveWhile(TimeSpan.FromSeconds(30), x => x as ViewChangeCommit, 3);
            await taskFinal.Task;
            replicaInitialized.Find(x => x.Item2 == 1).Item1.Tell(request, TestActor);

            var Finalresult = ReceiveWhile(TimeSpan.FromSeconds(30), x => x as SpecResponse<IPersonaResponse>, 3);
            Assert.True(Finalresult.All(x => x.SpecResponseSigned.View == 1) && Finalresult.Count==3);
        }

        [Fact]
        public async void ViewchangeAfterFailedFillHoleConfirmReqTest()
        {
            replicas = 3;
            maxFailures = 1;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
            var orderReqSigned = new OrderReqSigned(0, 1, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
            var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
            replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));

            primary.ReceiveWhile(TimeSpan.FromSeconds(10), x => x as FillHole, 3);
            primary.ReceiveWhile(TimeSpan.FromSeconds(30), x => x as ViewChangeCommit, 3);
            await taskFinal.Task;
            var confirmReq = new ConfirmReq<IRequest>(1, request, 0);
            confirmReq.Signature = EncryptionManager.SignMsg(confirmReq.ToString(), privatePrimary);
            replicaInitialized.Find(x => x.Item2 == 1).Item1.Tell(confirmReq, primary);
            primary.ExpectMsg<NewView>();
            var Finalresult = primary.ExpectMsg<OrderReq<IPersonaRequest>>();
            Assert.True( EncryptionManager.VerifySignature(Finalresult.Signature, DigestManager.GenerateSHA512String(Finalresult.OrderReqSigned.ToString()), replicaInitialized.Find(x => x.Item2 == 1).Item3));
        }

        [Fact]
        public async void ViewchangeAfterFailedFillHoleConfirmReqToNonPrimaryTest()
        {
            replicas = 3;
            maxFailures = 1;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 0);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
            var orderReqSigned = new OrderReqSigned(0, 1, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
            var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
            replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));

            primary.ReceiveWhile(TimeSpan.FromSeconds(10), x => x as FillHole, 3);
            primary.ReceiveWhile(TimeSpan.FromSeconds(30), x => x as ViewChangeCommit, 3);
            await taskFinal.Task;
            var confirmReq = new ConfirmReq<IRequest>(1, request, 0);
            confirmReq.Signature = EncryptionManager.SignMsg(confirmReq.ToString(), privatePrimary);
            replicaInitialized.Find(x => x.Item2 == 2).Item1.Tell(confirmReq, primary);
            primary.ExpectMsg<NewView>();
            var Finalresult = primary.ExpectMsg<OrderReq<IPersonaRequest>>();
            Assert.True(EncryptionManager.VerifySignature(Finalresult.Signature, DigestManager.GenerateSHA512String(Finalresult.OrderReqSigned.ToString()), replicaInitialized.Find(x => x.Item2 == 1).Item3));
        }

        [Fact]
        public async void ReplicaGetsFillHoleFromOtherReplicasWhenPrimaryDontSendItTest()
        {
            replicas = 3;
            maxFailures = 1;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var primaryHistory = new List<OrderReq>();
            
            Enumerable.Range(0, 5).ToList().ForEach(x => 
            {
                var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
                request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
                var orderReqSigned = new OrderReqSigned(0, x, DigestManager.GenerateSHA512String(request.ToString()), 
                    DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
                var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
                primaryHistory.Add(orderReq);
                replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            });

            Enumerable.Range(5, 4).ToList().ForEach(x => 
            {
                var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
                request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
                var orderReqSigned = new OrderReqSigned(0, x, DigestManager.GenerateSHA512String(request.ToString()),
                    DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
                var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
                primaryHistory.Add(orderReq);
                replicaInitialized.Where(x => x.Item2 != 1).ToList().ForEach(x => x.Item1.Tell(orderReq, primary));
            });

            ReceiveN(23, TimeSpan.FromSeconds(20));
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, 9);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
            var orderReqSigned = new OrderReqSigned(0, 9, DigestManager.GenerateSHA512String(request.ToString()),
                    DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
            var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
            replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            
            var Finalresult = ReceiveWhile(TimeSpan.FromSeconds(30), x => x as SpecResponse<IPersonaResponse>, 7);
            var historyOfFirstReplicaLastTimestamp = Finalresult.
                    First(x => x.ReplicaId == 1 && x.SpecResponseSigned.Timestamp == 9).SpecResponseSigned.History;
            Assert.True(Finalresult.Count==7 && Finalresult.Where(x => x.ReplicaId == 1).Count() == 5 && 
            Finalresult.Where(x => x.SpecResponseSigned.Timestamp == 9 && x.ReplicaId != 1)
                .All(x => x.SpecResponseSigned.History.SequenceEqual(historyOfFirstReplicaLastTimestamp)));
        }


        [Fact]
        public async void CorrectViewchangeWhenOneReplicasHistoryIsBehindTheOthersTest()
        {
            replicas = 3;
            maxFailures = 1;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var primaryHistory = new List<OrderReq>();
            
            Enumerable.Range(0, 5).ToList().ForEach(x => 
            {
                var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
                request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
                var orderReqSigned = new OrderReqSigned(0, x, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
                var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
                primaryHistory.Add(orderReq);
                replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            });

            Enumerable.Range(5, 4).ToList().ForEach(x => 
            {
                var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
                request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
                var orderReqSigned = new OrderReqSigned(0, x, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
                var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
                primaryHistory.Add(orderReq);
                replicaInitialized.Where(x => x.Item2 != 1).ToList().ForEach(x => x.Item1.Tell(orderReq, primary));
            });

            var resps = ReceiveN(23, TimeSpan.FromSeconds(20)).Select(x => x as SpecResponse<IPersonaResponse>).ToList();
            
            var req = primaryHistory.First() as OrderReq<IPersonaRequest>;
            replicaInitialized.First().Item1.Tell(req.Request, TestActor);
            var cachedResponse = ExpectMsg<SpecResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            ExpectNoMsg();
            var orderReq = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary, 1);

            var orderReq2 = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary, 2);

            var proof = new ProofOfMisbehaviour(orderReq.OrderReqSigned.View, (orderReq, orderReq2)); proof.Signature= EncryptionManager.SignMsg(proof.ToString(), privateClient);
            replicaInitialized.Where(x => x.Item2 != proof.View).ToList().ForEach(x=>x.Item1.Tell(proof, TestActor));           
            var newView = await taskFinal.Task;
            var reconciliationSpecs = ReceiveN(4, TimeSpan.FromSeconds(10)).Select(x => x as SpecResponse<IPersonaResponse>);
            Assert.True(reconciliationSpecs.All(x => x.SpecResponseSigned.View == newView.First().View));
        }
        
        [Fact]
        public async void CorrectViewchangeWhenOneReplicasHistoryIsBehindTheOthersAfterCheckpointTest()
        {
            replicas = 6;
            maxFailures = 2;
            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey();
            var privateClient = EncryptionManager.GeneratePrivateKey();

            var replicaActors  = ClusterConstructor.ConstructReplicas(this, replicas,0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            ClusterConstructor.SendKey(privateClient, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);
            var primaryHistory = new List<OrderReq>();
            numberOfRequests = 51;
            Enumerable.Range(0, numberOfRequests).ToList().ForEach(x => 
            {   
                var orderReq = CreateOrderReq(x, privateClient, primaryHistory, privatePrimary,x);
                primaryHistory.Add(orderReq);
                replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            });
            var replies = ReceiveN(numberOfRequests * replicas, TimeSpan.FromSeconds(40)).Select(x => x as SpecResponse<IPersonaResponse>).ToList();
            
            var resps = replies.Where(x => x.SpecResponseSigned.SequenceNumber == 50);
            List<(int,byte[])> listOfReplica = new();
            resps.Select(x => (x.ReplicaId,x.Signature)).ToList().ForEach(x => listOfReplica.Add(x));
            var commitCC = new Commit(TestActor, new CommitCertificate(listOfReplica, resps.First().SpecResponseSigned));
            commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateClient);

            replicaInitialized.ForEach(x => x.Item1.Tell(commitCC, TestActor));
            primaryHistory = primaryHistory.Where(x => x.OrderReqSigned.SequenceNumber == 50).ToList();
            ReceiveN(replicas,TimeSpan.FromSeconds(30));
            Enumerable.Range(numberOfRequests, 4).ToList().ForEach(x => 
            { 
                var orderReq = CreateOrderReq(x, privateClient, primaryHistory, privatePrimary,x);
                primaryHistory.Add(orderReq);
                replicaInitialized.Where(x => x.Item2 != 1).ToList().ForEach(x => x.Item1.Tell(orderReq, primary));
            }); 
            var respsaftercheck = ReceiveN(20, TimeSpan.FromSeconds(30)).Select(x => x as SpecResponse<IPersonaResponse>).ToList();

            
            respsaftercheck.GroupBy(x => x.SpecResponseSigned.Timestamp).OrderBy(x => x.Key).ToList()
                .ForEach(x => {
                    var replicas = x.Select(xx => (xx.ReplicaId, xx.Signature)).ToList();
                    var commitCC = new Commit(TestActor, new CommitCertificate(replicas, x.First().SpecResponseSigned));
                    commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateClient); 
                    replicaInitialized.Where(x => x.Item2 != 1).ToList().ForEach(x => x.Item1.Tell(commitCC, TestActor)); 
                    ReceiveN( 5, TimeSpan.FromSeconds(20));
                });



            var orderReq = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary,1);

            var orderReq2 = CreateOrderReq(55, privateClient, primaryHistory, privatePrimary,2);
             
            var proof = new ProofOfMisbehaviour(orderReq.OrderReqSigned.View, (orderReq, orderReq2));
            proof.Signature= EncryptionManager.SignMsg(proof.ToString(), privateClient); 
            replicaInitialized.Where(x => x.Item2 != proof.View).Reverse().ToList().ForEach(x=>x.Item1.Tell(proof, TestActor));   
            var newView = await taskFinal.Task; 
            var reconciliationSpecs = ReceiveN(1, TimeSpan.FromSeconds(30)).Select(x => x as SpecResponse<IPersonaResponse>);
            Assert.True(newView.First().View == 1 && reconciliationSpecs.All(x => x.ReplicaId == 1) && reconciliationSpecs.All(x=>x.SpecResponseSigned.View==newView.First().View));
         }
        
        private OrderReq CreateOrderReq(int timestamp, RSAParameters privateClient, List<OrderReq> primaryHistory, RSAParameters privatePrimary,int sequenceNumber)
        {
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, timestamp);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateClient);
            var orderReqSigned = new OrderReqSigned(0, sequenceNumber, DigestManager.GenerateSHA512String(request.ToString()), DigestManager.DigestList(primaryHistory.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(request.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privatePrimary);
            var orderReq = new OrderReq<IPersonaRequest>(orderReqSigned, request, signature);
            return orderReq;
        }
        [Fact]
        public async void CorrectNewHistoryAndReplyAfterViewchangeBeforeCheckpointTest()
        {
            replicas = 4;
            maxFailures = 1;
            numberOfRequests = 4;

            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();

            var primary = CreateTestProbe();
            var privatePrimary = EncryptionManager.GeneratePrivateKey(); 
           
            var replicaActors = ClusterConstructor.ConstructReplicas(this, replicas, 0);
            var replicaInitialized = await ClusterConstructor.InitializeNReplicasNoPrimary(this, replicaActors, (primary, 0, EncryptionManager.GetPublicKey(privatePrimary)), taskFinal, maxFailures);
            var newPrimary = replicaInitialized.Find(x=>x.Item2==1);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicaInitialized.Select(x => x.Item1).ToList(), TestActor);


            var primaryHistory = new List<OrderReq>(); 
            Enumerable.Range(0, numberOfRequests).ToList().ForEach(x =>
            {
                var orderReq = CreateOrderReq(x, privateKey, primaryHistory, privatePrimary, x);
                primaryHistory.Add(orderReq);
                replicaInitialized.ForEach(x => x.Item1.Tell(orderReq, primary));
            });

              
            var resps = ReceiveWhile(TimeSpan.FromSeconds(10), x => (x as SpecResponse<IPersonaResponse>), numberOfRequests * replicas).ToList();
            

            var orderReq = CreateOrderReq(55, privateKey, resps.First().SpecResponseSigned.History, privatePrimary, 1);

            var orderReq2 = CreateOrderReq(55, privateKey, resps.First().SpecResponseSigned.History, privatePrimary, 2);
            var proof = new ProofOfMisbehaviour(orderReq.OrderReqSigned.View, (orderReq, orderReq2));
            proof.Signature = EncryptionManager.SignMsg(proof.ToString(), privateKey);
            replicaInitialized.Where(x => x.Item2 != proof.View).Reverse().ToList().ForEach(x => x.Item1.Tell(proof, TestActor));
            replicaInitialized.Where(x => x.Item2 != proof.View).ToList().ForEach(x=>x.Item1.Tell(proof, TestActor));           
            var newView = await taskFinal.Task;
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, numberOfRequests);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey);

            newPrimary.Item1.Tell(request, TestActor);


            primary.FishForMessage(x=>x is OrderReq<IPersonaRequest>);

            var responseVC = ReceiveN(3, TimeSpan.FromSeconds(10)).Select(x => x as SpecResponse<IPersonaResponse>).ToList();

            Assert.True(responseVC.All(x => x.SpecResponseSigned.View  == newView.First().View) && 
                        responseVC.All(x => x.SpecResponseSigned.History.All(xx => xx.OrderReqSigned.View == newView.First().View)) &&
                        responseVC.All(x => x.SpecResponseSigned.History.SkipLast(1).SequenceEqual(resps.First(x => 
                            x.SpecResponseSigned.Timestamp == numberOfRequests-1).SpecResponseSigned.History,new CompareOrderReq())));
        }

        [Fact]
        public async void ReplyLocalCommitWhenResenOldRequestTest()
        {
            replicas = 4;
            maxFailures = 1;
            numberOfRequests = 53;

            var taskFinal = new TaskCompletionSource<List<FinalNewView>>();
            var (replicasR,manager) = await ClusterConstructor.ConstructAllReplicasWithManager(this, replicas, maxFailures, taskFinal);
            var privateKey = EncryptionManager.GeneratePrivateKey();
            ClusterConstructor.SendKey(privateKey, replicasR.Select(x => x.Item1).ToList(), TestActor);
            var primary = replicasR.Find(x => x.Item2 == 0);
            List<Request<IPersonaRequest>> reqs = SendRequest(numberOfRequests, primary, privateKey);
            var resps = ReceiveWhile(TimeSpan.FromSeconds(40), x => (x as SpecResponse<IPersonaResponse>), numberOfRequests * replicas);
            replicasR.Find(x => x.Item2 == 1).Item1.Tell(reqs.First(), TestActor);
            var localcommit = ExpectMsg<LocalCommit>(TimeSpan.FromSeconds(10));
  
            Assert.Equal(localcommit.DigestRequest, DigestManager.GenerateSHA512String(reqs.First().ToString()));
        }

        [Fact]
        public async void GetClientTest()
        { 
            var clientManager = Sys.ActorOf(ClientManagerActor.MyProps, CLIENT_NAME); 
            var taskRouter = new TaskCompletionSource<IActorRef>();
            ClusterConstructor.ConstructManagerRouter(this, taskRouter, MANAGER_NAME);
            var clientRouter = await taskRouter.Task; 
            clientRouter.Tell(new ClusterReady());
            var clients = ReceiveN(3,TimeSpan.FromSeconds(20)).Select(x=>x as ReplicaInitMessage).ToList();
            var replicas = await ClusterConstructor.ConstructAllReplicas(this, 3, 1);
            clientRouter.Tell(new ReplicasListMessage(replicas.ToImmutableList(),1));
            await Task.Delay(1500);
            clientManager.Tell(new PersonaClient(TestActor));
            ExpectMsg<CreateClientResponse>();
        }

        [Fact]
        public async void ClientResponseAfterViewChangeTest()
        { 
            replicas = 4;
            maxFailures = 1;
            idPersona = 1; 
            var newView = 1;
            var sequenceNumber = 0;
            var history = new List<OrderReq>();
            var historyFaulty = new List<OrderReq>();
            var ((client, publicKeyClient), replicasFalse) = await ClusterConstructor.ConstructClientAndReplicasControl(this, replicas, maxFailures);
            var request = new DeletePersona(idPersona);
            client.Tell(request, TestActor);
            var primary = replicasFalse.First();
            var newprimary = replicasFalse.Skip(1).First();
            var requestPrimary = primary.Item1.ExpectMsg<Request<IPersonaRequest>>();
            var orderReqSigned = new OrderReqSigned(newView, sequenceNumber++, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(string.Empty,DigestManager.GenerateSHA512String(requestPrimary.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), newprimary.Item3);
            var finalrequest = new OrderReq<IPersonaRequest>(orderReqSigned, requestPrimary, signature);
            history.Add(finalrequest);
            var responseReplicasAndPrimary = new DeletePersonaResponse(ImmutableList<Persona>.Empty, true);
            var semiFinalResponse = replicasFalse.Select(x => ConstructSpecResponse(responseReplicasAndPrimary, finalrequest)).ToList();

            var replyToClient = replicasFalse.Zip(semiFinalResponse).Select(specToSign => SignSpecRespone(specToSign.Second, history, specToSign.First.Item3, specToSign.First.Item2)).ToList();

            replicasFalse.Zip(replyToClient).ToList().ForEach(finalResponse => finalResponse.Second.SpecResponseSigned.Client.Tell(finalResponse.Second, finalResponse.First.Item1));

            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10)); 
            Assert.True(response.ReplicaResponses.Count==4 && response.ReplicaResponses.Select(x => x.Value).All(x => x.Equals(response.Response)));
           
        }

        [Fact]
        public async void ClientSendRequestToNewPrimaryAfterViewChange()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 1;
            var newView = 1;
            var sequenceNumber = 0;
            var history = new List<OrderReq>(); 
            var ((client, publicKeyClient), replicasFalse) = await ClusterConstructor.ConstructClientAndReplicasControl(this, replicas, maxFailures);
            var request = new DeletePersona(idPersona);
            client.Tell(request, TestActor);
            var primary = replicasFalse.First();
            var newprimary = replicasFalse.Skip(1).First();
            var requestPrimary = primary.Item1.ExpectMsg<Request<IPersonaRequest>>();
            var orderReqSigned = new OrderReqSigned(newView, sequenceNumber++, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(requestPrimary.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), newprimary.Item3);
            var finalrequest = new OrderReq<IPersonaRequest>(orderReqSigned, requestPrimary, signature);
            history.Add(finalrequest);
            var responseReplicasAndPrimary = new DeletePersonaResponse(ImmutableList<Persona>.Empty, true);
            var semiFinalResponse = replicasFalse.Select(x => ConstructSpecResponse(responseReplicasAndPrimary, finalrequest)).ToList();

            var replyToClient = replicasFalse.Zip(semiFinalResponse).Select(specToSign => SignSpecRespone(specToSign.Second, history, specToSign.First.Item3, specToSign.First.Item2)).ToList();

            replicasFalse.Zip(replyToClient).ToList().ForEach(finalResponse => finalResponse.Second.SpecResponseSigned.Client.Tell(finalResponse.Second, finalResponse.First.Item1));

            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));

            client.Tell(request, TestActor);
            requestPrimary = newprimary.Item1.ExpectMsg<Request<IPersonaRequest>>();
            orderReqSigned = new OrderReqSigned(newView, sequenceNumber, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(history.LastOrDefault().OrderReqSigned.DigestHistory,  DigestManager.GenerateSHA512String(requestPrimary.ToString())));
            signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), newprimary.Item3);
            finalrequest = new OrderReq<IPersonaRequest>(orderReqSigned, requestPrimary, signature);

          
            responseReplicasAndPrimary = new DeletePersonaResponse(ImmutableList<Persona>.Empty, true);
            semiFinalResponse = replicasFalse.Select(x => ConstructSpecResponse(responseReplicasAndPrimary, finalrequest)).ToList();


            replyToClient = replicasFalse.Zip(semiFinalResponse).Select(specToSign => SignSpecRespone(specToSign.Second, history, specToSign.First.Item3, specToSign.First.Item2)).ToList();

            replicasFalse.Zip(replyToClient).ToList().ForEach(finalResponse => finalResponse.Second.SpecResponseSigned.Client.Tell(finalResponse.Second, finalResponse.First.Item1));

            var finalResponse = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));
            Assert.True(finalResponse.ReplicaResponses.Count == 4 && finalResponse.ReplicaResponses.Select(x => x.Value).All(x => x.Equals(finalResponse.Response)));

        }

        [Fact]
        public async void ClientCreateProofMisBehaviourTest()
        {
            replicas = 4;
            maxFailures = 1;
            idPersona = 1;
            var view = 0;
            var sequenceNumber = 0;
            var history = new List<OrderReq>();
            var historyFaulty = new List<OrderReq>();
            var ((client, publicKeyClient), replicasFalse) = await ClusterConstructor.ConstructClientAndReplicasControl(this, replicas, maxFailures);
            var request = new DeletePersona(idPersona);
            client.Tell(request, TestActor);
            var primary = replicasFalse.First();
            var requestPrimary = primary.Item1.ExpectMsg<Request<IPersonaRequest>>();
            var orderReqSigned = new OrderReqSigned(view, sequenceNumber++, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(requestPrimary.ToString())));
            var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), primary.Item3);
            var finalrequest = new OrderReq<IPersonaRequest>(orderReqSigned, requestPrimary, signature);
            history.Add(finalrequest);
            var responseReplicasAndPrimary = new DeletePersonaResponse(ImmutableList<Persona>.Empty, true);
            var semiFinalResponse = replicasFalse.Select(x => ConstructSpecResponse(responseReplicasAndPrimary, finalrequest)).ToList();

            var replyToClient = replicasFalse.Zip(semiFinalResponse).Select(specToSign => SignSpecRespone(specToSign.Second, history, specToSign.First.Item3, specToSign.First.Item2)).ToList();

            replicasFalse.Zip(replyToClient).ToList().ForEach(finalResponse => finalResponse.Second.SpecResponseSigned.Client.Tell(finalResponse.Second, finalResponse.First.Item1));

            var response = ExpectMsg<ReplicaResponse<IPersonaResponse>>(TimeSpan.FromSeconds(10));

            client.Tell(request, TestActor);

            requestPrimary = primary.Item1.ExpectMsg<Request<IPersonaRequest>>();
            orderReqSigned = new OrderReqSigned(view, sequenceNumber, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(history.LastOrDefault().OrderReqSigned.DigestHistory, DigestManager.GenerateSHA512String(requestPrimary.ToString()))); 
            signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), primary.Item3);
            finalrequest = new OrderReq<IPersonaRequest>(orderReqSigned, requestPrimary, signature);

            var orderReqSigned2 = new OrderReqSigned(view, sequenceNumber, DigestManager.GenerateSHA512String(requestPrimary.ToString()), DigestManager.DigestList(string.Empty, DigestManager.GenerateSHA512String(requestPrimary.ToString())));
            var signature2 = EncryptionManager.SignMsg(orderReqSigned2.ToString(), primary.Item3);
            var finalrequest2 = new OrderReq<IPersonaRequest>(orderReqSigned2, requestPrimary, signature2);

            responseReplicasAndPrimary = new DeletePersonaResponse(ImmutableList<Persona>.Empty, true);
            semiFinalResponse = replicasFalse.Select(x => {
                if (x.Item2 != 0)
                {
                    return ConstructSpecResponse(responseReplicasAndPrimary, finalrequest);
                }
                else
                {
                    return ConstructSpecResponse(responseReplicasAndPrimary, finalrequest2);
                }
            }).ToList();


            replyToClient = replicasFalse.Zip(semiFinalResponse).Select(specToSign => SignSpecRespone(specToSign.Second, history, specToSign.First.Item3, specToSign.First.Item2)).ToList();

            replicasFalse.Zip(replyToClient).ToList().ForEach(finalResponse => finalResponse.Second.SpecResponseSigned.Client.Tell(finalResponse.Second, finalResponse.First.Item1));
            replicasFalse.Find(x => x.Item2 == 0).Item1.ExpectMsg<Commit>(TimeSpan.FromSeconds(60));
            replicasFalse.Find(x => x.Item2 == 1).Item1.ExpectMsg<Commit>(TimeSpan.FromSeconds(60));
            replicasFalse.Find(x => x.Item2 == 2).Item1.ExpectMsg<Commit>(TimeSpan.FromSeconds(60));
            replicasFalse.Find(x => x.Item2 == 3).Item1.ExpectMsg<Commit>(TimeSpan.FromSeconds(60));
            primary.Item1.ExpectNoMsg();
            var proof = replicasFalse.Find(x => x.Item2 == 1).Item1.ExpectMsg<ProofOfMisbehaviour>(TimeSpan.FromSeconds(60));
            replicasFalse.Find(x => x.Item2 == 2).Item1.ExpectMsg<ProofOfMisbehaviour>(TimeSpan.FromSeconds(60));
            replicasFalse.Find(x => x.Item2 == 3).Item1.ExpectMsg<ProofOfMisbehaviour>(TimeSpan.FromSeconds(60));
            Assert.NotEqual(proof.spectTuple.Item1, proof.spectTuple.Item2);
        }

        private static SpecResponse<T> SignSpecRespone<T>(SpecResponse<T> specToSign,List<OrderReq> history, RSAParameters privateKey, int myId)
        {
            var specSigned = new SpecResponseSigned(specToSign.SpecResponseSigned, history);
            var signature = EncryptionManager.SignMsg(specSigned.ToString(), privateKey);
            return new SpecResponse<T>(specToSign.GetResponse(), specToSign.OrderReqSigned, specSigned, specToSign.OrderReqSignature, myId, signature);
        }

        private static SpecResponse<IPersonaResponse> ConstructSpecResponse<T>(IPersonaResponse response, OrderReq<T> msg) =>
          new SpecResponse<IPersonaResponse>(response, msg.OrderReqSigned,
               new SpecResponseSigned(msg.OrderReqSigned.View, msg.OrderReqSigned.SequenceNumber,
                   DigestManager.GenerateSHA512String(response.ToString()), msg.Request.Client, msg.Request.Timestamp), msg.Signature);
        
        private List<Request<IPersonaRequest>> SendRequest(int numberOfRequest, (IActorRef, int, RSAParameters) primary, RSAParameters privateKey) => Enumerable.Range(0, numberOfRequest).ToList().Select(x => {
            var request = new Request<IPersonaRequest>(new ReadAllPersona(), TestActor, x);
            request.Signature = EncryptionManager.SignMsg(request.ToString(), privateKey);
            primary.Item1.Tell(request, TestActor);
            return request;
        }).ToList();


        private static int GroupingLocalCommit(IReadOnlyList<LocalCommit> lista) => lista.GroupBy(Metodino4).Select(localCommit => (localCommit.ToList().Count, localCommit.ToList()))
                .OrderByDescending(specResponse => specResponse.Count).First().Count;
        private static (IActorRef, string, int, string) Metodino4(LocalCommit arg) => 
        (arg.Client, arg.DigestRequest, arg.View, arg.History.LastOrDefault().OrderReqSigned.DigestHistory);

        private static int Grouping<T>(IReadOnlyList<SpecResponse<T>> lista) => lista.GroupBy(Metodino3).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList()))
                .OrderByDescending(specResponse => specResponse.Count).First().Count;
        private static (int, int, int, int, string, string, IActorRef, string, int, string) Metodino3<T>(SpecResponse<T> arg) => 
        (arg.SpecResponseSigned.View, arg.OrderReqSigned.View, arg.SpecResponseSigned.SequenceNumber, arg.OrderReqSigned.SequenceNumber, arg.OrderReqSigned.DigestHistory, arg.SpecResponseSigned.History.LastOrDefault().OrderReqSigned.DigestHistory, 
        arg.SpecResponseSigned.Client, arg.OrderReqSigned.DigestRequest, arg.SpecResponseSigned.Timestamp, arg.SpecResponseSigned.DigestResponse);        

       
        private class CompareOrderReq : IEqualityComparer<OrderReq>
        {
            public bool Equals(OrderReq compared, OrderReq toCompare)
            {
                return compared.OrderReqSigned.SequenceNumber == toCompare.OrderReqSigned.SequenceNumber  &&
                compared.OrderReqSigned.DigestHistory == toCompare.OrderReqSigned.DigestHistory &&
                compared.OrderReqSigned.DigestRequest == toCompare.OrderReqSigned.DigestRequest;
            }

            public int GetHashCode(OrderReq compared)
            {
                return 1;
            }
        }

        private class ComparePersonaResponse : IEqualityComparer<IPersonaResponse>
        {
            public bool Equals(IPersonaResponse compared, IPersonaResponse toCompare)
            { 
                return compared.Equals(toCompare);
            }

            public int GetHashCode(IPersonaResponse compared)
            {
                return 1;
            }
        }
        private class CompareForCache : IEqualityComparer<SpecResponse<IPersonaResponse>>
        {
            public bool Equals(SpecResponse<IPersonaResponse> compared, SpecResponse<IPersonaResponse> toCompare)
            {
                return compared.OrderReqSigned.Equals(toCompare.OrderReqSigned) &&
                    compared.ReplicaId == toCompare.ReplicaId &&
                    compared.SpecResponseSigned.Client.Equals(toCompare.SpecResponseSigned.Client) &&
                    compared.SpecResponseSigned.DigestResponse.Equals(toCompare.SpecResponseSigned.DigestResponse) &&
                    compared.SpecResponseSigned.History.SequenceEqual(toCompare.SpecResponseSigned.History) &&
                    compared.SpecResponseSigned.SequenceNumber == toCompare.SpecResponseSigned.SequenceNumber &&
                    compared.SpecResponseSigned.View == toCompare.SpecResponseSigned.View;
            }

            public int GetHashCode(SpecResponse<IPersonaResponse> compared)
            {
                return 1;
            }
        }

        private static bool SetDirectory()
        {
            Directory.SetCurrentDirectory($"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}");
            return true;
        }
    }

}