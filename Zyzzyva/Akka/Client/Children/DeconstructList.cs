using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Client.Children
{
    /// <include file="Docs/Akka/Client/Children/DeconstructList.xml" path='docs/members[@name="deconstructlist"]/DeconstructList/*'/>
    public static class DeconstructList
    {
        /// <include file="Docs/Akka/Client/Children/DeconstructList.xml" path='docs/members[@name="deconstructlist"]/Deconstruct/*'/>
        public static void Deconstruct<T>(this List<T> list, out T head, out List<T> tail)
        {
            head = list.FirstOrDefault();
            tail = new List<T>(list.Skip(1));
        }
        
    }
}
