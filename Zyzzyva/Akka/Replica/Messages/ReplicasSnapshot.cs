using System.Collections.Generic;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ReplicasSnapshot.xml" path='docs/members[@name="replicassnapshot"]/ReplicasSnapshot/*'/>
    class ReplicasSnapshot
    {
        /// <include file="Docs/Akka/Replica/Messages/ReplicasSnapshot.xml" path='docs/members[@name="replicassnapshot"]/ReplicasId/*'/>
        public List<int> ReplicasId;

        /// <include file="Docs/Akka/Replica/Messages/ReplicasSnapshot.xml" path='docs/members[@name="replicassnapshot"]/ReplicasSnapshotMsg/*'/>
        public ReplicasSnapshot(List<int> replicasId) => ReplicasId = replicasId;

    }
}
