using System;
using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/InsertPersonaResponse.xml" path='docs/members[@name="insertpersona"]/InsertPersonaResponse/*'/>
    public class InsertPersonaResponse : AbstractPersonaWithListResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/InsertPersonaResponse.xml" path='docs/members[@name="insertpersona"]/InsertPersonaResp/*'/>
        public InsertPersonaResponse(ImmutableList<Persona> persone,bool statusOperation) : base(persone,statusOperation) { }

        ///<inheritdoc/>
        public override string ToString() => $"INSERT-RESPONSE - {base.ToString()}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is InsertPersonaResponse && base.Equals(obj);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Persone);
    }
}
