using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.Replica.Messages.Response;
using Zyzzyva.Database.Tables;

namespace ZyzzyvagRPC.Subscriber.EventArgument
{
    /// <include file="../../Docs/Subscriber/EventArgument/UpdateEventArgs.xml" path='docs/members[@name="updateeventargs"]/UpdateEventArgs/*'/>
    public class UpdateEventArgs : EventArgs
    {
        /// <include file="../../Docs/Subscriber/EventArgument/UpdateEventArgs.xml" path='docs/members[@name="updateeventargs"]/PersonaResult/*'/>
        public readonly ImmutableList<Persona> PersonaResult;
        /// <include file="../../Docs/Subscriber/EventArgument/UpdateEventArgs.xml" path='docs/members[@name="updateeventargs"]/ReplicasResult/*'/>
        public readonly Dictionary<int, ImmutableList<Persona>> ReplicasResult;
         /// <include file="../../Docs/Subscriber/EventArgument/InsertEventArgs.xml" path='docs/members[@name="inserteventargs"]/FinalStatus/*'/>
        public readonly bool FinalStatus;

        /// <include file="../../Docs/Subscriber/EventArgument/UpdateEventArgs.xml" path='docs/members[@name="updateeventargs"]/UpdateEventArgsC/*'/>
        public UpdateEventArgs(ReplicaResponse<IPersonaResponse> response) =>
        (ReplicasResult, PersonaResult, FinalStatus) = (response.ReplicaResponses.ToDictionary(x => x.Key, x => (x.Value as UpdatePersonaResponse).Persone), (response.Response as UpdatePersonaResponse).Persone, (response.Response as UpdatePersonaResponse).SuccefulResponse);
    }
}
