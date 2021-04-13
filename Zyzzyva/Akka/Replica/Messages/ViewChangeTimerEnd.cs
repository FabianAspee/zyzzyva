namespace Zyzzyva.Akka.Replica.Messages
{
    /// <include file="Docs/Akka/Replica/Messages/ViewChangeTimerEnd.xml" path='docs/members[@name="viewchangetimerend"]/ViewChangeTimerEnd/*'/>
    public class ViewChangeTimerEnd
    {
        /// <include file="Docs/Akka/Replica/Messages/ViewChangeTimerEnd.xml" path='docs/members[@name="viewchangetimerend"]/View/*'/>
        public readonly int View;

        /// <include file="Docs/Akka/Replica/Messages/ViewChangeTimerEnd.xml" path='docs/members[@name="viewchangetimerend"]/ViewChangeTimerEndC/*'/>
        public ViewChangeTimerEnd(int view)=> View = view;
        
    }
}
