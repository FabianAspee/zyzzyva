using System;
using ZyzzyvagRPC.Subscriber.SubscriberImplementation;
using ZyzzyvaRPC.ClusterClientAccess;
namespace ZyzzyvagRPC.Subscriber
{
    /// <include file="../Docs/Subscriber/ISubscriber.xml" path='docs/members[@name="isubscriber"]/ISubscriber/*'/>
    public interface ISubscriber : IDisposable
    {
        /// <include file="../Docs/Subscriber/ISubscriber.xml" path='docs/members[@name="isubscriber"]/CreateActor/*'/> 
        void CreateActor();
    }
}
