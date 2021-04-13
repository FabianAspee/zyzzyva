using System;

namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/ReadPersona.xml" path='docs/members[@name="read"]/ReadPersona/*'/>
    public class ReadPersona : IPersonaRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/ReadPersona.xml" path='docs/members[@name="read"]/Id/*'/>
        public readonly int Id;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/ReadPersona.xml" path='docs/members[@name="read"]/ReadPersonaMsg/*'/>
        public ReadPersona(int id) => Id = id;

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ReadPersona persona &&
                   Id == persona.Id;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id);

        ///<inheritdoc/>
        public override string ToString() => $"READ - {Id}";

    }
}
