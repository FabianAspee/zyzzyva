using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZyzzyvagRPC.Services;
using ZyzzyvagRPC.Subscriber.SubscriberFactory;

namespace ZyzzyvagRPC
{
    /// <include file="Docs/Startup.xml" path='docs/members[@name="startup"]/Startup/*'/>
    public class Startup
    {
        /// <include file="Docs/Startup.xml" path='docs/members[@name="startup"]/ConfigureServices/*'/>
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddGrpc();
            services.AddSingleton<ISubscriberFactory, SubscriberFactory>();
        }

        /// <include file="Docs/Startup.xml" path='docs/members[@name="startup"]/Configure/*'/>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapGrpcService<DataBaseService>();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
