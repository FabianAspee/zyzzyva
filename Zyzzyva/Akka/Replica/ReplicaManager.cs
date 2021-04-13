using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Zyzzyva.Akka.Replica.Children;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.ZyzzyvaManager.Messages;
using Zyzzyva.Security; 

namespace Zyzzyva.Akka.Replica
{
    /// <include file="Docs/Akka/Replica/ReplicaManager.xml" path='docs/members[@name="replicamanager"]/ReplicaManager/*'/>
    public partial class ReplicaManager : ReceiveActor, IWithUnboundedStash, IWithTimers
    {

        private static RSAParameters privateKey;
        private static readonly int CP_INTERVAL = 50;
        private static readonly int TIME_TO_RESP = 6;
        private readonly IActorRef _databaseChildren;
        private IActorRef _zyzzyvaRouter;
        private int countViewChangeTimer = 1;
        private (ActorRefRoutee, RSAParameters) primary;
        private Router replicaRouter;
        private Dictionary<int, (ActorRefRoutee, RSAParameters)> replicas;
        private List<OrderReq> history = new();
        private readonly Dictionary<IActorRef, (ISpecResponse, OrderReqSigned)> cacheResponse = new();
        private readonly Dictionary<string, int> lastClientTimestamp = new();
        private readonly Dictionary<IActorRef, IHateThePrimary> hateDict = new();
        private readonly Dictionary<IActorRef, ViewChange> viewChangeDict = new();
        private int myId;
        private int view = 0;
        private bool viewChangeStatus;
        private int sequenceNumber = 0;
        private int actualPrimary = 0;
        private int maxFailures;
        private CommitCertificate lastCommitCertificate;
        private readonly Dictionary<int, List<Checkpoint>> checkpointDictionaryAccum = new();
        private readonly Dictionary<int, List<SpecResponse<string>>> checkpointDictionarySpec = new();
        private Checkpoint checkpoint;
        private ImmutableQueue<OrderReq> waitingRequests = ImmutableQueue<OrderReq>.Empty;
        private ImmutableQueue<OrderReq> waitingRequestsFillHole = ImmutableQueue<OrderReq>.Empty;
        private NewView newView;
        private readonly List<SnapshotReply> snapshotReplies = new();
        private int max_l;
        private int min_s;
        private Dictionary<IActorRef, RSAParameters> clientKeys = new();
        private readonly HashSet<OrderReq> requestOk = new HashSet<OrderReq>();
        private bool byzantine = false;
        ///<inheritdoc/>
        public IStash Stash { get; set; }
        ///<inheritdoc/>
        public ITimerScheduler Timers { get; set; }

        ///<inheritdoc/>
        protected override void PostStop()
        {
            _zyzzyvaRouter.Tell(new POSTMORTEM(Self.Path.Address.HostPort()));
        }

        /// <include file="Docs/Akka/Replica/ReplicaManager.xml" path='docs/members[@name="replicamanager"]/ReplicaManagerC/*'/>
        public ReplicaManager()
        {
            
            privateKey = EncryptionManager.GeneratePrivateKey();
            _databaseChildren = Context.ActorOf(DatabaseActor.MyProps(), "database");
            Receive<ClusterReady>(_ =>
            {
                _zyzzyvaRouter = Sender;
                _zyzzyvaRouter.Tell(new ReplicaInitMessage(EncryptionManager.GetPublicKey(privateKey)), Self);
            });
            Receive<ReplicaNumberMessage>(msg =>
            {
                myId = msg.Id;
                view = msg.View;
                Stash.UnstashAll();
                Become(WaitForBrothers);
            });

            ReceiveAny(_ => Stash.Stash());
        }

        private void Byzantine()
        {
            Receive<SetByzantine>(msg => msg.Id == myId, msg1 =>
            {
                byzantine = !byzantine;
                Sender.Tell(new SetByzantineResponse(byzantine));
            });
        }

        private void WaitForBrothers()
        {
            Receive<ClientListMessage>(msg =>
            {
                clientKeys = msg.Clients;
                Sender.Tell(new GetAnotherReplicas());
            });
            Receive<ReplicasListMessage>(msg =>
            {
                actualPrimary = view < (3 * maxFailures + 1) ? view : view % (3 * maxFailures + 1);
                maxFailures = msg.MaxFailures;
                replicas = msg.Replicas.Where(actor => actor.Item2 != myId).ToDictionary(x => x.Item2, x => (new ActorRefRoutee(x.Item1), x.Item3));
                replicas.Remove(actualPrimary, out primary);

                CreateRouter();

                Stash.UnstashAll();
                if (myId == actualPrimary)
                {
                    Become(Primary);
                }
                else
                {
                    Become(Replica);
                }
            });

            Receive<IRequest>(_ => Stash.Stash());
        }

        private void Replica()
        {
            viewChangeStatus = false;
            Receive<OrderReq<IPersonaRequest>>(VerifyOrderReq, msg =>
            {
                requestOk.Add(msg);
                var condition = waitingRequests.IsEmpty && waitingRequestsFillHole.IsEmpty;
                if (condition)
                {
                    waitingRequests = waitingRequests.Enqueue(msg); 
                    ManageRequest(msg);
                }
                else
                { 
                    waitingRequests = waitingRequests.Enqueue(msg);
                }
            });

            Receive<ProofOfMisbehaviour>(VerifyProofMisbehaviour, msg =>
            {
                var hatePrimary = new IHateThePrimary(view);
                hatePrimary.Signature = EncryptionManager.SignMsg(hatePrimary.ToString(), privateKey);
                replicaRouter.Route(hatePrimary, Self);
                replicaRouter.Route(msg, Sender);

            });
            ManageDBResponse();
            ReceiveCommit();
            ReplicaManageClientRequest();
            ViewChangeManagment();
            FillHoleRequest();
            ManageCheckPoint();
            AskSnapshot();
            DeadReplica();
            ClientArrived();
            Byzantine();
            ViewChange();
        }

