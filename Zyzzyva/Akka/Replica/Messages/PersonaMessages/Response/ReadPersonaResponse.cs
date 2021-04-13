using System;
using Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadPersonaResponse.xml" path='docs/members[@name="readpersona"]/ReadPersonaResponse/*'/>
    public class ReadPersonaResponse : IPersonaResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadPersonaResponse.xml" path='docs/members[@name="readpersona"]/Persona/*'/>
        public readonly Persona Persona;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/ReadPersonaResponse.xml" path='docs/members[@name="readpersona"]/ReadPersonaResp/*'/>
        public ReadPersonaResponse(Persona persona) => Persona = persona;

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ReadPersonaResponse response &&
                   response.Persona.Equals(Persona);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Persona);

        ///<inheritdoc/>
        public override string ToString() => $"READ-RESPONSE {Persona}";

    }
}
