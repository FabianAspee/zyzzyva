using System;

namespace Zyzzyva.Akka.Replica.Messages
{

    /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/OrderReqSigned/*'/>
    public class OrderReqSigned
    {
        /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/View/*'/>
        public readonly int View;

        /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/SequenceNumber/*'/>
        public readonly int SequenceNumber;

        /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/DigestRequest/*'/>
        public readonly string DigestRequest;

        /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/DigestHistory/*'/>
        public readonly string DigestHistory;


        /// <include file="Docs/Akka/Replica/Messages/OrderReqSigned.xml" path='docs/members[@name="ordereqsigned"]/OrderReqSignedMsg/*'/>
        public OrderReqSigned(int view, int sequenceNumber, string digestRequest, string digestHistory)
        {
            View = view;
            SequenceNumber = sequenceNumber;
            DigestRequest = digestRequest;
            DigestHistory = digestHistory;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is OrderReqSigned signed &&
                   View == signed.View &&
                   SequenceNumber == signed.SequenceNumber &&
                   DigestRequest == signed.DigestRequest &&
                   DigestHistory == signed.DigestHistory;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(View, SequenceNumber, DigestRequest, DigestHistory);

        ///<inheritdoc/>
        public override string ToString() => $"ORDER-REQ-SIGNED {View} {SequenceNumber} {DigestRequest} {DigestHistory}"; 
        ///<inheritdoc/>
        public string GetStringDigest() => $"ORDER-REQ-SIGNED {SequenceNumber} {DigestRequest}";

    }

}
