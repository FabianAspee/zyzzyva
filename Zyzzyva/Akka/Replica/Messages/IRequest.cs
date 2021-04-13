
using Akka.Actor;

namespace Zyzzyva.Akka.Replica.Messages
{

    /// <include file="Docs/Akka/Replica/Messages/IRequest.xml" path='docs/members[@name="irequest"]/IRequest/*'/>
    public interface IRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/IRequest.xml" path='docs/members[@name="irequest"]/Client/*'/>
        public IActorRef Client { get; }
        /// <include file="Docs/Akka/Replica/Messages/IRequest.xml" path='docs/members[@name="irequest"]/Timestamp/*'/>
        public int Timestamp { get; }

        ///<inheritdoc/>
        public byte[] Signature { get; set; }
    }
    /// <include file="Docs/Akka/Replica/Messages/IRequest.xml" path='docs/members[@name="irequest"]/IRequestT/*'/>
    public interface IRequest<T> : IRequest
    {
        /// <include file="Docs/Akka/Replica/Messages/IRequest.xml" path='docs/members[@name="irequest"]/GetRequest/*'/>
        public T GetRequest();
    }
}