        private void ClientArrived()
        {
            Receive<ClientAndKey>(msg => 
                clientKeys.TryAdd(msg.ActorRef, msg.PublicKey)
            );
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

        private void SendRequestToDB(OrderReq msg)
        {
            Timers.Cancel("fill-hole" + msg.OrderReqSigned.SequenceNumber);
            Timers.Cancel("fill-hole-end" + msg.OrderReqSigned.SequenceNumber);
            if (byzantine && new Random().NextDouble() > 0.2)
            {
                history.Add(msg);
                _databaseChildren.Tell(msg, Self);
            }
            else if (byzantine)
            {
                _databaseChildren.Tell(msg, Self);
            }
            else
            {
                history.Add(msg);
                _databaseChildren.Tell(msg, Self);
            }

        }
        private void FillHoleRequest()
        {
            Receive<FillHoleTimerEnd>(msg =>
            { 
                var fillHoleMsg = new FillHole(msg.FillHole.View, msg.FillHole.MaxSequenceNumber, msg.FillHole.SequenceNumber, msg.FillHole.ReplicaId);
                fillHoleMsg.Signature = EncryptionManager.SignMsg(fillHoleMsg.ToString(), privateKey);
                if (!Timers.IsTimerActive("fill-hole-end" + msg.FillHole.SequenceNumber))
                {
                    Timers.StartSingleTimer("fill-hole-end" + msg.FillHole.SequenceNumber, new FillHoleTimerEnd2(), TimeSpan.FromSeconds(TIME_TO_RESP));
                }
                replicaRouter.Route(fillHoleMsg, Self);

            });

            Receive<FillHoleTimerEnd2>(msg =>
            {
                var hatePrimary = new IHateThePrimary(view);
                hatePrimary.Signature = EncryptionManager.SignMsg(hatePrimary.ToString(), privateKey);
                replicaRouter.Route(hatePrimary, Self);

            });
            Receive<FillHole>(VerifyFillHole, msg =>
            {
                FillHole(msg, Sender);
            });

            Receive<OrderReqFillHole>(VerifyOrderFillHole, msg =>
            {
                if (waitingRequestsFillHole.IsEmpty)
                {
                    if (IfFattorizzato2(msg.OrderReq, history))
                    {
                        waitingRequestsFillHole = waitingRequestsFillHole.Enqueue(msg.OrderReq);
                        SendRequestToDB(msg.OrderReq);
                    }
                    else if (IfFattorizzato3(msg.OrderReq, history))
                    {
                        SendFillHole(msg.OrderReq.OrderReqSigned.SequenceNumber);
                    }
                    else if ((checkpoint?.SequenceNumber ?? CP_INTERVAL) < msg.OrderReq.OrderReqSigned.SequenceNumber)
                    {
                        var myOrderReq = history.Find(x => x.OrderReqSigned.SequenceNumber == msg.OrderReq.OrderReqSigned.SequenceNumber);
                        if (myOrderReq is not null && !myOrderReq.Equals(msg.OrderReq))
                        {
                            var hatePrimary = new IHateThePrimary(view);
                            hatePrimary.Signature = EncryptionManager.SignMsg(hatePrimary.ToString(), privateKey);
                            replicaRouter.Route(hatePrimary, Self);
                            var proof = new ProofOfMisbehaviour(view, (myOrderReq, msg.OrderReq));
                            proof.Signature = EncryptionManager.SignMsg(proof.ToString(), privateKey);
                            replicaRouter.Route(proof, Self);
                        }
                    }
                }
                else
                {
                    var myOrderReq = history.Find(x => x.OrderReqSigned.SequenceNumber == msg.OrderReq.OrderReqSigned.SequenceNumber);
                     if (!(myOrderReq?.Equals(msg.OrderReq) ?? true))
                    {
                        var hatePrimary = new IHateThePrimary(view);
                        hatePrimary.Signature = EncryptionManager.SignMsg(hatePrimary.ToString(), privateKey);
                        replicaRouter.Route(hatePrimary, Self);
                        var proof = new ProofOfMisbehaviour(view, (myOrderReq, msg.OrderReq));
                        proof.Signature = EncryptionManager.SignMsg(proof.ToString(), privateKey);
                        replicaRouter.Route(proof, Self);
                    }
                    else
                    {    
                        if (!history.Exists(x => x.Equals(msg.OrderReq)))
                        {
                            waitingRequestsFillHole = waitingRequestsFillHole.Enqueue(msg.OrderReq);
                        }
                    }

                }
            });

        }
        private void ReplicaManageClientRequest()
        {

            Receive<IRequest<IPersonaRequest>>(VerifyRequest, msg =>
             {
                 if (lastCommitCertificate?.Response.History.Exists(x => x.OrderReqSigned.DigestRequest == DigestManager.GenerateSHA512String(msg.ToString()))??false)
                 {
                     var localCommit = new LocalCommit(view, DigestManager.GenerateSHA512String(msg.ToString()), lastCommitCertificate.Response.History.ToList(), myId, msg.Client);
                     localCommit.Signature = EncryptionManager.SignMsg(localCommit.ToString(), privateKey);
                     msg.Client.Tell(localCommit);
                 }
                 else if (!cacheResponse.TryGetValue(msg.Client, out (ISpecResponse, OrderReqSigned) value) || (value.Item1 is SpecResponse<IPersonaResponse> t && t.SpecResponseSigned.Timestamp < msg.Timestamp))
                 {
                     if (!Timers.IsTimerActive(msg.ToString()))
                     {
                         var confirmReq = new ConfirmReq<IRequest>(view, msg, myId);
                         confirmReq.Signature = EncryptionManager.SignMsg(confirmReq.ToString(), privateKey);
                         Timers.StartSingleTimer(msg.ToString(), confirmReq, TimeSpan.FromSeconds(TIME_TO_RESP));
                         primary.Item1.Actor.Tell(confirmReq);

                     } 
                 }
                 else
                 {
                    msg.Client.Tell(value.Item1);
                 }
             });

            Receive<ConfirmReq<IRequest>>(VerifyConfirmReq, msg =>
             {
                 if (msg.myId == myId && msg.view == view)
                 {
                     var hatePrimary = new IHateThePrimary(view);
                     hatePrimary.Signature = EncryptionManager.SignMsg(hatePrimary.ToString(), privateKey);
                     replicaRouter.Route(hatePrimary, Self);
                     replicaRouter.Route(msg, Self);
                 }
                 else
                 {
                    var request = msg.msg as IRequest<IPersonaRequest>; 
                    var x = history.Where(x => x.OrderReqSigned.DigestRequest == DigestManager.GenerateSHA512String(msg.msg.ToString()));
                    if (x.Count() == 1)
                     {
                         Sender.Tell(new OrderReq<IPersonaRequest>(x.First().OrderReqSigned, request, x.First().Signature)); 
                    }
                     else
                     {
                        var confirmReq = new ConfirmReq<IRequest>(view, request, myId);
                        confirmReq.Signature = EncryptionManager.SignMsg(confirmReq.ToString(), privateKey);
                        if (!Timers.IsTimerActive(request.ToString()))
                        {
                            Timers.StartSingleTimer(request.ToString(), confirmReq, TimeSpan.FromSeconds(TIME_TO_RESP));
                        }
                        primary.Item1.Actor.Tell(confirmReq);
                     } 

                }
             });

        }

        private void ViewChangeManagment()
        {
            Receive<IHateThePrimary>(VerifyHatePrimary, msg =>
            {
                hateDict.TryAdd(Sender, msg);
                if (hateDict.Count >= maxFailures + 1)
                {
                    StartViewChange();
                }
            });
        }
        private void ViewChange()
        {

            Receive<ViewChangeCommit>(VerifyViewChange, msg =>
            {
                if (myId == actualPrimary)
                {
                    actualPrimary = -1;
                }
                hateDict.Clear();
                msg.MyProof.ToList().ForEach(x => hateDict.Add(x.Key, x.Value));
                viewChangeDict.TryAdd(Sender, msg.ViewChange);
                StartViewChange();
             });
        }
        private void StartViewChange()
        {
            if (!waitingRequestsFillHole.IsEmpty)
            {
                var fillhole = waitingRequestsFillHole.Peek();
                if (fillhole.Equals(history.LastOrDefault()))
                {
                    history.Remove(fillhole);
                    _databaseChildren.Tell(new RevertAction(), ActorRefs.NoSender);
                }
             
            }
            else if (!waitingRequests.IsEmpty)
            {  
                var request = waitingRequests.Peek();
                if (request.Equals(history.LastOrDefault()))
                {
                    history.Remove(request);
                    _databaseChildren.Tell(new RevertAction(), ActorRefs.NoSender);
                }
                
            }

            waitingRequests = waitingRequests.Clear();
            waitingRequestsFillHole = waitingRequestsFillHole.Clear();
            checkpointDictionaryAccum.TryGetValue(checkpoint?.SequenceNumber ?? 0, out List<Checkpoint> checks);
            var sequence = checks?.First().SequenceNumber ?? 0;
            var viewChangeMsg = new ViewChange(view + 1, sequence, checks, lastCommitCertificate, history.SkipWhile(x => x.OrderReqSigned.SequenceNumber < sequence).ToList(), myId);    
            viewChangeMsg.Signature = EncryptionManager.SignMsg(viewChangeMsg.ToString(), privateKey);
            replicaRouter.Route(new ViewChangeCommit(viewChangeMsg, hateDict), Self);
            if (actualPrimary != -1)
            {
                primary.Item1.Actor.Tell(new ViewChangeCommit(viewChangeMsg, hateDict), Self);
                replicas.TryAdd(actualPrimary, primary);
                CreateRouter();
            }
            viewChangeDict.TryAdd(Self, viewChangeMsg);
            view++;
            viewChangeStatus = true;  
            Become(ViewChangeStage);
        } 
       
        private void ViewChangeStage()
        {
            ClientArrived();
            Timers.CancelAll();
            Receive<ViewChangeCommit>(VerifyEqualViewChange, msg =>
            {
                viewChangeDict.TryAdd(Sender, msg.ViewChange);
                if (viewChangeDict.Count == 2 * maxFailures + 1)
                {
                    if (myId != view % (replicas.Count + 1))
                    {
                        actualPrimary = view % (replicas.Count + 1);
                        Timers.StartSingleTimer("tentative-view".Concat(view.ToString()), new ViewChangeTimerEnd(view), TimeSpan.FromSeconds(Math.Pow(10, countViewChangeTimer)));
                    }
                    else
                    {
                        actualPrimary = myId;  
                        var history = ConstructViewChangeHistory(viewChangeDict.Values.Select(x => x).ToList());
                        var newView = new NewView(view, viewChangeDict.Values.Select(x => x).ToList(), history);
                        newView.Signature = EncryptionManager.SignMsg(newView.ToString(), privateKey);
                        replicaRouter.Route(newView, Self);
                        Self.Tell(newView, Self);
                    }
                 }
            });
            Receive<ViewChangeCommit>(VerifyGreaterViewChange, msg =>
            {
                countViewChangeTimer++;
                Timers.Cancel("tentative-view".Concat(view.ToString()));
                var oldViewChangeMsg = viewChangeDict[Self];
                viewChangeDict.Clear();
                viewChangeDict.TryAdd(Sender, msg.ViewChange);
                var viewChangeMsg = new ViewChange(msg.ViewChange.NewView, oldViewChangeMsg.SequenceNumber, oldViewChangeMsg.CheckPointProof, oldViewChangeMsg.CommitCertificate, oldViewChangeMsg.History.Select(x => x).ToList(), myId);
                viewChangeMsg.Signature = EncryptionManager.SignMsg(viewChangeMsg.ToString(), privateKey);
                replicaRouter.Route(new ViewChangeCommit(viewChangeMsg, hateDict), Self); 
                viewChangeDict.TryAdd(Self, viewChangeMsg);
                view = msg.ViewChange.NewView;
                Become(ViewChangeStage);
            });
            Receive<ViewChangeTimerEnd>(msg =>
            {
                countViewChangeTimer++;
                var oldViewChangeMsg = viewChangeDict[Self];
                var viewChangeMsg = new ViewChange(oldViewChangeMsg.NewView + 1, oldViewChangeMsg.SequenceNumber, oldViewChangeMsg.CheckPointProof, oldViewChangeMsg.CommitCertificate, oldViewChangeMsg.History.Select(x => x).ToList(), myId);
                viewChangeMsg.Signature = EncryptionManager.SignMsg(viewChangeMsg.ToString(), privateKey);
                replicaRouter.Route(new ViewChangeCommit(viewChangeMsg, hateDict), Self);
                viewChangeDict.Clear();
                viewChangeDict.TryAdd(Self, viewChangeMsg);
                view = oldViewChangeMsg.NewView + 1;
                Become(ViewChangeStage);
            });

            Receive<NewView>(VerifyNewView, msg =>
             {
                if (myId == actualPrimary)
                {
                    ConstructHistoryAndDbForViewChange(msg);
                }
                else
                {
                    var _history = ConstructViewChangeHistory(msg.ViewChange); 
                    if (_history.SequenceEqual(msg.History))
                    {
                        Timers.Cancel("tentative-view".Concat(view.ToString())); 
                        ConstructHistoryAndDbForViewChange(msg);
                    }
                }
            });
            AskSnapshot();
        }
        private void ConstructHistoryAndDbForViewChange(NewView msg)
        {
           
            cacheResponse.Clear();
            newView = msg;
            max_l = history.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? -1;  
            min_s = msg.ViewChange.Max(x => x.CheckPointProof?.Max(x => x.SequenceNumber)) ?? -1;
            if(max_l>=min_s && (msg.History.Count==0 || history.Find(x => x.OrderReqSigned.SequenceNumber == max_l)?.OrderReqSigned.DigestHistory == msg.History.Find(x => x.OrderReqSigned.SequenceNumber == max_l)?.OrderReqSigned.DigestHistory))
            {
                UpdateHistoryWhenEqualsWithNew(msg, max_l);
            }
            else if ((max_l < min_s) || (max_l >= min_s && history.Find(x => x.OrderReqSigned.SequenceNumber == max_l)?.OrderReqSigned.DigestHistory != msg.History.Find(x => x.OrderReqSigned.SequenceNumber == max_l)?.OrderReqSigned.DigestHistory))
            {
                var variabile = newView.ViewChange.Where(x => x.CheckPointProof?.Count > 0);
                checkpoint = variabile.Select(x => x.CheckPointProof.Find(xx => xx.SequenceNumber == min_s)).FirstOrDefault();
                var checkpoints = variabile.SelectMany(x => x.CheckPointProof.FindAll(x => x.SequenceNumber == min_s));
                if (checkpoints.Any())
                {
                    var idReplica = checkpoints.Select(x => x.ReplicaId).Distinct(); 
                    idReplica.ToList().ForEach(x =>
                    {
                        if (x != myId)
                        {
                             
                            replicas[x].Item1.Actor.Tell(new AskSnapshot(), Self);
                        }
                        else
                        {
                            _databaseChildren.Tell(new AskSnapshot(), Self);
                        }
                    });
                    Timers.StartPeriodicTimer(myId, new ReplicasSnapshot(idReplica.ToList()), TimeSpan.FromSeconds(5));
                    Become(SnapshotState);
                }
                else
                {
                    Timers.StartPeriodicTimer(myId, new ReplicasSnapshot(new List<int> { myId }), TimeSpan.FromSeconds(5));
                    _databaseChildren.Tell(new AskSnapshot(), Self);
                    Become(SnapshotState);
                }
            }
            
        }

        private void SnapshotState()
        {
            ViewChangeManagment();
            ViewChangeStage();
            AskSnapshot();
            Receive<ReplicasSnapshot>(msg =>
            {
                msg.ReplicasId.ForEach(x =>
                {
                    if (x != myId)
                    { 
                        replicas[x].Item1.Actor.Tell(new AskSnapshot(), Self);
                    }
                    else
                    {
                        _databaseChildren.Tell(new AskSnapshot(), Self);
                    }
                });
            });
            Receive<SnapshotReply>(msg =>
            {
                Timers.Cancel(myId);
                _databaseChildren.Tell(msg, Self);
                
            });
            Receive<SnapshotSave>(msg =>
            {
                history.Clear(); 
                CopyMinS(newView, min_s); 
                if (min_s == 0 && newView.History.Count > 0)
                {
                    waitingRequests = waitingRequests.Enqueue(newView.History.First());
                }
                UpdateDBStatus(newView, min_s);
            });
        }

        private void EndReconciliate()
        {
            InitializeNewView();
            Stash.UnstashAll(); 
            actualPrimary = view % (replicas.Count + 1);
            if (myId == actualPrimary)
            {
                replicas = replicas.Where(x => x.Key != myId).ToDictionary(x=>x.Key,x=>x.Value);
                CreateRouter();
                Become(Primary);
            }
            else
            {
                UpdateReplicaRouter();
                Become(Replica);
            }
        }


        private void ReconciliateStatus()
        {

            ReceiveCommit();
            Timers.StartPeriodicTimer(myId, new ReconciliateMsg(), TimeSpan.FromSeconds(1));
            Receive<ReconciliateMsg>(_ =>
            {
                Timers.Cancel(myId);
                NextElementInQueue(waitingRequests);
                if (waitingRequests.IsEmpty)
                {
                    EndReconciliate();
                }
            });

            ViewChangeManagment(); 
            AskSnapshot();
            Receive<SpecResponse<IPersonaResponse>>(msg =>
            {
                if (msg.SpecResponseSigned.SequenceNumber >= (newView.ViewChange.Max(x => x.CommitCertificate?.Response.SequenceNumber) ?? min_s))
                {
                    var replyToClient = SignSpecRespone(msg);
                    replyToClient.SpecResponseSigned.Client.Tell(replyToClient, Self); 
                    if (!cacheResponse.TryAdd(replyToClient.SpecResponseSigned.Client, (replyToClient, replyToClient.OrderReqSigned)))
                    {
                        cacheResponse[replyToClient.SpecResponseSigned.Client] = (replyToClient, replyToClient.OrderReqSigned);
                    }
                } 
                 
                if (!waitingRequests.IsEmpty)
                {
                    waitingRequests = waitingRequests.Dequeue();
                }
                if (waitingRequests.IsEmpty)
                {
                    EndReconciliate();
                }
                else
                {
                    NextElementInQueue(waitingRequests);
                    if (waitingRequests.IsEmpty)
                    {
                        EndReconciliate();
                    }
                }
               
            });
           
            Receive<ViewChangeCommit>(_ =>
            {
                Stash.Stash();
            });  

        } 
        private void AskSnapshot()
        {
            Receive<AskSnapshot>(msg =>
            {
                _databaseChildren.Tell(msg, Sender);
            });
        }
        private void UpdateReplicaRouter()
        {
            replicas.TryGetValue(view % (replicas.Count + 1), out primary);
            actualPrimary = view % (replicas.Count + 1);
            replicas.Remove(view % (replicas.Count + 1));
            CreateRouter();
        }

        private void InitializeNewView()
        {  
            countViewChangeTimer = 1;

            requestOk.Clear(); 
            lastClientTimestamp.Clear();
            hateDict.Clear();
            viewChangeDict.Clear();
            checkpointDictionarySpec.Clear();
            checkpointDictionaryAccum.Clear();
            waitingRequests = waitingRequests.Clear();
            waitingRequestsFillHole = waitingRequestsFillHole.Clear();
            sequenceNumber = newView.History.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? history.LastOrDefault()?.OrderReqSigned.SequenceNumber ??
                newView.ViewChange.SelectMany(x => x.History).GroupBy(x => x.OrderReqSigned?.SequenceNumber)
                .Where(x => x.Count() == 2 * maxFailures + 1).OrderByDescending(x => x.Key).FirstOrDefault()?.Key ??
                newView.ViewChange.Max(x => x.CommitCertificate?.Response.SequenceNumber) ?? 0;
            if (sequenceNumber >= 0 && newView.History.Any())
            {
                sequenceNumber++;
            }
            var maxCheckPoint = newView.ViewChange.Max(x => x.CheckPointProof?.Max(x=>x.SequenceNumber))??-1;
            var variabile = newView.ViewChange.Exists(x => x.CheckPointProof?.Exists(x=>x.SequenceNumber == maxCheckPoint)??false)? newView.ViewChange.FindAll(x => x.CheckPointProof?.Exists(x => x.SequenceNumber == maxCheckPoint)??false).SelectMany(x => x.CheckPointProof).Take(2 * maxFailures + 1).ToList():new List<Checkpoint>();
        
            lastCommitCertificate = newView.ViewChange.Find(x => newView.ViewChange.Max(x => x.CommitCertificate?.Response.SequenceNumber) == x.CommitCertificate?.Response.SequenceNumber)?.CommitCertificate;
            var max = variabile.Max(x => x?.SequenceNumber);
            checkpoint = variabile.Find(x => x?.SequenceNumber == max);//.Select(x => x.CheckPointProof.Find(xx => xx.SequenceNumber == x.CheckPointProof.Max(xxx => xxx.SequenceNumber))).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();

            if (checkpoint is not null)
            {
                checkpoint = new Checkpoint(checkpoint.SequenceNumber, checkpoint.DigestHistory, myId);
            }
            
            checkpoint = (lastCommitCertificate?.Response.SequenceNumber % CP_INTERVAL == 0 && lastCommitCertificate?.Response.SequenceNumber != 0? (checkpoint?.SequenceNumber >= lastCommitCertificate.Response.SequenceNumber ?
            checkpoint : new Checkpoint(lastCommitCertificate.Response.SequenceNumber, lastCommitCertificate.Response.History.Last().OrderReqSigned.DigestHistory, myId))
            : checkpoint) ?? checkpoint;

            if(checkpoint is not null)
            {
                checkpoint.Signature = EncryptionManager.SignMsg(checkpoint.ToString(), privateKey);
                checkpointDictionaryAccum.TryAdd(checkpoint.SequenceNumber, new List<Checkpoint>() {checkpoint});
            }
            
            if (variabile.Any())
            {
                checkpointDictionaryAccum.TryGetValue(checkpoint.SequenceNumber, out List<Checkpoint> proof);
                proof.AddRange(variabile.Where(x => x.ReplicaId != myId).ToList());
                checkpointDictionaryAccum[checkpoint.SequenceNumber] = proof;
            }

            _zyzzyvaRouter.Tell(new FinalNewView(view));
            snapshotReplies.Clear();
        }
        private void CopyMinS(NewView msg, int min_s)
        {
            waitingRequests.Clear();
            waitingRequestsFillHole.Clear();
            if (min_s > 0) 
            {
                var max_cc = msg.ViewChange.Max(x => x.CommitCertificate.Response.SequenceNumber);
                var orderReq = msg.ViewChange.Find(x => x.CommitCertificate?.Response.SequenceNumber == max_cc && x.CommitCertificate.Response.History.Exists(x => x.OrderReqSigned.SequenceNumber == min_s))?.CommitCertificate.Response
               .History.Find(x => x.OrderReqSigned?.SequenceNumber == min_s);
                 
                history.Add(ConstructNewHistoryMinS(orderReq)); 
            }
        }

        private void UpdateHistoryWhenEqualsWithNew(NewView msg, int sequenceNumber)
        {
            if (msg.History.Count > 0)
            {
                history.Clear();
                CopyMinS(msg,min_s);
                history.AddRange(msg.History.TakeWhile(x => x.OrderReqSigned.SequenceNumber <= sequenceNumber).ToList());
               
            }
            UpdateDBStatus(msg, sequenceNumber);
        }
        private void UpdateDBStatus(NewView msg, int sequenceNumber)
        {
           
            msg.History.Where(x => x.OrderReqSigned.SequenceNumber > sequenceNumber).ToList().ForEach(x => waitingRequests = waitingRequests.Enqueue(x));
            if (waitingRequests.IsEmpty)
            {
                InitializeNewView();
                Stash.UnstashAll();
                if (myId == actualPrimary)
                { 
                    Become(Primary);
                }
                else
                { 
                    UpdateReplicaRouter();
                    Become(Replica);
                }
            }
            else
            {
                actualPrimary = -1; 
                Become(ReconciliateStatus);
            }
        }

        private void ManageCheckPoint()
        {
          
            Receive<SpecResponse<string>>(VerifySpecResponseCheckPoint, msg =>
            {

                if (history.Count > 0 && msg.SpecResponseSigned.SequenceNumber >= CP_INTERVAL && msg.SpecResponseSigned.SequenceNumber != checkpoint?.SequenceNumber && msg.SpecResponseSigned.SequenceNumber % CP_INTERVAL == 0)
                {
                    if (!checkpointDictionarySpec.ContainsKey(msg.SpecResponseSigned.SequenceNumber))
                    {
                        checkpointDictionarySpec.Add(msg.SpecResponseSigned.SequenceNumber, new List<SpecResponse<string>>() { msg });
                    }
                    else if (!checkpointDictionarySpec[msg.SpecResponseSigned.SequenceNumber].Contains(msg) && checkpointDictionarySpec[msg.SpecResponseSigned.SequenceNumber].Count < 2 * maxFailures + 1)
                    {
                        checkpointDictionarySpec[msg.SpecResponseSigned.SequenceNumber].Add(msg);
                    }
                    if (history.Last().OrderReqSigned.SequenceNumber == ((checkpoint?.SequenceNumber + CP_INTERVAL) ?? CP_INTERVAL) && checkpointDictionarySpec[msg.SpecResponseSigned.SequenceNumber].Count == 2 * maxFailures + 1)
                    {
                        CheckCheckPointMsg(msg.SpecResponseSigned.SequenceNumber, checkpointDictionarySpec[msg.SpecResponseSigned.SequenceNumber]);
                    }
                }
            });
            Receive<Checkpoint>(VerifyCheckPoint, msg =>
             {
                 if (history.Count > 0 && msg.SequenceNumber >= CP_INTERVAL && msg.SequenceNumber != checkpoint?.SequenceNumber && msg.SequenceNumber % CP_INTERVAL == 0)
                 {
                     if (!checkpointDictionaryAccum.ContainsKey(msg.SequenceNumber))
                     {
                         checkpointDictionaryAccum.Add(msg.SequenceNumber, new List<Checkpoint>() { msg });
                     }
                     else if (!checkpointDictionaryAccum[msg.SequenceNumber].Contains(msg))
                     {
                         checkpointDictionaryAccum[msg.SequenceNumber].Add(msg);
                     }
                     if (history.LastOrDefault()?.OrderReqSigned.SequenceNumber == ((checkpoint?.SequenceNumber + CP_INTERVAL) ?? CP_INTERVAL) && (checkpointDictionaryAccum[msg.SequenceNumber].Count >= maxFailures + 1) && lastCommitCertificate?.Response.SequenceNumber == history.LastOrDefault()?.OrderReqSigned.SequenceNumber)
                     {
                         MakeCheckPoint(checkpointDictionaryAccum[msg.SequenceNumber]);
                     }
                 }

             });
        }

        private void CheckCheckPointMsg(int sequenceNumber, List<SpecResponse<string>> specForCheckpoint)
        {
            var listaR = specForCheckpoint.GroupBy(Metodinos).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList())).OrderByDescending(x => x.Count);
            if (listaR.First().Count >= 2 * maxFailures + 1)
            {
                var replicaRight = listaR.First().Item2;
                lastCommitCertificate = new CommitCertificate(replicaRight.Select(x => (x.ReplicaId, x.Signature)).ToList(), replicaRight.First().SpecResponseSigned);
                var orderReq = lastCommitCertificate.Response.History.Exists(x => x.OrderReqSigned.SequenceNumber == min_s)? lastCommitCertificate.Response.History.Find(x => x.OrderReqSigned?.SequenceNumber == min_s):null;
                SendCheckpoint(sequenceNumber);
            }

        }
        private void MakeCheckPoint(List<Checkpoint> checkpoints)
        {
            var listaCheckpoint = checkpoints.GroupBy(CheckCheckpoint).Select(specResponse => (specResponse.ToList().Count, specResponse.ToList())).OrderByDescending(x => x.Count);
             
            if (listaCheckpoint.First().Count >= maxFailures + 1)
            {
                var check = listaCheckpoint.First().Item2.First();
                checkpointDictionarySpec.Remove(check.SequenceNumber);
                checkpoint = check;
                history = history.SkipWhile(x => x.OrderReqSigned.SequenceNumber < check.SequenceNumber).ToList();
                _databaseChildren.Tell(check);
                ContinueNextRequest();
                if (actualPrimary == myId && !waitingRequestPrimary.IsEmpty)
                {
                    var req = waitingRequestPrimary.Peek();
                    waitingRequestPrimary = waitingRequestPrimary.Dequeue();
                    ConvertRequest(req);
                }
                
            }
        }
        private void SendCheckpoint(int SequenceNumber)
        {
           
            var digestHistory = history.TakeWhile(x => x.OrderReqSigned.SequenceNumber <= SequenceNumber).ToList();
            var checkPointMsg = new Checkpoint(SequenceNumber, digestHistory.Last().OrderReqSigned.DigestHistory, myId);
            checkPointMsg.Signature = EncryptionManager.SignMsg(checkPointMsg.ToString(), privateKey);
            if (myId != actualPrimary && actualPrimary != -1)
            {
                primary.Item1.Actor.Tell(checkPointMsg, Self);
            }
            replicaRouter.Route(checkPointMsg, Self);

            if (checkpointDictionaryAccum.TryGetValue(SequenceNumber, out List<Checkpoint> checks) && checks.Count >= maxFailures + 1)
            {
                MakeCheckPoint(checks);
            }
        }

