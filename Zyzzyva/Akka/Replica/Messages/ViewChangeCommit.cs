using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ViewChangeCommit.xml" path='docs/members[@name="viewchangecommit"]/ViewChangeCommit/*'/>
    public class ViewChangeCommit
    {
        /// <include file="Docs/Akka/Replica/Messages/ViewChangeCommit.xml" path='docs/members[@name="viewchangecommit"]/ViewChange/*'/>
        public readonly ViewChange ViewChange;
        /// <include file="Docs/Akka/Replica/Messages/ViewChangeCommit.xml" path='docs/members[@name="viewchangecommit"]/MyProof/*'/>
        public readonly Dictionary<IActorRef, IHateThePrimary> MyProof;
        /// <include file="Docs/Akka/Replica/Messages/ViewChangeCommit.xml" path='docs/members[@name="viewchangecommit"]/ViewChangeCommitC/*'/>
        public ViewChangeCommit(ViewChange viewChange, Dictionary<IActorRef, IHateThePrimary> myProof)
        {
            ViewChange = viewChange;
            MyProof = myProof.Select(x => x).ToDictionary(x => x.Key, x => x.Value);

        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is ViewChangeCommit commit &&
                   commit.ViewChange.Equals(ViewChange) &&
                   commit.MyProof.SequenceEqual(MyProof);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(ViewChange, MyProof);
    }
}
