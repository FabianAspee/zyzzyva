using System;
using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Replica.Messages
{
    //public class CommitCertificate:IViewChangeCC { }
    //public class CommitCertificate<T>:CommitCertificate
    /// <include file="Docs/Akka/Replica/Messages/CommitCertificate.xml" path='docs/members[@name="commitcertificate"]/CommitCertificate/*'/>
    public class CommitCertificate
    {
        /// <include file="Docs/Akka/Replica/Messages/CommitCertificate.xml" path='docs/members[@name="commitcertificate"]/Replica/*'/>
        public readonly List<(int, byte[])> Replica;
        /// <include file="Docs/Akka/Replica/Messages/CommitCertificate.xml" path='docs/members[@name="commitcertificate"]/Response/*'/>
        public readonly SpecResponseSigned Response;

        /// <include file="Docs/Akka/Replica/Messages/CommitCertificate.xml" path='docs/members[@name="commitcertificate"]/CommitCertificateMsg/*'/>
        public CommitCertificate(List<(int, byte[])> replica, SpecResponseSigned response)
        {
            Replica = replica.Select(x => x).ToList();
            Response = new SpecResponseSigned(response, response.History);
        }

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is CommitCertificate certificate &&
                   certificate.Replica.SequenceEqual(Replica) &&
                   certificate.Response.Equals(Response);

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Replica, Response);

        ///<inheritdoc/>
        public override string ToString() => $"COMMIT-CERTIFICATE - REPLICHE {PrintReplicas} - {Response}";

        private string PrintReplicas => string.Join(",", Replica.Select(x => $"REPLICA-ID {x.Item1}").ToArray());

    }
}