        private SpecResponse<T> SignSpecRespone<T>(SpecResponse<T> specToSign){
                var specSigned = new SpecResponseSigned(specToSign.SpecResponseSigned, history);
                var signature = EncryptionManager.SignMsg(specSigned.ToString(), privateKey);
                return new SpecResponse<T>(specToSign.GetResponse(), specToSign.OrderReqSigned, specSigned, specToSign.OrderReqSignature, myId, signature);
        }
        private void ManageDBResponse()
        {
            Receive<SpecResponse<IPersonaResponse>>(msg =>
            {
                var replyToClient = SignSpecRespone(msg);
                 
                if (!cacheResponse.TryAdd(replyToClient.SpecResponseSigned.Client, (replyToClient, replyToClient.OrderReqSigned)))
                {
                    
                    cacheResponse[replyToClient.SpecResponseSigned.Client] = (replyToClient, replyToClient.OrderReqSigned);
                }
                replyToClient.SpecResponseSigned.Client.Tell(replyToClient, Self);
                CheckpointCheckRequest(replyToClient);
                if(actualPrimary == myId && !waitingRequestPrimary.IsEmpty)
                {
                    var req = waitingRequestPrimary.Peek();
                    waitingRequestPrimary = waitingRequestPrimary.Dequeue();
                    ConvertRequest(req);
                }
            });
        }

