namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/POSTMORTEM.xml" path='docs/members[@name="postmortem"]/POSTMORTEM/*'/>
    public class POSTMORTEM
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/POSTMORTEM.xml" path='docs/members[@name="postmortem"]/Address/*'/>
        public readonly string Address;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/POSTMORTEM.xml" path='docs/members[@name="postmortem"]/POSTMORTEMC/*'/>
        public POSTMORTEM(string address) => Address = address;
    }
}
