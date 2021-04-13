namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/ReadAllPersona.xml" path='docs/members[@name="readall"]/ReadAllPersona/*'/>
    public class ReadAllPersona : IPersonaRequest
    {
        private static readonly int HASH_CODE = 1;
        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ReadAllPersona;

        ///<inheritdoc/>
        public override string ToString() => $"READALL - ";

        ///<inheritdoc/>
        public override int GetHashCode() => HASH_CODE;
    }
}