        private void ConvertRequest(IRequest request) 
        {
            if(request is IRequest<IPersonaRequest> req)
            {
                SendRequest(req);
            }   
        }

        private void CheckpointCheckRequest<T>(SpecResponse<T> replyToClient)
        {
            if (replyToClient.OrderReqSigned.SequenceNumber == ((checkpoint?.SequenceNumber + CP_INTERVAL) ?? CP_INTERVAL))
            {
                var signature = EncryptionManager.SignMsg(replyToClient.SpecResponseSigned.ToString(), privateKey);
                var msgSpecResponseCheckpoint = new SpecResponse<string>(replyToClient.SpecResponseSigned.DigestResponse, replyToClient.OrderReqSigned, replyToClient.SpecResponseSigned, replyToClient.OrderReqSignature, replyToClient.ReplicaId, signature);
                if (myId != actualPrimary && actualPrimary != -1)
                {
                    primary.Item1.Actor.Tell(msgSpecResponseCheckpoint, Self);
                }
                replicaRouter.Route(msgSpecResponseCheckpoint, Self);
                if (checkpointDictionarySpec.TryGetValue(msgSpecResponseCheckpoint.SpecResponseSigned.SequenceNumber, out List<SpecResponse<string>> specs) && specs.Count >= 2 * maxFailures + 1)
                {
                    var sequence = msgSpecResponseCheckpoint.SpecResponseSigned.SequenceNumber;
                    CheckCheckPointMsg(sequence, checkpointDictionarySpec[sequence]);
                }
            }
            else
            {
                ContinueNextRequest();
            }
        }

