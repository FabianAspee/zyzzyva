using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/OrderReq/*'/>
    public class OrderReq
    {
        /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/OrderReqSigned/*'/>
        public readonly OrderReqSigned OrderReqSigned;

        ///<inheritdoc/>
        public readonly byte[] Signature;

        /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/OrderReqMsg/*'/>
        public OrderReq(OrderReqSigned orderReqSigned, byte[] signature)
        {
            OrderReqSigned = orderReqSigned;
            Signature = signature;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is OrderReq orderReq &&
                    orderReq.OrderReqSigned.Equals(OrderReqSigned);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(OrderReqSigned);

        ///<inheritdoc/>
        public override string ToString() => $"ORDER-REQ - {OrderReqSigned}";

    }
    /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/OrderReqT/*'/>
    public class OrderReq<T> : OrderReq
    {

        /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/IRequest/*'/>
        public readonly IRequest<T> Request;

        /// <include file="Docs/Akka/Replica/Messages/OrderReq.xml" path='docs/members[@name="orderreq"]/OrderReqMsgT/*'/>
        public OrderReq(OrderReqSigned orderReq, IRequest<T> request, byte[] signature) : base(orderReq, signature) => Request = request;

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is OrderReq<T> req &&
                   req.OrderReqSigned.Equals(OrderReqSigned) &&
                   req.Request.Equals(Request);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), OrderReqSigned, Request);

        ///<inheritdoc/>
        public override string ToString() => $"ORDER-REQ<T> - {base.ToString()} {Request}";


    }
}
