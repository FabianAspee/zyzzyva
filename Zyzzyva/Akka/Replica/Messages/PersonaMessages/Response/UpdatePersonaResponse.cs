using System;
using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/UpdatePersonaResponse.xml" path='docs/members[@name="updatepersona"]/UpdatePersonaResponse/*'/>
    public class UpdatePersonaResponse : AbstractPersonaWithListResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadAllPersonaResponse.xml" path='docs/members[@name="readpersona"]/ReadAllPersonaResp/*'/>
        public UpdatePersonaResponse(ImmutableList<Persona> persone,bool statusOperation) : base(persone,statusOperation) { }

        ///<inheritdoc/>
        public override string ToString() => $"UPDATE-RESPONSE - {base.ToString()}";
        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is UpdatePersonaResponse && base.Equals(obj);
        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Persone);
    }
}