        private void ContinueNextRequest()
        {
            if (!waitingRequestsFillHole.IsEmpty)
            {
                waitingRequestsFillHole = waitingRequestsFillHole.Dequeue(); 
                NextElementInQueue(waitingRequestsFillHole);
                if (waitingRequestsFillHole.IsEmpty)
                {
                    if (!waitingRequests.IsEmpty)
                    {
                        var msg = waitingRequests.Peek();
                        requestOk.RemoveWhere(x => x.OrderReqSigned.SequenceNumber == msg.OrderReqSigned.SequenceNumber);
                        NextElementInQueue(waitingRequests);
                    }
                }
            }
            else
            {
                if (!waitingRequests.IsEmpty)
                {
                    var msg = waitingRequests.Peek();
                    requestOk.RemoveWhere(x => x.OrderReqSigned.SequenceNumber == msg.OrderReqSigned.SequenceNumber);
                    waitingRequests = waitingRequests.Dequeue();
                    NextElementInQueue(waitingRequests);
                }
            }
        }
        private void NextElementInQueue(ImmutableQueue<OrderReq> orders)
        {
            
            if (!orders.IsEmpty)
            {
                var nextReq = orders.Peek();
                var fd = (nextReq switch
                {
                    OrderReq<IPersonaRequest> req => ((Action<OrderReq<IPersonaRequest>>)ManageRequest, req),
                    _ => throw new NotImplementedException()
                });
                fd.Item1(fd.req);
            }
        }
        private void ManageRequest<T>(OrderReq<T> msg)
        {

            if (IfFattorizzato(msg, view))
            {
                if (IfFattorizzato2(msg, history))
                {
                    CancelTimer(msg);  
                    if (byzantine)
                    {
                        var request = new Request<IPersonaRequest>(new ReadPersona(new Random().Next()), msg.Request.Client, msg.Request.Timestamp)
                        {
                            Signature = msg.Request.Signature
                        };
                        var newOrderReq = new OrderReq<IPersonaRequest>(msg.OrderReqSigned, request, msg.Signature);
                        SendRequestToDB(newOrderReq);
                    }
                    else
                    {
                        SendRequestToDB(msg);
                    }
                }
                else if (IfFattorizzato3(msg, history) && !viewChangeStatus && myId != actualPrimary)
                {
                     
                    waitingRequestsFillHole = waitingRequestsFillHole.Clear();

                    SendFillHole(msg.OrderReqSigned.SequenceNumber);
                }
                else
                { 
                    ContinueNextRequest();
                }
            }
            else
            {
                ContinueNextRequest();
            }
        }
        private void ReceiveCommit()
        {
            
            Receive<Commit>(VerifyCommit, msg =>
             {   
                if (msg.CommitCertificate.Replica.Count >= (2 * maxFailures + 1) && msg.CommitCertificate.Replica.Count < (3 * maxFailures + 1) && VerifyCC(msg.CommitCertificate))
                 {
                     var notPresent = msg.CommitCertificate.Response.History.Where(x => !history.Contains(x) && x.OrderReqSigned.SequenceNumber >= (history.FirstOrDefault()?.OrderReqSigned.SequenceNumber??-1)).OrderBy(x => x.OrderReqSigned.SequenceNumber);
                     if (!notPresent.Any())
                     {
                         lastCommitCertificate ??= msg.CommitCertificate;

                         if (lastCommitCertificate.Response.SequenceNumber < msg.CommitCertificate.Response.SequenceNumber)
                         {
                             lastCommitCertificate = msg.CommitCertificate;
                         }
                         var digestRequest = msg.CommitCertificate.Response.History.Find(x => msg.CommitCertificate.Response.SequenceNumber == x.OrderReqSigned.SequenceNumber).OrderReqSigned.DigestRequest;
                         var localCommit = new LocalCommit(view, digestRequest, lastCommitCertificate.Response.History.TakeWhile(x => x.OrderReqSigned.SequenceNumber <= msg.CommitCertificate.Response.SequenceNumber).ToList(), myId, msg.Client);
                         localCommit.Signature = EncryptionManager.SignMsg(localCommit.ToString(), privateKey);
                         msg.Client.Tell(localCommit);
                         if (msg.CommitCertificate.Response.SequenceNumber == ((checkpoint?.SequenceNumber ?? 0) + CP_INTERVAL))
                         {
                             SendCheckpoint(msg.CommitCertificate.Response.SequenceNumber);
                         }
                     }
                     else if (myId != actualPrimary && notPresent.First().OrderReqSigned.SequenceNumber > (history.LastOrDefault()?.OrderReqSigned.SequenceNumber??-1))
                     {
                         
                        SendFillHole(notPresent.Last().OrderReqSigned.SequenceNumber);
                     }
                     else if (actualPrimary != myId && history.Exists(x=>!msg.CommitCertificate.Response.History.Contains(x)))
                     {
                        var hate = new IHateThePrimary(view);
                        hate.Signature = EncryptionManager.SignMsg(hate.ToString(), privateKey);
                        replicaRouter.Route(hate, Self);
                        var proof = new ProofOfMisbehaviour(view, (notPresent.First(), history.Find(x => x.OrderReqSigned.SequenceNumber == notPresent.First().OrderReqSigned.SequenceNumber)));
                        proof.Signature = EncryptionManager.SignMsg(proof.ToString(), privateKey);
                        replicaRouter.Route(proof, Self);
                     }
                 }
             });

        }

