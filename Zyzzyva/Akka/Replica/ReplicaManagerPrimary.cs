using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Zyzzyva.Akka.Client.Children;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Security;
namespace Zyzzyva.Akka.Replica
{
    public partial class ReplicaManager : ReceiveActor, IWithUnboundedStash, IWithTimers
    {
        private readonly List<OrderReq> orderReqs = new List<OrderReq>();
        private ImmutableQueue<IRequest> waitingRequestPrimary = ImmutableQueue<IRequest>.Empty;
        private void Primary()
        {
            viewChangeStatus = false;
            Receive<IRequest<IPersonaRequest>>(VerifyRequest, msg =>
            {
                if (lastCommitCertificate?.Response.History.Exists(x => x.OrderReqSigned.DigestRequest == DigestManager.GenerateSHA512String(msg.ToString())) ?? false)
                {
                    var localCommit = new LocalCommit(view, DigestManager.GenerateSHA512String(msg.ToString()), lastCommitCertificate.Response.History.ToList(), myId, msg.Client);
                    localCommit.Signature = EncryptionManager.SignMsg(localCommit.ToString(), privateKey);
                    msg.Client.Tell(localCommit);
                }
                else if (!lastClientTimestamp.TryGetValue(msg.Client.ToString(), out int lastTimestamp) || lastTimestamp < msg.Timestamp)
                {
                    lastClientTimestamp[msg.Client.ToString()] = msg.Timestamp;
                    if (!byzantine)
                    {
                        SendRequest(msg);
                    }
                    else
                    {
                        SendByzantineRequest(msg);
                    }
                }
            });
            ConfirmRequest();
            FillHoles();
            ManageDBResponse();
            ViewChange();
            ManageCheckPoint();
            AskSnapshot();
            ReceiveCommit();
            DeadReplica();
            ClientArrived();
            Byzantine();
        }

        private void FillHoles()
        {
            Receive<FillHole>(VerifyFillHole, msg =>
             {
                 if (msg.View == view)
                 {
                     FillHole(msg, Sender);
                 }
             });
        }

        private void ConfirmRequest()
        {
            Receive<ConfirmReq<IRequest>>(VerifyConfirmReq, m =>
            { 
                var request = m.msg as IRequest<IPersonaRequest>;

                if (!lastClientTimestamp.TryGetValue(request.Client.ToString(), out int lastTimestamp) || lastTimestamp < request.Timestamp)
                { 
                    lastClientTimestamp[request.Client.ToString()] = request.Timestamp;
                    SendRequest(request);
                }
                else if (cacheResponse.ContainsKey(request.Client))
                { 
                    ConfirmCachedRequest(request, m);
                }

            });
        }

        private void ConfirmCachedRequest<T>(IRequest<T> m, ConfirmReq<IRequest> confirm)
        {
            if (replicas.TryGetValue(confirm.myId, out (ActorRefRoutee, RSAParameters) value))
            {
                var cachedReq = cacheResponse[m.Client].Item2;
                var signature = EncryptionManager.SignMsg(cachedReq.ToString(), privateKey);
                value.Item1.Actor.Tell(new OrderReq<T>(cachedReq, m, signature));
            }
        }

        private void SendRequest<T>(IRequest<T> msg)
        { 
            if (waitingRequests.IsEmpty)
            {
                var orderReqSigned = new OrderReqSigned(view, sequenceNumber++, DigestManager.GenerateSHA512String(msg.ToString()), DigestManager.DigestList(history.LastOrDefault()?.OrderReqSigned.DigestHistory?? string.Empty, DigestManager.GenerateSHA512String(msg.ToString()))); 
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privateKey);
                var request = new OrderReq<T>(orderReqSigned, msg, signature);
                replicaRouter.Route(request, Self);
                waitingRequests = waitingRequests.Enqueue(request);
                SendRequestToDB(request);
            }
            else
            {
                waitingRequestPrimary = waitingRequestPrimary.Enqueue(msg);
            }
        }

        private void SendByzantineRequest<T>(IRequest<T> msg)
        {
           
           
            if (waitingRequests.IsEmpty)
            {
                var orderReqSigned = new OrderReqSigned(view, sequenceNumber++, DigestManager.GenerateSHA512String(msg.ToString()), DigestManager.DigestList(history.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, DigestManager.GenerateSHA512String(msg.ToString())));
                var signature = EncryptionManager.SignMsg(orderReqSigned.ToString(), privateKey);
                var request = new OrderReq<T>(orderReqSigned, msg, signature); 
                waitingRequests = waitingRequests.Enqueue(request);
               
                if (new Random().NextDouble() > 0.7)
                {
                    replicaRouter.Route(request, Self);
                }
                else if (new Random().NextDouble() > 0.8)
                {
                    replicaRouter.Route(request, Self);
                    sequenceNumber--;
                }
                else
                {
                    replicas.Values.Where(x => new Random().NextDouble() > 0.5).ToList().ForEach(x => x.Item1.Actor.Tell(request, Self));
                }
                SendRequestToDB(request);
            }
            else
            {
                waitingRequestPrimary = waitingRequestPrimary.Enqueue(msg);
            } 
             
        }
        private OrderReq ConstructNewHistoryMinS(OrderReq x) => x switch
        {
            OrderReq<IPersonaRequest> xx => RecreateOrderReq(xx),
            _ => throw new NotImplementedException()
        };

