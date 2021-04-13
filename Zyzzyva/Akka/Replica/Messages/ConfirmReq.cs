using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ConfirmReq.xml" path='docs/members[@name="confirmreq"]/ConfirmReq/*'/>
    public class ConfirmReq<T> where T : IRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/ConfirmReq.xml" path='docs/members[@name="confirmreq"]/view/*'/>
        public readonly int view;
        /// <include file="Docs/Akka/Replica/Messages/ConfirmReq.xml" path='docs/members[@name="confirmreq"]/msg/*'/>
        public readonly T msg;
        /// <include file="Docs/Akka/Replica/Messages/ConfirmReq.xml" path='docs/members[@name="confirmreq"]/myId/*'/>
        public readonly int myId;
        ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/ConfirmReq.xml" path='docs/members[@name="confirmreq"]/ConfirmReqMsg/*'/>
        public ConfirmReq(int view, T msg, int myId)
        {
            this.view = view;
            this.msg = msg;
            this.myId = myId;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ConfirmReq<T> req &&
                   view == req.view &&
                   req.msg.Equals(msg) &&
                   myId == req.myId;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(view, msg, myId);

        ///<inheritdoc/>
        public override string ToString() => $"CONFIRM-REQ - {view} {msg} {myId}";
    }
}