        private void FillHole(FillHole msg, IActorRef Sender) => history.Where(x => x.OrderReqSigned.SequenceNumber >= msg.MaxSequenceNumber && 
        x.OrderReqSigned.SequenceNumber <= msg.SequenceNumber).ToList().ForEach(x => Sender.Tell(new OrderReqFillHole(x), Self));

        private void SendFillHole(int SequenceNumber)
        {
            var fillHoleMsg = new FillHole(view, ((history.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? -1) + 1), SequenceNumber, myId);
            var signature = EncryptionManager.SignMsg(fillHoleMsg.ToString(), privateKey);
            fillHoleMsg.Signature = signature;
            if (!Timers.IsTimerActive("fill-hole" + SequenceNumber) && !Timers.IsTimerActive("fill-hole-end" + SequenceNumber))
            {
                Timers.StartSingleTimer("fill-hole" + SequenceNumber, new FillHoleTimerEnd(fillHoleMsg), TimeSpan.FromSeconds(TIME_TO_RESP));
            }
            primary.Item1.Actor.Tell(fillHoleMsg, Self);
        }

        /// <include file="Docs/Akka/Replica/ReplicaManager.xml" path='docs/members[@name="replicamanager"]/MyProps/*'/>
        public static Props MyProps => Props.Create(() => new ReplicaManager());
    }
}