        private OrderReq RecreateOrderReq<T>(OrderReq<T> xx)
        {

            var signedReq = new OrderReqSigned(view, xx.OrderReqSigned.SequenceNumber, xx.OrderReqSigned.DigestRequest, xx.OrderReqSigned.DigestHistory);
            var signature = EncryptionManager.SignMsg(signedReq.ToString(), privateKey);
            var request = new Request<T>(xx.Request.GetRequest(), xx.Request.Client, xx.Request.Timestamp)
            {
                Signature = xx.Request.Signature
            };
            var result = new OrderReq<T>(signedReq, request, signature); 
            return result;
        }
        private (int,string,string) GroupingConstructHistory(OrderReq x)=>(x.OrderReqSigned.SequenceNumber, x.OrderReqSigned.DigestHistory, x.OrderReqSigned.DigestRequest);
        private List<OrderReq> ConstructViewChangeHistory(List<ViewChange> changeDict)
        {
            
            int min_s = changeDict.Max(x => x.CheckPointProof?.Max(x => x.SequenceNumber)) ?? -1;
            var max_cc = changeDict.Max(x => x.CommitCertificate?.Response.SequenceNumber) ?? min_s;
            var max_r = changeDict.SelectMany(x => x.History).GroupBy(GroupingConstructHistory)
                        .OrderByDescending(x => x.Key.Item1).FirstOrDefault(x => x.Count() == 2 * maxFailures + 1)?.Key.Item1 ?? max_cc;
            max_r = max_r >= max_cc ? max_r : max_cc;
            var max_s = changeDict.Max(x => x.History.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? max_r);
            max_s = max_s >= max_r ? max_s : max_r;
            var computedHistory = new List<OrderReq>();
            if (min_s != -1)
            {
                var orderReq = changeDict.Find(x => x.CommitCertificate?.Response.SequenceNumber == max_cc && x.CommitCertificate.Response.History.Exists(x =>x.OrderReqSigned.SequenceNumber == min_s))?.CommitCertificate.Response
                .History.Find(x => x.OrderReqSigned?.SequenceNumber == min_s);
                orderReqs.Add(ConstructNewHistoryMinS(orderReq));
            }
            computedHistory.AddRange(changeDict.Find(x => x.CommitCertificate?.Response.SequenceNumber == max_cc && x.CommitCertificate.Response.History.Exists(x=>min_s!=-1 || x.OrderReqSigned.SequenceNumber==(min_s+1)))?.CommitCertificate.Response
                .History.Where(x => (x.OrderReqSigned?.SequenceNumber > min_s) && x.OrderReqSigned?.SequenceNumber <= max_cc)
                    .Select(x => ConstructNewHistory(x)) ?? Enumerable.Empty<OrderReq>());
            var bhe = changeDict
                .Select(x => x.History.Where
                        (xx => xx.OrderReqSigned?.SequenceNumber >= max_cc && xx.OrderReqSigned?.SequenceNumber <= max_r).ToList())
                .ToList();
            computedHistory.AddRange(FilterUncommitedRequests(bhe, max_r, max_cc)
                .Select(x => ConstructNewHistory(x)));

            orderReqs.Clear();
            return computedHistory;
        }
        private OrderReq ConstructNewHistory(OrderReq x) => x switch
        {
            OrderReq<IPersonaRequest> xx => GenerateReq(xx),
            _ => throw new NotImplementedException()
        };

        private OrderReq<T> GenerateReq<T>(OrderReq<T> xx)
        {

            var signedReq = new OrderReqSigned(view, xx.OrderReqSigned.SequenceNumber, xx.OrderReqSigned.DigestRequest, DigestManager.DigestList(orderReqs.LastOrDefault()?.OrderReqSigned.DigestHistory??string.Empty, xx.OrderReqSigned.DigestRequest));
            var signature = EncryptionManager.SignMsg(signedReq.ToString(), privateKey);
            var request = new Request<T>(xx.Request.GetRequest(), xx.Request.Client, xx.Request.Timestamp)
            {
                Signature = xx.Request.Signature
            };
            var result = new OrderReq<T>(signedReq, request, signature);
            orderReqs.Add(result);
            return result;
        }

        private List<OrderReq> FilterUncommitedRequests(List<List<OrderReq>> viewChange, int max_r, int max_cc)
        {
            List<OrderReq> _filter(List<List<OrderReq>> viewChanges, List<OrderReq> result, int i, List<OrderReq> accumulatore) => viewChanges switch
            {
                (List<OrderReq> Head, List<List<OrderReq>> Tail) when i <= max_r => _filter(Tail, addReq(Head, result, i), i, accumulatore),
                _ when i <= max_r => _filter(viewChange, new List<OrderReq>(), i + 1, accumula(result, accumulatore)),
                _ => accumulatore
            };

            List<OrderReq> addReq(List<OrderReq> req, List<OrderReq> result, int i) => result.Append(req.Find(x => x.OrderReqSigned.SequenceNumber == i)).Where(x => x is not null).ToList();

            List<OrderReq> accumula(List<OrderReq> req, List<OrderReq> result)
            { 

                var casino = req.GroupBy(x => new { x.OrderReqSigned.SequenceNumber, x.OrderReqSigned.DigestHistory, x.OrderReqSigned.DigestRequest }).OrderByDescending(x => x.Count());
                var a = casino.First().First();
                var digestHistory = viewChange.SelectMany(x => x).FirstOrDefault(x => x.OrderReqSigned.SequenceNumber == a.OrderReqSigned.SequenceNumber - 1)?.OrderReqSigned.DigestHistory; 

                if (casino.FirstOrDefault().Count() >= maxFailures + 1 && (DigestManager.DigestList(string.Empty,a.OrderReqSigned.DigestRequest).Equals(a.OrderReqSigned.DigestHistory) || DigestManager.DigestList(digestHistory, a.OrderReqSigned.DigestRequest).Equals(a.OrderReqSigned.DigestHistory)))
                {
                    result.Add(casino.First().First());
                }
                return result;
            }

            return _filter(viewChange, new List<OrderReq>(), max_cc + 1, new List<OrderReq>());
        }
    }
}
