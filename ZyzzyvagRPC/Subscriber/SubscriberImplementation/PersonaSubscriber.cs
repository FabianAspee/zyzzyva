using Akka.Actor;
using System;
using Zyzzyva.Akka.Client.Messages;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.Replica.Messages.Response;
using Zyzzyva.Database.Tables;
using ZyzzyvagRPC.Subscriber.EventArgument;
using ZyzzyvagRPC.Subscriber.SubscriberContract;
using ZyzzyvaRPC.ClusterClientAccess;

namespace ZyzzyvagRPC.Subscriber.SubscriberImplementation
{

    /// <include file="../../Docs/Subscriber/SubscriberImplementation/PersonaSubscriber.xml" path='docs/members[@name="personasubscriber"]/PersonaSubscriber/*'/> 
    public class PersonaSubscriber : AbstractSubscriber, IPersonSubscriber
    {
        ///<inheritdoc/>
        public event EventHandler<ReadEventArgs> ReadEvent;
        ///<inheritdoc/>
        public event EventHandler<ReadAllEventArgs> ReadAllEvent;
        ///<inheritdoc/>
        public event EventHandler<InsertEventArgs> InsertEvent;
        ///<inheritdoc/>
        public event EventHandler<UpdateEventArgs> UpdateEvent;
        ///<inheritdoc/>
        public event EventHandler<DeleteEventArgs> DeleteEvent;
        ///<inheritdoc/>
        public event EventHandler<ByzantineEventArgs> SetByzantineEvent;

        /// <inheritdoc/>
        public override void CreateActor() => _actor = ClusterClientAccess.CreateActor(PersonaActor.MyProps(this, ReadEvent, ReadAllEvent, InsertEvent, UpdateEvent, DeleteEvent, SetByzantineEvent));

        /// <inheritdoc/>
        public void Delete(int id) => _actor.Tell(new DeletePersona(id));

        /// <inheritdoc/>
        public void Insert(Persona persona) => _actor.Tell(new InsertPersona(persona));

        /// <inheritdoc/>
        public void Read(int id) => _actor.Tell(new ReadPersona(id));
        /// <inheritdoc/>
        public void ReadAll() => _actor.Tell(new ReadAllPersona());

        /// <inheritdoc/>
        public void Update(Persona persona) => _actor.Tell(new UpdatePersona(persona));

        ///<inheritdoc/>
        public void SetByzantine(int id) => _actor.Tell(new SetByzantine(id));


        private class PersonaActor : ReceiveActor, IWithUnboundedStash
        {
            public event EventHandler<ReadEventArgs> ReadEvent;
            public event EventHandler<ReadAllEventArgs> ReadAllEvent;
            public event EventHandler<InsertEventArgs> InsertEvent;
            public event EventHandler<UpdateEventArgs> UpdateEvent;
            public event EventHandler<DeleteEventArgs> DeleteEvent;
            public event EventHandler<ByzantineEventArgs> SetByzantineEvent;
            private readonly PersonaSubscriber _personaSubscriber;

            private IActorRef myClient;

            public IStash Stash { get; set; }

            public PersonaActor(PersonaSubscriber persona, EventHandler<ReadEventArgs> readEvent, EventHandler<ReadAllEventArgs> readAllEvent, EventHandler<InsertEventArgs> insertEvent,
                EventHandler<UpdateEventArgs> updateEvent, EventHandler<DeleteEventArgs> deleteEvent, EventHandler<ByzantineEventArgs> byzantineEvent)
            {
                (ReadEvent, ReadAllEvent, InsertEvent, UpdateEvent, DeleteEvent, SetByzantineEvent) = (readEvent, readAllEvent, insertEvent, updateEvent, deleteEvent, byzantineEvent);
                _personaSubscriber = persona;

                Receive<CreateClientResponse>(x =>
                {
                    myClient = x._actor;
                    Stash.UnstashAll();
                    Become(ActiveActor);
                });

                ReceiveAny(_ => Stash.Stash());

            }

            private void ActiveActor()
            {


                Receive<DeletePersona>(x => myClient.Tell(x, Self));
                Receive<InsertPersona>(x => myClient.Tell(x, Self));
                Receive<ReadPersona>(x => myClient.Tell(x, Self));
                Receive<ReadAllPersona>(x => myClient.Tell(x, Self));
                Receive<UpdatePersona>(x => myClient.Tell(x, Self));
                Receive<SetByzantine>(x => myClient.Tell(x, Self)); 
                Receive<ReplicaResponse<IPersonaResponse>>(operation => operation.Response is InsertPersonaResponse, response => InsertEvent?.Invoke(_personaSubscriber, new InsertEventArgs(response)));
                Receive<ReplicaResponse<IPersonaResponse>>(operation => operation.Response is UpdatePersonaResponse, response=> UpdateEvent?.Invoke(_personaSubscriber, new UpdateEventArgs(response)));
                Receive<ReplicaResponse<IPersonaResponse>>(operation => operation.Response is DeletePersonaResponse, response => DeleteEvent?.Invoke(_personaSubscriber, new DeleteEventArgs(response)));
                Receive<ReplicaResponse<IPersonaResponse>>(operation => operation.Response is ReadPersonaResponse, response => ReadEvent?.Invoke(_personaSubscriber, new ReadEventArgs(response)));
                Receive<ReplicaResponse<IPersonaResponse>>(operation => operation.Response is ReadAllPersonaResponse, response => ReadAllEvent?.Invoke(_personaSubscriber, new ReadAllEventArgs(response)));
                Receive<SetByzantineResponse>(response => SetByzantineEvent?.Invoke(_personaSubscriber, new ByzantineEventArgs(response.Byzantine)));
            } 
            
            public static Props MyProps(PersonaSubscriber persona, EventHandler<ReadEventArgs> readEvent, EventHandler<ReadAllEventArgs> readAllEvent, EventHandler<InsertEventArgs> insertEvent,
                EventHandler<UpdateEventArgs> updateEvent, EventHandler<DeleteEventArgs> deleteEvent, EventHandler<ByzantineEventArgs> byzantineEvent) => Props.Create(() => new PersonaActor(persona, readEvent, readAllEvent, insertEvent, updateEvent, deleteEvent, byzantineEvent));
        }
    }
}
