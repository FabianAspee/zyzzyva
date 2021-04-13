namespace Zyzzyva.Akka.Replica.Messages.PersonaMessages.Response
{
    /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/SetByzantineResponse.xml" path='docs/members[@name="byzantine"]/SetByzantineResponse/*'/>
    public class SetByzantineResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/SetByzantineResponse.xml" path='docs/members[@name="byzantine"]/Byzantine/*'/>
        public readonly bool Byzantine;

        /// <include file="Docs/Akka/Replica/Messages/PersonaMessages/Response/SetByzantineResponse.xml" path='docs/members[@name="byzantine"]/SetByzantineResponseMsg/*'/>
        public SetByzantineResponse(bool byzantine) => Byzantine = byzantine;
    }
}
