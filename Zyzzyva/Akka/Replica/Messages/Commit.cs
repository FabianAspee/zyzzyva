using Akka.Actor;
using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/Commit.xml" path='docs/members[@name="commit"]/Commit/*'/>
    public class Commit
    {
        /// <include file="Docs/Akka/Replica/Messages/Commit.xml" path='docs/members[@name="commit"]/Client/*'/>
        public readonly IActorRef Client;
        /// <include file="Docs/Akka/Replica/Messages/Commit.xml" path='docs/members[@name="commit"]/CommitCertificate/*'/>
        public readonly CommitCertificate CommitCertificate;
        ///<inheritdoc/>
        public byte[] Signature { get; set; }


        /// <include file="Docs/Akka/Replica/Messages/Commit.xml" path='docs/members[@name="commit"]/CommitMsg/*'/>
        public Commit(IActorRef client, CommitCertificate commitCertificate)
        {
            Client = client;
            CommitCertificate = commitCertificate;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is Commit commit &&
                   commit.Client.Path.ToStringWithoutAddress().Equals(Client.Path.ToStringWithoutAddress()) &&
                   commit.CommitCertificate.Equals(CommitCertificate);

        ///<inheritdoc/>
        public override string ToString() => $"COMMIT {CommitCertificate} CLIENT {Client.Path.ToStringWithoutAddress()}";

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Client, CommitCertificate);
    }
}
