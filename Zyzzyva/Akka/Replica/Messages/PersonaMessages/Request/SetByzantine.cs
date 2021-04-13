namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Request
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/SetByzantine.xml" path='docs/members[@name="byzantine"]/Byzantine/*'/>
    public class SetByzantine
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/SetByzantine.xml" path='docs/members[@name="byzantine"]/Id/*'/>
        public readonly int Id;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Request/SetByzantine.xml" path='docs/members[@name="byzantine"]/SetByzantine/*'/>
        public SetByzantine(int id) => Id = id;
    }
}
