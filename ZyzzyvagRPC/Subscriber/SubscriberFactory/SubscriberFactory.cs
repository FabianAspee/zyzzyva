using ZyzzyvagRPC.Subscriber.SubscriberContract;
using ZyzzyvagRPC.Subscriber.SubscriberImplementation;

namespace ZyzzyvagRPC.Subscriber.SubscriberFactory
{
    /// <include file="../../Docs/Subscriber/SubscriberFactory/SubscriberFactory.xml" path='docs/members[@name="subscriberfactory"]/SubscriberFactory/*'/>
    public class SubscriberFactory : ISubscriberFactory
    {
        /// <inheritdoc/>
        public IPersonSubscriber GetPersonSubscriber() => new PersonaSubscriber();
    }
}
