using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ProofOfMisbehaviour.xml" path='docs/members[@name="proofofmisbehaviour"]/ProofOfMisbehaviour/*'/>
    public class ProofOfMisbehaviour
    {
        /// <include file="Docs/Akka/Replica/Messages/ProofOfMisbehaviour.xml" path='docs/members[@name="proofofmisbehaviour"]/View/*'/>
        public readonly int View;
        /// <include file="Docs/Akka/Replica/Messages/ProofOfMisbehaviour.xml" path='docs/members[@name="proofofmisbehaviour"]/spectTuple/*'/>
        public readonly (OrderReq, OrderReq) spectTuple;
        ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/ProofOfMisbehaviour.xml" path='docs/members[@name="proofofmisbehaviour"]/ProofOfMisbehaviourMsg/*'/>
        public ProofOfMisbehaviour(int view, (OrderReq, OrderReq) spectTuple)
        {
            this.View = view;
            this.spectTuple = spectTuple;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ProofOfMisbehaviour misbehaviour &&
                   View == misbehaviour.View &&
                   spectTuple.Equals(misbehaviour.spectTuple);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(View, spectTuple);
        ///<inheritdoc/> 
        public bool VerifiedProof() => spectTuple.Item1.OrderReqSigned.DigestRequest==spectTuple.Item2.OrderReqSigned.DigestRequest && 
                                        spectTuple.Item1.OrderReqSigned.View == spectTuple.Item2.OrderReqSigned.View &&
                                        (spectTuple.Item1.OrderReqSigned.SequenceNumber!= spectTuple.Item2.OrderReqSigned.SequenceNumber || 
                                        spectTuple.Item1.OrderReqSigned.DigestHistory != spectTuple.Item2.OrderReqSigned.DigestHistory);

        ///<inheritdoc/>
        public override string ToString() => $"PROOF-OF-MISBEHAVIOUR {View} {spectTuple.Item1} {spectTuple.Item2}";
    }
}
