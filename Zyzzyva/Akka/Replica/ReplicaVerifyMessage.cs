using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Zyzzyva.Akka.Replica.Messages;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Security;

namespace Zyzzyva.Akka.Replica
{
    public partial class ReplicaManager : ReceiveActor
    {
        private bool VerifyRequest<T>(IRequest<T> msg1) => clientKeys.TryGetValue(Sender,out RSAParameters key) && EncryptionManager.VerifySignature(msg1.Signature, DigestManager.GenerateSHA512String(msg1.ToString()), key);
        
        private bool ConfirmReq(ConfirmReq<IRequest> confirmReq, RSAParameters key)=> clientKeys.TryGetValue(confirmReq.msg.Client, out RSAParameters publicKey) && confirmReq.msg is IRequest<IPersonaRequest> &&
        EncryptionManager.VerifySignature(confirmReq.msg.Signature, DigestManager.GenerateSHA512String(confirmReq.msg.ToString()), publicKey) &&
        EncryptionManager.VerifySignature(confirmReq.Signature, DigestManager.GenerateSHA512String(confirmReq.ToString()), key);
         
        private bool VerifyConfirmReq(ConfirmReq<IRequest> confirmReq)=> replicas.TryGetValue(confirmReq.myId, out (ActorRefRoutee, RSAParameters) infoRep) ?
        ConfirmReq(confirmReq, infoRep.Item2) : ConfirmReq(confirmReq, privateKey);
        
        private bool VerifyOrderReq<T>(OrderReq<T> req)=>req.OrderReqSigned.View==view  &&
            EncryptionManager.VerifySignature(req.Signature, DigestManager.GenerateSHA512String(req.OrderReqSigned.ToString()), primary.Item2);
        

        private bool VerifyFillHole(FillHole req) =>replicas.TryGetValue(req.ReplicaId,out (ActorRefRoutee,RSAParameters) key) && EncryptionManager.VerifySignature(req.Signature, DigestManager.GenerateSHA512String(req.ToString()), key.Item2);

        private bool VerifyProofMisbehaviour(ProofOfMisbehaviour proofOf) => proofOf.VerifiedProof()  && proofOf.View == view && (clientKeys.TryGetValue(Sender, out RSAParameters key) ?
                    EncryptionManager.VerifySignature(proofOf.Signature, DigestManager.GenerateSHA512String(proofOf.ToString()), key) : (SelectKey(Sender) is RSAParameters keyR &&
                    EncryptionManager.VerifySignature(proofOf.Signature, DigestManager.GenerateSHA512String(proofOf.ToString()), keyR))) &&
                    EncryptionManager.VerifySignature(proofOf.spectTuple.Item1.Signature, DigestManager.GenerateSHA512String(proofOf.spectTuple.Item1.OrderReqSigned.ToString()), primary.Item2) &&
                    EncryptionManager.VerifySignature(proofOf.spectTuple.Item2.Signature, DigestManager.GenerateSHA512String(proofOf.spectTuple.Item2.OrderReqSigned.ToString()), primary.Item2);
                    
                     
         
           
        private readonly Func<OrderReq, int, bool> IfFattorizzato = (msg, view) => msg.OrderReqSigned.View == view;

        private readonly Func<OrderReq, List<OrderReq>, bool> IfFattorizzato2 = (msg, history) => (msg.OrderReqSigned.SequenceNumber == ((history.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? -1) + 1) && msg.OrderReqSigned.DigestHistory.Equals(DigestManager.DigestList(history.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty, msg.OrderReqSigned.DigestRequest)));

        private readonly Func<OrderReq, List<OrderReq>, bool> IfFattorizzato3 = (msg, history) => msg.OrderReqSigned.SequenceNumber > ((history.LastOrDefault()?.OrderReqSigned.SequenceNumber ?? -1) + 1);

        private bool VerifyOrderFillHole(OrderReqFillHole req) => IfFattorizzato(req.OrderReq, view) && req.OrderReq switch
        {
            OrderReq<IPersonaRequest> x => VerifyOrderReq(x),
            _ => throw new NotImplementedException()
        }; 
       
        private RSAParameters SelectKey(IActorRef Sender) => replicas.Values.ToList().Exists(x => x.Item1.Actor.Equals(Sender)) ? replicas.Values.FirstOrDefault(x => x.Item1.Actor.Equals(Sender)).Item2 : privateKey;
        
        private bool VerifyHatePrimary(IHateThePrimary hateThePrimary) => hateThePrimary.View == view && SelectKey(Sender) is RSAParameters key &&
                    EncryptionManager.VerifySignature(hateThePrimary.Signature, DigestManager.GenerateSHA512String(hateThePrimary.ToString()), key);

