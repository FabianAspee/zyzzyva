namespace Zyzzyva.Akka.ZyzzyvaManager.Messages
{
    /// <include file="Docs/Akka/ZyzzyvaManager/Messages/FinalNewView.xml" path='docs/members[@name="finalnewview"]/FinalNewView/*'/>
    public class FinalNewView
    {
        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/FinalNewView.xml" path='docs/members[@name="finalnewview"]/View/*'/>
        public readonly int View;

        /// <include file="Docs/Akka/ZyzzyvaManager/Messages/FinalNewView.xml" path='docs/members[@name="finalnewview"]/FinalNewViewC/*'/>
        public FinalNewView(int view) => View = view;
    }
}
