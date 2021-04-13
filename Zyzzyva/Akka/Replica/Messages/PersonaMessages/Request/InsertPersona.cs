using System;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/InsertPersona.xml" path='docs/members[@name="insert"]/InsertPersona/*'/>
    public class InsertPersona : IPersonaRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/InsertPersona.xml" path='docs/members[@name="insert"]/Persona/*'/>
        public readonly Persona Persona;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/InsertPersona.xml" path='docs/members[@name="insert"]/InsertPersonaMsg/*'/>
        public InsertPersona(Persona persona) => Persona = persona;

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is InsertPersona persona &&
                   persona.Persona.Equals(Persona);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Persona);

        ///<inheritdoc/>
        public override string ToString() => $"INSERT - {Persona}";

    }
}
