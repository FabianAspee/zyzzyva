using System;

namespace ZyzzyvagRPC.Subscriber.EventArgument
{
    /// <include file="../../Docs/Subscriber/EventArgument/ByzantineEventArgs.xml" path='docs/members[@name="byzantineeventargs"]/ByzantineEventArgs/*'/>
    public class ByzantineEventArgs : EventArgs
    {

        /// <include file="../../Docs/Subscriber/EventArgument/ByzantineEventArgs.xml" path='docs/members[@name="byzantineeventargs"]/SetByzantineResponse/*'/>
        public readonly bool SetByzantineResponse;

        /// <include file="../../Docs/Subscriber/EventArgument/ByzantineEventArgs.xml" path='docs/members[@name="byzantineeventargs"]/ByzantineEventArgsC/*'/>
        public ByzantineEventArgs(bool setByzantineResponse) => SetByzantineResponse = setByzantineResponse;
    }
}
