using System;
using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/ViewChange/*'/>
    public class ViewChange
    {
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/NewView/*'/>
        public readonly int NewView;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/SequenceNumber/*'/>
        public readonly int SequenceNumber;
        //public readonly ??? Checkpoint;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/CheckPointProof/*'/>
        public readonly List<Checkpoint> CheckPointProof;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/CommitCertificate/*'/>
        public readonly CommitCertificate CommitCertificate;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/History/*'/>
        public readonly List<OrderReq> History;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/ReplicaId/*'/>
        public readonly int ReplicaId;
        ///<inheritdoc/>
        public byte[] Signature;
        /// <include file="Docs/Akka/Replica/Messages/ViewChange.xml" path='docs/members[@name="viewchange"]/ViewChangeC/*'/>
        public ViewChange(int newView, int sequenceNumber, List<Checkpoint> checkPointProof, CommitCertificate commitCertificate, List<OrderReq> history, int replicaId)
        {
            NewView = newView;
            SequenceNumber = sequenceNumber;
            CheckPointProof = checkPointProof?.Select(x => x).ToList();
            CommitCertificate = commitCertificate is not null ? new CommitCertificate(commitCertificate?.Replica, commitCertificate?.Response) : commitCertificate;
            History = history;
            ReplicaId = replicaId;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ViewChange change &&
                   NewView == change.NewView &&
                   SequenceNumber == change.SequenceNumber && (change.CheckPointProof is null || change.CheckPointProof.SequenceEqual(CheckPointProof)) &&
                   (change.CommitCertificate is null || change.CommitCertificate.Equals(CommitCertificate)) &&
                   change.History.SequenceEqual(History) &&
                   ReplicaId == change.ReplicaId;

        ///<inheritdoc/>
        public override string ToString() => $"VIEW-CHANGE - CHECKPOINTPROOF - {PrintCheckDictionary} - {CommitCertificate} - {PrintOrderReq} REPLICA-ID - {ReplicaId} - NEW-VIEW - {NewView} - SEQUENCE-NUMBER - {SequenceNumber}";

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(NewView);
            hash.Add(SequenceNumber);
            hash.Add(CheckPointProof);
            hash.Add(CommitCertificate);
            hash.Add(History);
            hash.Add(ReplicaId);
            hash.Add(PrintCheckDictionary);
            hash.Add(PrintOrderReq);
            return hash.ToHashCode();
        }

        private string PrintCheckDictionary => string.Join(",", CheckPointProof?.Select(x => $"VALORE {x}").ToArray() ?? Array.Empty<string>());

        private string PrintOrderReq => string.Join(",", History.Select(x => $"{x}").ToArray());


    }
}
