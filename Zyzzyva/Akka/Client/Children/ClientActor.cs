using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Zyzzyva.Akka.Client.Messages;
using Zyzzyva.Akka.Client.Messages.gRPCCreation;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.ZyzzyvaManager.Messages;
using Zyzzyva.Security;
namespace Zyzzyva.Akka.Client.Children
{
    /// <include file="Docs/Akka/Client/Children/ClientActor.xml" path='docs/members[@name="clientactor"]/ClientActor/*'/>
    public class ClientActor : ReceiveActor, IWithUnboundedStash, IWithTimers
    {
        private static readonly int TIME_TO_RESP = 4;
        private static readonly int TIME_TO_RESP2 = 7;
        private static readonly int TIME_TO_LOCALCOMMIT = 3;
        private static readonly string NAME_TIMER = "request-to-primary";
        private (IActorRef, RSAParameters) Primary;
        private int timestamp = 0;
        private int maxFailures;
        private Router replicaRouter;
        private Dictionary<int, (ActorRefRoutee, RSAParameters)> replicas;
        private readonly Dictionary<string, IActorRef> clientgRPC = new();
        private readonly Dictionary<int, ISpecResponse> specResponseDictionary = new();
        private readonly Dictionary<int, LocalCommit> localCommit = new();
        private ISpecResponse localCommitResponse = default;
        private int actualView = 0;
        private RSAParameters privateKey;
        private ImmutableQueue<IRequest> request = ImmutableQueue<IRequest>.Empty;
        ///<inheritdoc/>
        public IStash Stash { get; set; }
        ///<inheritdoc/>
        public ITimerScheduler Timers { get; set; }

        /// <include file="Docs/Akka/Client/Children/ClientActor.xml" path='docs/members[@name="clientactor"]/ClientActorC/*'/>
        public ClientActor()
        {
            privateKey = EncryptionManager.GeneratePrivateKey();

            Receive<ClusterReady>(_ => Sender.Tell(new ClientInitMessage(EncryptionManager.GetPublicKey(privateKey)), Self));
            Receive<ReplicasListMessage>(msg =>
            {
                maxFailures = msg.MaxFailures;
                var primary = msg.Replicas.Find(x => x.Item2 == 0);
                Primary = (primary.Item1, primary.Item3);
                replicas = msg.Replicas.Where(x => x.Item2 != 0).ToDictionary(x => x.Item2, x => (new ActorRefRoutee(x.Item1), x.Item3));
                CreateRouter();
                Stash.UnstashAll();
                Become(ReplicasArrived);
            });
            ReceiveAny(_ => Stash.Stash());
        }


        /// <include file="Docs/Akka/Client/Children/ClientActor.xml" path='docs/members[@name="clientactor"]/MyProps/*'/>
        public static Props MyProps => Props.Create(() => new ClientActor());

        private bool VerifySpec<T>(SpecResponse<T> spec)=> !request.IsEmpty && spec.OrderReqSigned.DigestRequest == DigestManager.GenerateSHA512String(request.Peek().ToString()) &&
            (spec.SpecResponseSigned.View >= actualView && replicas.TryGetValue(spec.ReplicaId, out (ActorRefRoutee, RSAParameters) replica) ? Decrypt(spec.Signature, spec.SpecResponseSigned.ToString(), replica.Item2) :
            Decrypt(spec.Signature, spec.SpecResponseSigned.ToString(), Primary.Item2)) && VerifySignatureOrderReq(spec.OrderReqSigned.View % (3 * maxFailures + 1), spec.OrderReqSignature, spec.OrderReqSigned.ToString()) &&
            spec.OrderReqSigned.SequenceNumber == spec.SpecResponseSigned.SequenceNumber && spec.OrderReqSigned.View == spec.SpecResponseSigned.View;
        
     
        private bool VerifySignatureOrderReq(int replicaId, byte[] signature, string classString) => replicas.TryGetValue(replicaId, out (ActorRefRoutee, RSAParameters) actor) ?
        EncryptionManager.VerifySignature(signature, DigestManager.GenerateSHA512String(classString), actor.Item2) : EncryptionManager.VerifySignature(signature,
        DigestManager.GenerateSHA512String(classString), Primary.Item2);
 
        private bool VerifyLocalCommit(LocalCommit commit) =>!request.IsEmpty && commit.DigestRequest==DigestManager.GenerateSHA512String(request.Peek().ToString()) && 
            replicas.TryGetValue(commit.Id, out (ActorRefRoutee, RSAParameters) replica) ? Decrypt(commit.Signature, commit.ToString(), replica.Item2) : Decrypt(commit.Signature, commit.ToString(), Primary.Item2);
        
