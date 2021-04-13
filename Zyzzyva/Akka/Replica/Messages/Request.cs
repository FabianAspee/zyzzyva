using Akka.Actor;
using System;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/Request.xml" path='docs/members[@name="request"]/Request/*'/>
    public class Request<T> : IRequest<T>
    {
        private readonly T _request;
        private readonly IActorRef _client;
        private readonly int _timestamp;

        ///<inheritdoc/>
        public IActorRef Client => _client;

        ///<inheritdoc/>
        public int Timestamp => _timestamp;

        ///<inheritdoc/>
        public T GetRequest() => _request;

        ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/Request.xml" path='docs/members[@name="request"]/RequestMsg/*'/>
        public Request(T request, IActorRef client, int timestamp) => (_request, _client, _timestamp) = (request, client, timestamp);

        ///<inheritdoc/>
        public override string ToString() => $"REQUEST - { _client.Path.ToStringWithoutAddress()} {_timestamp} {_request}";

        ///<inheritdoc/>
        public override bool Equals(object obj) => obj is Request<T> request &&
                   request._request.Equals(_request) &&
                   request._client.Path.ToStringWithoutAddress().Equals(_client.Path.ToStringWithoutAddress()) &&
                   _timestamp == request._timestamp;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_request, _client, _timestamp, Client, Timestamp);
    }
}
