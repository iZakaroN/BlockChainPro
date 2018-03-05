using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace BlockChanPro.Web.Api
{
    public class Host
    {
        public static void Main(string[] args)
        {
            BuildWebHost().Run();
        }

        public static IWebHost BuildWebHost(string host = null) =>
            WebHost.CreateDefaultBuilder()
				.UseUrls(host)
                .UseStartup<Startup>()
                .Build();
    }
}
