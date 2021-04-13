using System;

namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/DeletePersona.xml" path='docs/members[@name="delete"]/DeletePersona/*'/>
    public class DeletePersona : IPersonaRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/DeletePersona.xml" path='docs/members[@name="delete"]/Persona/*'/>
        public readonly int Id;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/DeletePersona.xml" path='docs/members[@name="delete"]/DeletePersonaMsg/*'/>
        public DeletePersona(int id) => Id = id;
        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is DeletePersona persona &&
                   Id == persona.Id;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id);

        ///<inheritdoc/>
        public override string ToString() => $"DELETE - {Id}";
    }
}
