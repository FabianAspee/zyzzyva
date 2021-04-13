using Grpc.Net.Client;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading;

namespace SMRView.Controller
{
    /// <include file="Docs/Controller/BaseController.xml" path='docs/members[@name="basecontroller"]/BaseController/*'/>
    public abstract class BaseController
    {

        /// <include file="Docs/Controller/BaseController.xml" path='docs/members[@name="basecontroller"]/channel/*'/>
        protected GrpcChannel channel = GrpcChannel.ForAddress(ConfigurationManager.ConnectionStrings["serverPath"].ConnectionString, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true

            }
        });

    }
}
