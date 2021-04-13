using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ZyzzyvagRPC
{
    /// <include file="Docs/Program.xml" path='docs/members[@name="program"]/Program/*'/>
    public class Program
    {
        /// <include file="Docs/Program.xml" path='docs/members[@name="program"]/Main/*'/>
        public static void Main(string[] args)
        {

            CreateHostBuilder(args).Build().Run();
        }
        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        /// <include file="Docs/Program.xml" path='docs/members[@name="program"]/CreateHostBuilder/*'/>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
