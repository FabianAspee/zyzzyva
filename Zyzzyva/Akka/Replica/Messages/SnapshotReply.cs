using System.Collections.Generic;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/SnapshotReply.xml" path='docs/members[@name="snapshotreply"]/SnapshotReply/*'/>
    public class SnapshotReply
    {
        /// <include file="Docs/Akka/Replica/Messages/SnapshotReply.xml" path='docs/members[@name="snapshotreply"]/StreamReader/*'/>
        public readonly Dictionary<int, Persona> StreamReader;

        /// <include file="Docs/Akka/Replica/Messages/SnapshotReply.xml" path='docs/members[@name="snapshotreply"]/SnapshotReplyMsg/*'/>
        public SnapshotReply(Dictionary<int, Persona> streamReader) => StreamReader = streamReader;
    }
}
