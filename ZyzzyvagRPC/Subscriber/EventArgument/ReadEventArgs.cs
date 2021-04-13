using System;
using System.Collections.Generic;
using System.Linq;
using Zyzzyva.Akka.Client.Messages.ResponseToApplication;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Akka.Replica.Messages.Response;
using Zyzzyva.Database.Tables;

namespace ZyzzyvagRPC.Subscriber.EventArgument
{
    /// <include file="../../Docs/Subscriber/EventArgument/ReadEventArgs.xml" path='docs/members[@name="readeventargs"]/ReadAllEventArgs/*'/>
    public class ReadEventArgs : EventArgs
    {
        /// <include file="../../Docs/Subscriber/EventArgument/ReadEventArgs.xml" path='docs/members[@name="readeventargs"]/PersonaResult/*'/>
        public readonly Persona PersonaResult;

        /// <include file="../../Docs/Subscriber/EventArgument/ReadEventArgs.xml" path='docs/members[@name="readeventargs"]/ReplicasResult/*'/>
        public readonly Dictionary<int, Persona> ReplicasResult;
        /// <include file="../../Docs/Subscriber/EventArgument/ReadEventArgs.xml" path='docs/members[@name="readeventargs"]/ReadEventArgsC/*'/>
        public ReadEventArgs(ReplicaResponse<IPersonaResponse> response) =>
        (ReplicasResult, PersonaResult) = (response.ReplicaResponses.ToDictionary(x => x.Key, x => (x.Value as ReadPersonaResponse).Persona), (response.Response as ReadPersonaResponse).Persona);
    }
}

