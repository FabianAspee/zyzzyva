using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/SpecResponse/*'/>
    public class SpecResponse<T> : ISpecResponse<T>
    { 
        private readonly T _response;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/ReplicaId/*'/>
        public readonly int ReplicaId;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/OrderReqSigned/*'/>
        public readonly OrderReqSigned OrderReqSigned;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/SpecResponseSigned/*'/>
        public readonly SpecResponseSigned SpecResponseSigned;

        ///<inheritdoc/>
        public readonly byte[] Signature;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/OrderReqSignature/*'/>
        public readonly byte[] OrderReqSignature;


        /// <include file="Docs/Akka/Replica/Messages/SpecResponse.xml" path='docs/members[@name="specresponse"]/SpecResponseC/*'/>
        public SpecResponse(T response, OrderReqSigned orderReq, SpecResponseSigned specResponse, byte[] orderSignature, int replicaId = -1, byte[] signature = null)
        {
            _response = response;
            ReplicaId = replicaId;
            OrderReqSigned = orderReq;
            SpecResponseSigned = specResponse;
            Signature = signature;
            OrderReqSignature = orderSignature;
        }
        ///<inheritdoc/>
        public T GetResponse() => _response;

        ///<inheritdoc/>
        public override string ToString() => $"SPEC-RESPONSE {_response} {OrderReqSigned} {SpecResponseSigned} {ReplicaId}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is SpecResponse<T> response &&
                   response._response.Equals(_response) &&
                   ReplicaId == response.ReplicaId &&
                   response.OrderReqSigned.Equals(OrderReqSigned) &&
                   response.SpecResponseSigned.Equals(SpecResponseSigned);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_response, ReplicaId, OrderReqSigned, SpecResponseSigned);
    }
}
