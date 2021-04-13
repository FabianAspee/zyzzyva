using System;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/UpdatePersona.xml" path='docs/members[@name="update"]/UpdatePersona/*'/>
    public class UpdatePersona : IPersonaRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/UpdatePersona.xml" path='docs/members[@name="update"]/Persona/*'/>
        public readonly Persona Persona;



        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/UpdatePersona.xml" path='docs/members[@name="update"]/UpdatePersonaMsg/*'/>
        public UpdatePersona(Persona persona) => Persona = persona;

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is UpdatePersona persona &&
                   persona.Persona.Equals(Persona);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Persona);

        ///<inheritdoc/>
        public override string ToString() => $"UPDATE - {Persona}";

    }
}
