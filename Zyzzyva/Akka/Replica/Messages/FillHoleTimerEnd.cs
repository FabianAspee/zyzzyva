using Zyzzyva.Akka.Replica.Messages;

namespace Zyzzyva.Akka.Replica
{
    /// <include file="Docs/Akka/Replica/Messages/FillHoleTimerEnd.xml" path='docs/members[@name="fillholetimerend"]/FillHoleTimerEnd/*'/>
    public class FillHoleTimerEnd
    {
        /// <include file="Docs/Akka/Replica/Messages/FillHoleTimerEnd.xml" path='docs/members[@name="fillholetimerend"]/FillHole/*'/>
        public readonly FillHole FillHole;

        /// <include file="Docs/Akka/Replica/Messages/FillHoleTimerEnd.xml" path='docs/members[@name="fillholetimerend"]/FillHoleTimerEndMsg/*'/>
        public FillHoleTimerEnd(FillHole fillHole) => FillHole = fillHole;

        ///<inheritdoc/>
        public override string ToString() => $"{FillHole}";
    }
}