        private static bool VerifyRequest(OrderReq req) => req switch
        {
            OrderReq<IPersonaRequest> request => request.OrderReqSigned.DigestRequest == DigestManager.GenerateSHA512String(request.Request.ToString()),
            _ => throw new NotImplementedException()
        };
        private bool VerifyBothViewChange(ViewChangeCommit viewChangeCommit) => viewChangeCommit.ViewChange.History.All(x => VerifySignatureViewCHange(x.OrderReqSigned.View % (3 * maxFailures + 1), x.Signature, x.OrderReqSigned.ToString()) && VerifyRequest(x)) && replicas.TryGetValue(viewChangeCommit.ViewChange.ReplicaId, out (ActorRefRoutee, RSAParameters) key) ?
               EncryptionManager.VerifySignature(viewChangeCommit.ViewChange.Signature, DigestManager.GenerateSHA512String(viewChangeCommit.ViewChange.ToString()), key.Item2) : EncryptionManager.VerifySignature(viewChangeCommit.ViewChange.Signature, DigestManager.GenerateSHA512String(viewChangeCommit.ViewChange.ToString()), primary.Item2) &&
               viewChangeCommit.MyProof.All(x => EncryptionManager.VerifySignature(x.Value.Signature, DigestManager.GenerateSHA512String(x.Value.ToString()), SelectKey(x.Key)));
        
        private bool VerifyViewChange(ViewChangeCommit viewChangeCommit) => viewChangeCommit.ViewChange.NewView > view && VerifyBothViewChange(viewChangeCommit);
        
        private bool VerifyEqualViewChange(ViewChangeCommit viewChangeCommit) => viewChangeCommit.ViewChange.NewView == view && VerifyBothViewChange(viewChangeCommit);
        
        private bool VerifyGreaterViewChange(ViewChangeCommit viewChangeCommit) => viewChangeCommit.ViewChange.NewView > view && VerifyBothViewChange(viewChangeCommit);
        
        private bool VerifyNewView(NewView newView) => newView.View == view && view % (replicas.Count + 1) == myId ? EncryptionManager.VerifySignature(newView.Signature, DigestManager.GenerateSHA512String(newView.ToString()), privateKey) :
        replicas.TryGetValue(view % (replicas.Count + 1), out (ActorRefRoutee, RSAParameters) key) &&
        EncryptionManager.VerifySignature(newView.Signature, DigestManager.GenerateSHA512String(newView.ToString()), key.Item2);
        
        private static (int, int, string, int, string, string) Metodinos<T>(SpecResponse<T> arg) => (arg.SpecResponseSigned.SequenceNumber, arg.OrderReqSigned.View, arg.SpecResponseSigned.Client.Path.ToStringWithoutAddress(), arg.SpecResponseSigned.Timestamp, arg.SpecResponseSigned.DigestResponse, arg.SpecResponseSigned.History.LastOrDefault()?.OrderReqSigned.DigestHistory ?? string.Empty); //digest??

        private bool VerifySignatureViewCHange(int replicaId, byte[] signature, string classString) => replicas.TryGetValue(replicaId, out (ActorRefRoutee, RSAParameters) actor) ?
        EncryptionManager.VerifySignature(signature, DigestManager.GenerateSHA512String(classString), actor.Item2) : EncryptionManager.VerifySignature(signature,
        DigestManager.GenerateSHA512String(classString), privateKey);
        private bool VerifySignature(int replicaId, byte[] signature, string classString) => replicas.TryGetValue(replicaId, out (ActorRefRoutee, RSAParameters) actor) ?
        EncryptionManager.VerifySignature(signature, DigestManager.GenerateSHA512String(classString), actor.Item2) : replicaId == actualPrimary && EncryptionManager.VerifySignature(signature,
        DigestManager.GenerateSHA512String(classString), primary.Item2);
        
        private bool VerifySpecResponseCheckPoint(SpecResponse<string> spec) => VerifySignature(spec.ReplicaId, spec.Signature, spec.SpecResponseSigned.ToString());
        
        private bool VerifyCheckPoint(Checkpoint checkpoint) => VerifySignature(checkpoint.ReplicaId, checkpoint.Signature, checkpoint.ToString());
        
        private (int, string) CheckCheckpoint(Checkpoint arg) => (arg.SequenceNumber, arg.DigestHistory);
        
        private bool VerifyCommit(Commit commit) => commit.CommitCertificate.Response.View == view && EncryptionManager.VerifySignature(commit.Signature, DigestManager.GenerateSHA512String(commit.ToString()), clientKeys[commit.Client]);
        
        private bool VerifyCC(CommitCertificate commitCertificate) => commitCertificate.Replica.All(x => x.Item1 == myId ? EncryptionManager.VerifySignature(x.Item2, DigestManager
                .GenerateSHA512String(commitCertificate.Response.ToString()), privateKey) : replicas.TryGetValue(x.Item1, out (ActorRefRoutee, RSAParameters) actor) ?
                EncryptionManager.VerifySignature(x.Item2, DigestManager.GenerateSHA512String(commitCertificate.Response.ToString()), actor.Item2) : x.Item1 == actualPrimary && EncryptionManager.VerifySignature(x.Item2, DigestManager
                .GenerateSHA512String(commitCertificate.Response.ToString()), primary.Item2));

        private void CreateRouter() => replicaRouter = new Router(new BroadcastRoutingLogic(), replicas.Select(x => x.Value.Item1).ToArray());
        
        private void CancelTimer<T>(OrderReq<T> msg) => Timers.Cancel(msg.Request.ToString());
    }
}
