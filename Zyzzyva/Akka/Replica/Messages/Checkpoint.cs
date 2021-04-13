using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/Checkpoint/*'/>
    public class Checkpoint
    {
        /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/SequenceNumber/*'/>
        public readonly int SequenceNumber;
        /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/DigestHistory/*'/>
        public readonly string DigestHistory;
        //public readonly string StateApplication;
        /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/ReplicaId/*'/>
        public readonly int ReplicaId;
        /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/Signature/*'/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/Checkpoint.xml" path='docs/members[@name="checkpoint"]/CheckpointMsg/*'/>
        public Checkpoint(int sequenceNumber, string digestHistory, int myId)
        {
            SequenceNumber = sequenceNumber;
            DigestHistory = digestHistory;
            ReplicaId = myId;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is Checkpoint checkpoint &&
                   SequenceNumber == checkpoint.SequenceNumber &&
                   DigestHistory == checkpoint.DigestHistory &&
                   ReplicaId == checkpoint.ReplicaId;

        ///<inheritdoc/>
        public override string ToString() => $"CHECKPOINT - {SequenceNumber} {DigestHistory}";

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(SequenceNumber, DigestHistory, ReplicaId);
    }
}
