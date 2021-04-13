namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ISpecResponse.xml" path='docs/members[@name="ispecresponse"]/ISpecResponse/*'/>
    public interface ISpecResponse
    {
    }
    /// <include file="Docs/Akka/Replica/Messages/ISpecResponse.xml" path='docs/members[@name="ispecresponse"]/ISpecResponseT/*'/>
    public interface ISpecResponse<T> : ISpecResponse
    {
        /// <include file="Docs/Akka/Replica/Messages/ISpecResponse.xml" path='docs/members[@name="ispecresponse"]/GetResponse/*'/>
        public T GetResponse();
    }
}
