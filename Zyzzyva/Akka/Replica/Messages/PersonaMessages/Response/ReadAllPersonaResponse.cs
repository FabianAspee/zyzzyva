using System;
using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadAllPersonaResponse.xml" path='docs/members[@name="readpersona"]/ReadAllPersonaResponse/*'/>
    public class ReadAllPersonaResponse : AbstractPersonaWithListResponse
    {


        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadAllPersonaResponse.xml" path='docs/members[@name="readpersona"]/ReadAllPersonaResp/*'/>
        public ReadAllPersonaResponse(ImmutableList<Persona> persone,bool statusOperation) : base(persone,statusOperation) { }

        ///<inheritdoc/>
        public override string ToString() => $"READ-ALL-RESPONSE - {base.ToString()}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ReadAllPersonaResponse && base.Equals(obj);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Persone);
    }
}