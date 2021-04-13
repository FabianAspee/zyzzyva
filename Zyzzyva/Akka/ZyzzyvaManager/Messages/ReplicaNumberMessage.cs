namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaNumberMessage.xml" path='docs/members[@name="replicanumbermessage"]/ReplicaNumberMessage/*'/>
    public class ReplicaNumberMessage
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaNumberMessage.xml" path='docs/members[@name="replicanumbermessage"]/Id/*'/>
        public readonly int Id;
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaNumberMessage.xml" path='docs/members[@name="replicanumbermessage"]/View/*'/>
        public readonly int View;
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/ReplicaNumberMessage.xml" path='docs/members[@name="replicanumbermessage"]/ReplicaNumberMessageC/*'/>
        public ReplicaNumberMessage(int id, int view) => (Id, View) = (id, view);
    }
}
