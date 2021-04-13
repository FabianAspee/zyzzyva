using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/LocalCommit/*'/>
    public class LocalCommit
    {
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/View/*'/>
        public readonly int View;
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/DigestRequest/*'/>
        public readonly string DigestRequest;
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/History/*'/>
        public readonly List<OrderReq> History;
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/Id/*'/>
        public readonly int Id;
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/Client/*'/>
        public readonly IActorRef Client;

        ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/LocalCommit.xml" path='docs/members[@name="localcommit"]/LocalCommitMsg/*'/>
        public LocalCommit(int view, string digestRequest, List<OrderReq> history, int id, IActorRef client)
        {
            View = view;
            DigestRequest = digestRequest;
            History = history;
            Id = id;
            Client = client;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is LocalCommit commit &&
                   View == commit.View &&
                   DigestRequest == commit.DigestRequest &&
                   commit.History.SequenceEqual(History) &&
                   Id == commit.Id &&
                    commit.Client.Path.ToStringWithoutAddress().Equals(Client.Path.ToStringWithoutAddress());

        ///<inheritdoc/>
        public override string ToString() => $"LOCAL-COMMIT - HISTORY {PrintHistory} - VIEW {View} - DIGEST-REQUEST {DigestRequest} - ID {Id} - CLIENT {Client.Path.ToStringWithoutAddress()}";

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(View, DigestRequest, History, Id, Client, PrintHistory);

        private string PrintHistory => string.Join(",", History.Select(x => $"{x}").ToArray());
    }
}
