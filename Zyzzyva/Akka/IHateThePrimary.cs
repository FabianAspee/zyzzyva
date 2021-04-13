namespace Zyzzyva.Akka
{
    /// <include file="Docs/Akka/IHateThePrimary.xml" path='docs/members[@name="ihatetheprimary"]/IHateThePrimary/*'/>
    public class IHateThePrimary
    {
        /// <include file="Docs/Akka/IHateThePrimary.xml" path='docs/members[@name="ihatetheprimary"]/View/*'/>
        public readonly int View;
       ///<inheritdoc/>
        public byte[] Signature { get; set; }
        /// <include file="Docs/Akka/IHateThePrimary.xml" path='docs/members[@name="ihatetheprimary"]/IHateThePrimaryC/*'/>
        public IHateThePrimary(int view)=>View = view;
        
        ///<inheritdoc/>
        public override string ToString() => $"I-HATE-THE-PRIMARY {View}";
    }
}
