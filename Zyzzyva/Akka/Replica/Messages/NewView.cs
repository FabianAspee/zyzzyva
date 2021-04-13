using System.Collections.Generic;
using System.Linq;

namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/NewView.xml" path='docs/members[@name="newview"]/NewView/*'/>
    public class NewView
    {
        /// <include file="Docs/Akka/Replica/Messages/NewView.xml" path='docs/members[@name="newview"]/View/*'/>
        public readonly int View;
        /// <include file="Docs/Akka/Replica/Messages/NewView.xml" path='docs/members[@name="newview"]/ViewChange/*'/>
        public readonly List<ViewChange> ViewChange;
        /// <include file="Docs/Akka/Replica/Messages/NewView.xml" path='docs/members[@name="newview"]/History/*'/>
        public readonly List<OrderReq> History;
        ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/Replica/Messages/NewView.xml" path='docs/members[@name="newview"]/NewViewMsg/*'/>
        public NewView(int view, List<ViewChange> viewChange, List<OrderReq> history)
        {
            View = view;
            ViewChange = viewChange;
            History = history;
        }
        ///<inheritdoc/>
        public override string ToString() => $"NEWVIEW - {View} {PrintViewChange} {PrintHistory}";
        private string PrintViewChange => $"{string.Join("", ViewChange.Select(x => $"{x}").ToArray())}";
        private string PrintHistory => $"{string.Join("", History.Select(x => $"{x}").ToArray())}";
    }
}