        private static bool Decrypt(byte[] signature, string toVerify, RSAParameters key) => EncryptionManager.VerifySignature(signature, DigestManager.GenerateSHA512String(toVerify), key);
        
        private byte[] SignMsg(string toSign) => EncryptionManager.SignMsg(toSign, privateKey);
        
        private void VerifyResultSpecResponse()
        {
            
            if (specResponseDictionary.Count == 3 * maxFailures + 1 && (Timers.IsTimerActive(NAME_TIMER) || Timers.IsTimerActive(timerKey2(NAME_TIMER))))
            {
                CallCheckMisbehaviourAndCheckResponse();
            }
        }

        private static Dictionary<int,ISpecResponse> VerifySpecResponses(Dictionary<int, ISpecResponse> specResponsesG, SpecResponse<IPersonaResponse> msg)
        {
            specResponsesG.TryAdd(msg.ReplicaId,msg);
            return specResponsesG;
        }
        private IRequest CreateRequest<T>(T request)
        {
            var requestFromgRPC = new Request<T>(request, Self, timestamp++);
            var signature = EncryptionManager.SignMsg(requestFromgRPC.ToString(), privateKey);
            requestFromgRPC.Signature = signature; 
            var digestRequest = DigestManager.GenerateSHA512String(requestFromgRPC.ToString());
            clientgRPC.Add(digestRequest, Sender);
            return requestFromgRPC;
        }
        private bool VerifySpecDictionary(RequestTimerEnd1 msg) => specResponseDictionary.Count > 0;
        private void SendRequestToPrimary(IRequest request)
        {
            StartTimersForReq(NAME_TIMER);
            Primary.Item1.Tell(request, Self);
        }
        private void ReplicasArrived()
        {
            DeadReplica();
            Receive<PersonaClient>(msg => msg.Actor.Tell(new CreateClientResponse(Self)));

            Receive<IPersonaRequest>(msg =>
            {
                var requestToPrimary = CreateRequest(msg);
                if (request.IsEmpty)
                {
                    
                    request = request.Enqueue(requestToPrimary);
                    SendRequestToPrimary(requestToPrimary);
                }
                else
                { 
                    request = request.Enqueue(requestToPrimary);
                }
            });
            Receive<SetByzantine>(msg =>
            {
                var replica = msg.Id == actualView % (replicas.Count + 1) ? Primary.Item1 : replicas.TryGetValue(msg.Id, out (ActorRefRoutee, RSAParameters) x) ? x.Item1.Actor : default;
                if (replica != default)
                {
                    replica.Tell(msg, Sender);
                }
            });
            Receive<SpecResponse<IPersonaResponse>>(VerifySpec, msg =>
            {
                
                if (msg.SpecResponseSigned.View > actualView)
                {
                    NewPrimaryAndReplica(actualView % (replicas.Count + 1), msg.SpecResponseSigned.View);
                    CleanSpecResponses(); 
                }
                VerifySpecResponses(specResponseDictionary, msg)
                .ToList().ForEach(value => specResponseDictionary.TryAdd(value.Key,value.Value));
                VerifyResultSpecResponse();
                
            });

            Receive<LocalCommit>(VerifyLocalCommit, msg =>
            { 
                
                localCommit.TryAdd(msg.Id,msg);
                if (localCommit.Count == (2 * maxFailures + 1))
                { 

                    SendResponseToClient(localCommitResponse);
                }  
            });

            Receive<RequestTimerEnd1>(VerifySpecDictionary, _ =>  CallCheckMisbehaviourAndCheckResponse());

            Receive<RequestTimerEnd2>(msg =>
            {
                if (specResponseDictionary.Count < (3 * maxFailures + 1 ) && specResponseDictionary.Count >= (2 * maxFailures +1))
                {
                    CallCheckMisbehaviourAndCheckResponse();
                }
                else
                {
                    StartTimersForReq(NAME_TIMER); 
                    ResendRequest(request.Peek());
                }
            });
            Receive<LocalCommitTimerEnd>(msg =>
            {
                StartTimersForReq(NAME_TIMER);
                ResendRequest(request.Peek());
            });
            ReceiveAny(_ => { });
        }
       
