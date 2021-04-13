using System.Collections.Generic;

namespace Zyzzyva.Akka.Client.Messages.ResponseToApplication
{
    /// <include file="Docs/Akka/Client/Messages/ResponseToApplication/ReplicaResponse.xml" path='docs/members[@name="replicaresponse"]/ReplicaResponse/*'/>
    public class ReplicaResponse<T>
    {
        /// <include file="Docs/Akka/Client/Messages/ResponseToApplication/ReplicaResponse.xml" path='docs/members[@name="replicaresponse"]/ReplicaResponses/*'/>
        public readonly Dictionary<int, T> ReplicaResponses;
        /// <include file="Docs/Akka/Client/Messages/ResponseToApplication/ReplicaResponse.xml" path='docs/members[@name="replicaresponse"]/Response/*'/>
        public readonly T Response;
       
        /// <include file="Docs/Akka/Client/Messages/ResponseToApplication/ReplicaResponse.xml" path='docs/members[@name="replicaresponse"]/ReplicaResponseC/*'/>
        public ReplicaResponse(Dictionary<int, T> replicaResponses, T response) => (ReplicaResponses, Response) = (replicaResponses, response);
    }
}
