using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/FillHole/*'/>
    public class FillHole
    {
        /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/View/*'/>
        public readonly int View;
        /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/MaxSequenceNumber/*'/>
        public readonly int MaxSequenceNumber;
        /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/SequenceNumber/*'/>
        public readonly int SequenceNumber;
        /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/ReplicaId/*'/>
        public readonly int ReplicaId;
        ///<inheritdoc/>
        public byte[] Signature { get; set; }

        /// <include file="Docs/Akka/Replica/Messages/FillHole.xml" path='docs/members[@name="fillhole"]/FillHoleMsg/*'/>
        public FillHole(int view, int maxSequenceNumber, int sequenceNumber, int replicaId)
        {
            View = view;
            MaxSequenceNumber = maxSequenceNumber;
            SequenceNumber = sequenceNumber;
            ReplicaId = replicaId;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is FillHole hole &&
                   View == hole.View &&
                   MaxSequenceNumber == hole.MaxSequenceNumber &&
                   SequenceNumber == hole.SequenceNumber &&
                   ReplicaId == hole.ReplicaId;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(View, MaxSequenceNumber, SequenceNumber, ReplicaId);

        ///<inheritdoc/>
        public override string ToString() => $"FILLHOLE - {View} {MaxSequenceNumber} {SequenceNumber} {ReplicaId}";
    }
}