        private void CallCheckMisbehaviourAndCheckResponse()
        {
            var responses = specResponseDictionary.Values.Select(convert).ToList();
            var (countResponse, specResponse, listOfReplica, responseGroups) = Metodino2(responses); 
            if(responseGroups.Count==1 && responseGroups.First().Item1>=(2 * maxFailures + 1))
            {
                CheckResponse(countResponse, specResponse, listOfReplica);
            }
            else
            {
                var responsesCheck = responses.GroupBy(Metodino).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList()))
                .OrderByDescending(specResponse => specResponse.Count).ToList();
                if (responsesCheck.Count == 1)
                {
                    var finalResponse = responses.GroupBy(Metodino5).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList())).OrderByDescending(specResponse => specResponse.Count).ToList();
                    CheckResponse(finalResponse.First().Count, finalResponse.First().Item2.First(), finalResponse.First().Item2.Select(x=>(x.ReplicaId,x.Signature)).ToList());
                }
                else
                {
                    CheckMisbehaviour(responsesCheck); 
                }
            } 
           
        }
 
        private void DeadReplica()
        {
            Receive<ReplicaDead>(msg =>
            {
                replicas.Remove(msg.ActorRef.Item2);
                CreateRouter();
             });
            Receive<ReplicaAdd>(msg =>
            {
                replicas.Remove(msg.ActorRef.Item2);
                replicas.TryAdd(msg.ActorRef.Item2, (new ActorRefRoutee(msg.ActorRef.Item1), msg.ActorRef.Item3));
                CreateRouter(); 
            });
        }

        private void NewPrimaryAndReplica(int actualPrimary, int newView)
        {
            replicas.TryAdd(actualPrimary, (new ActorRefRoutee(Primary.Item1), Primary.Item2));
            var newPrimary = newView % replicas.Count;
            var primary = replicas[newPrimary];
            Primary = (primary.Item1.Actor, primary.Item2);
            replicas.Remove(newPrimary);
            actualView = newView;
            CreateRouter();
        }

        private void SendResponseToClient(ISpecResponse specResponse)
        {
            if (specResponse is SpecResponse<IPersonaResponse> response)
            {
                var responseDictionary= specResponseDictionary.Select(x => convert(x.Value)).GroupBy(Metodino4).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList()))
                .OrderByDescending(specResponse => specResponse.Count).First().Item2.ToDictionary(x => x.ReplicaId, x => x.GetResponse());  
                var grpcResponse = new ReplicaResponse<IPersonaResponse>(responseDictionary, response.GetResponse());
                clientgRPC[response.OrderReqSigned.DigestRequest].Tell(grpcResponse);
                clientgRPC.Remove(response.OrderReqSigned.DigestRequest);
                Timers.Cancel(NAME_TIMER);
                Timers.Cancel(timerKey2(NAME_TIMER));
                Timers.Cancel(timerKey3(NAME_TIMER));
                request = request.Dequeue();
                CleanSpecResponses(); 
                localCommitResponse = default;
                if (!request.IsEmpty)
                {
                    var newRequest = request.Peek();
                    SendRequestToPrimary(newRequest);
                }  
            }

        }

        private void CleanSpecResponses()
        {
            specResponseDictionary.Clear();
            localCommit.Clear();
             
        }

        private void StartTimersForReq(string nameTimer)
        {
            Timers.StartSingleTimer(nameTimer, new RequestTimerEnd1(), TimeSpan.FromSeconds(TIME_TO_RESP));
            Timers.StartSingleTimer(timerKey2(nameTimer), new RequestTimerEnd2(), TimeSpan.FromSeconds(TIME_TO_RESP2));
        }

        private void CheckResponse<T>(int countResponse, SpecResponse<T> response, List<(int, byte[])> listOfReplica)
        {

            if (countResponse >= 3 * maxFailures + 1)
            {
                SendResponseToClient(response);
                
            }
            else if (countResponse >= 2 * maxFailures + 1)
            {
                SendCommitToReplicas(response,listOfReplica);
            }
            else if (!Timers.IsTimerActive(timerKey2(response.OrderReqSigned.DigestRequest)))
            {
                CheckResponse();
            }
        }

        private void CheckResponse()
        {
            StartTimersForReq(NAME_TIMER);
            ResendRequest(request.Peek());
        }

        private void ResendRequest(IRequest request) => replicaRouter.Route(request, Self);

        private readonly Func<ISpecResponse, SpecResponse<IPersonaResponse>> convert = converts => converts as SpecResponse<IPersonaResponse>;
        
        private static IOrderedEnumerable<(int, List<SpecResponse<T>>)> Grouping<T>(List<SpecResponse<T>> lista) => lista.GroupBy(Metodino3).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList()))
                .OrderByDescending(specResponse => specResponse.Count);
 
        private static (int, SpecResponse<T>, List<(int, byte[])>, List<(int, List<SpecResponse<T>>)>) Metodino2<T>(List<SpecResponse<T>> lista)
        {
            var listaR = Grouping(lista);
            var finalResponse = listaR.First();
            return (finalResponse.Item1, finalResponse.Item2.First(), finalResponse.Item2.Select(specResponse => (specResponse.ReplicaId, specResponse.Signature)).ToList(), listaR.ToList());

        } 

        private void SendCommitToReplicas<T>(SpecResponse<T> response,List<(int, byte[])> listOfReplica){
            Timers.Cancel(timerKey2(NAME_TIMER)); //SI STANO SETTANDO DEI TIMER CON DIGUEST REQUEST COMPLETATE
            Timers.StartSingleTimer(timerKey3(NAME_TIMER), new LocalCommitTimerEnd(), TimeSpan.FromSeconds(TIME_TO_LOCALCOMMIT));
            var commitCC = new Commit(Self, new CommitCertificate(listOfReplica, response.SpecResponseSigned));
            commitCC.Signature = EncryptionManager.SignMsg(commitCC.ToString(), privateKey);
            localCommitResponse = response;
            replicaRouter.Route(commitCC, Self);
            Primary.Item1.Tell(commitCC, Self);
        }

        private void SendProofOfMisbehaviour<T>(List<(int, List<SpecResponse<T>>)> specGroup){
                var (head, tail) = specGroup.Take(2).Select(x => x.Item2.First()).ToList();
                var proof = new ProofOfMisbehaviour(head.OrderReqSigned.View, (new OrderReq(head.OrderReqSigned, head.Signature), new OrderReq(tail.First().OrderReqSigned, tail.First().Signature)));
                proof.Signature = SignMsg(proof.ToString());
                replicaRouter.Route(proof, Self);
        }

        private void CheckMisbehaviour<T>(List<(int, List<SpecResponse<T>>)> specGroup)
        {
            if(specGroup.Count > 1 )
            {
                if(specGroup.First().Item1>=(2*maxFailures+1))
                {
                    var result = specGroup.First().Item2;
                    SendCommitToReplicas(result.First(), result.Select(x => (x.ReplicaId, x.Signature)).ToList());
                    SendProofOfMisbehaviour(specGroup); 
                }  
                else
                {

                    var head = specGroup.First().Item2.First();
                    var tail = specGroup.Skip(1).First().Item2.First();
                    var proof = new ProofOfMisbehaviour(head.OrderReqSigned.View, (new OrderReq(head.OrderReqSigned, head.Signature), new OrderReq(tail.OrderReqSigned, tail.Signature)));
                    proof.Signature = SignMsg(proof.ToString());
                    replicaRouter.Route(proof, Self); 
                     
                }
            }
        } 
       
        private static List<OrderReq> CreateListMisbehaviour<T>(List<IGrouping<(int, string, int), SpecResponse<T>>> lists)
        {
            static List<OrderReq> _CreateListMisbehaviour(List<IGrouping<(int, string, int), SpecResponse<T>>> lists, List<OrderReq> newList) => lists switch
            {
                (IGrouping<(int, string, int), SpecResponse<T>> head, List<IGrouping<(int, string, int), SpecResponse<T>>> tail) when newList.Count < 2 => _CreateListMisbehaviour(tail, SpecResponses(newList, head.First())),
                _ => newList
            };
            return _CreateListMisbehaviour(lists, new());
        }

        private static List<OrderReq> SpecResponses<T>(List<OrderReq> list, SpecResponse<T> specResponse)
        {
            list.Add(new OrderReq(specResponse.OrderReqSigned, specResponse.OrderReqSignature));
            return list;
        }
 
        
        private readonly Func<string, string> timerKey2 = digestRequest => $"{digestRequest}-{digestRequest}";
        
        private readonly Func<string, string> timerKey3 = digestRequest => $"{digestRequest}-{digestRequest}--{digestRequest}";
        
        private static (int, int, int, int, string, string, IActorRef, string, int, string) Metodino3<T>(SpecResponse<T> arg) =>         
            (arg.SpecResponseSigned.View, arg.OrderReqSigned.View, arg.SpecResponseSigned.SequenceNumber, arg.OrderReqSigned.SequenceNumber, arg.OrderReqSigned.DigestHistory, arg.SpecResponseSigned.History.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, 
            arg.SpecResponseSigned.Client, arg.OrderReqSigned.DigestRequest, arg.SpecResponseSigned.Timestamp, arg.SpecResponseSigned.DigestResponse); 
        
        private static Type Metodino4<T>(SpecResponse<T> arg) => arg.GetResponse().GetType();

        private static (int, string) Metodino<T>(SpecResponse<T> arg) => (arg.OrderReqSigned.SequenceNumber, arg.OrderReqSigned.DigestHistory);
        private static string Metodino5<T>(SpecResponse<T> arg) => arg.SpecResponseSigned.DigestResponse;

        private void CreateRouter() => replicaRouter = new Router(new BroadcastRoutingLogic(), replicas.Select(x => x.Value.Item1).ToArray());
    }
}
