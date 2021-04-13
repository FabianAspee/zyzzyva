using System;
using System.Collections.Immutable;
using System.Linq;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/AbstractPersonaWithListResponse.xml" path='docs/members[@name="abstractPersonaWithList"]/AbstractPersonaWithListResponse/*'/>
    public abstract class AbstractPersonaWithListResponse : IPersonaResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/AbstractPersonaWithListResponse.xml" path='docs/members[@name="abstractPersonaWithList"]/SuccefulResponse/*'/>
        public readonly bool SuccefulResponse;
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/AbstractPersonaWithListResponse.xml" path='docs/members[@name="abstractPersonaWithList"]/Personas/*'/>
        public readonly ImmutableList<Persona> Persone;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/AbstractPersonaWithListResponse.xml" path='docs/members[@name="abstractPersonaWithList"]/AbstractPersonaWithListResponseMsg/*'/>
        public AbstractPersonaWithListResponse(ImmutableList<Persona> persone, bool statusOperation) => (Persone,SuccefulResponse) = (persone,statusOperation);

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is AbstractPersonaWithListResponse response &&
                response.Persone.SequenceEqual(Persone);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Persone);
        ///<inheritdoc/>
        public override string ToString() => string.Join(",", Persone.Select(x => x.ToString()).ToArray());

    }
}
