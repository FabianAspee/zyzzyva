using System;
using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/DeletePersonaResponse.xml" path='docs/members[@name="deletepersona"]/DeletePersonaResponse/*'/>
    public class DeletePersonaResponse : AbstractPersonaWithListResponse

    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/DeletePersonaResponse.xml" path='docs/members[@name="deletepersona"]/DeletePersonaResp/*'/>
        public DeletePersonaResponse(ImmutableList<Persona> persone,bool statusOperation) : base(persone,statusOperation) { }

        ///<inheritdoc/>
        public override string ToString() => $"DELETE-RESPONSE - {base.ToString()}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is DeletePersonaResponse && base.Equals(obj);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Persone);
    }
}