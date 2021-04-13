using System;
using Zyzzyva.Database.Tables;
using ZyzzyvagRPC.Subscriber.EventArgument;

namespace ZyzzyvagRPC.Subscriber.SubscriberContract
{
    /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/IPersonSubscriber/*'/>
    public interface IPersonSubscriber : ISubscriber
    {

        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/ReadEvent/*'/>
        event EventHandler<ReadEventArgs> ReadEvent;
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/ReadAllEvent/*'/>
        event EventHandler<ReadAllEventArgs> ReadAllEvent;
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/InsertEvent/*'/>
        event EventHandler<InsertEventArgs> InsertEvent;
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/UpdateEvent/*'/>
        event EventHandler<UpdateEventArgs> UpdateEvent;
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/DeleteEvent/*'/>
        event EventHandler<DeleteEventArgs> DeleteEvent;
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/SetByzantineEvent/*'/>
        event EventHandler<ByzantineEventArgs> SetByzantineEvent;

        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/Read/*'/>
        void Read(int id);
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/ReadAll/*'/>
        void ReadAll();
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/Insert/*'/>
        void Insert(Persona persona);
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/Update/*'/>
        void Update(Persona persona);
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/Delete/*'/>
        void Delete(int id);
        /// <include file="../../Docs/Subscriber/SubscriberContract/IPersonSubscriber.xml" path='docs/members[@name="ipersonasubscriber"]/SetByzantine/*'/>
        void SetByzantine(int id);
    }
}
