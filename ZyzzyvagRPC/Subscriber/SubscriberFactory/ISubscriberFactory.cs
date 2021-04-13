using ZyzzyvagRPC.Subscriber.SubscriberContract;
using ZyzzyvagRPC.Subscriber.SubscriberImplementation;
namespace ZyzzyvagRPC.Subscriber.SubscriberFactory
{
    /// <include file="../../Docs/Subscriber/SubscriberFactory/ISubscriberFactory.xml" path='docs/members[@name="isubscriberfactory"]/ISubscriberFactory/*'/>
    public interface ISubscriberFactory
    {
        /// <include file="../../Docs/Subscriber/SubscriberFactory/ISubscriberFactory.xml" path='docs/members[@name="isubscriberfactory"]/GetPersonSubscriber/*'/>
        IPersonSubscriber GetPersonSubscriber();
    }

}
