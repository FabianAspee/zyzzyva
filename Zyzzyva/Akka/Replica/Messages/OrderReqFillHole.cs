namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/OrderReqFillHole.xml" path='docs/members[@name="orderreqfillhole"]/OrderReqFillHole/*'/>
    public class OrderReqFillHole
    {
        /// <include file="Docs/Akka/Replica/Messages/OrderReqFillHole.xml" path='docs/members[@name="orderreqfillhole"]/OrderReq/*'/>
        public OrderReq OrderReq;

        /// <include file="Docs/Akka/Replica/Messages/OrderReqFillHole.xml" path='docs/members[@name="orderreqfillhole"]/OrderReqFillHoleMsg/*'/>
        public OrderReqFillHole(OrderReq orderReq)
        {
            OrderReq = orderReq;
        }
    }
}
