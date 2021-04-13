using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using Zyzzyva.Security;
using System.Security.Cryptography;
 
namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/SpecResponseSigned/*'/>
    public class SpecResponseSigned
    {

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/View/*'/>
        public readonly int View;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/SequenceNumber/*'/>
        public readonly int SequenceNumber;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/History/*'/>
        public readonly List<OrderReq> History;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/DigestResponse/*'/>
        public readonly string DigestResponse;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/Client/*'/>
        public readonly IActorRef Client;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/Timestamp/*'/>
        public readonly int Timestamp;

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/SpecResponseSignedC/*'/>
        public SpecResponseSigned(int view, int sequenceNumber, string digestResponse, IActorRef client, int timestamp)
        {
            View = view;
            SequenceNumber = sequenceNumber;
            DigestResponse = digestResponse;
            Client = client;
            Timestamp = timestamp;
        }

        /// <include file="Docs/Akka/Replica/Messages/SpecResponseSigned.xml" path='docs/members[@name="specresponsesigned"]/SpecResponseSignedC2/*'/>
        public SpecResponseSigned(SpecResponseSigned old, List<OrderReq> history) : this(old.View, old.SequenceNumber, old.DigestResponse, old.Client, old.Timestamp) =>
            History = history.Select(t => t).ToList();//SANTO SIGNORE

        ///<inheritdoc/>
        public override string ToString() => $"SPEC-RESPONSE-SIGNED {View} {SequenceNumber} {DigestResponse} {History.LastOrDefault()?.OrderReqSigned.DigestHistory ?? ""} {Timestamp} {Client.Path.ToStringWithoutAddress()}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is SpecResponseSigned SpecResponse &&
                 SpecResponse.View.Equals(View) &&
                 SpecResponse.SequenceNumber.Equals(SequenceNumber) &&
                 SpecResponse.DigestResponse.Equals(DigestResponse) &&
                 SpecResponse.History.SequenceEqual(History) &&
                 SpecResponse.Timestamp.Equals(Timestamp) &&
                 SpecResponse.Client.Path.ToStringWithoutAddress().Equals(Client.Path.ToStringWithoutAddress());

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(View, SequenceNumber, History, DigestResponse, Client, Timestamp);
    }
}