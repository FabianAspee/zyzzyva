using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Zyzzyva.Database.Tables;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.Replica.Messages.Response;
using System.Linq;
namespace ZyzzyvagRPC.Subscriber.EventArgument
{
    /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/InsertEventArgs/*'/>
    public class InsertEventArgs : EventArgs
    {
        /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/PersonaResult/*'/>
        public readonly ImmutableList<Persona> PersonaResult;
        /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/ReplicasResult/*'/>
        public readonly Dictionary<int, ImmutableList<Persona>> ReplicasResult;
       /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/FinalStatus/*'/>
        public readonly bool FinalStatus;
         /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/InsertEventArgsC/*'/>
        public InsertEventArgs(ReplicaResponse<IPersonaResponse> response) => 
        (ReplicasResult, PersonaResult,FinalStatus) = (response.ReplicaResponses.ToDictionary(x => x.Key, x => (x.Value as InsertPersonaResponse).Persone), (response.Response as InsertPersonaResponse).Persone, (response.Response as InsertPersonaResponse).SuccefulResponse);
    }
}
