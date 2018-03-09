using System;
using System.Threading;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Engine.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlockChanPro.Web.Api
{
    public class Startup
    {
	    private static IP2PNetwork _network;
	    private static IEngine _engine;
	    private static Func<string, LogLevel, bool> _consoleLogFilter;
	    private static Action<IApplicationLifetime> _applicationLifetimeCreated;

		public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


		public static void Initialize(
		    IP2PNetwork network, 
		    IEngine engine, 
		    Func<string, LogLevel, bool> consoleLogFilter,
		    Action<IApplicationLifetime> applicationLifetimeCreated)
	    {
		    _network = network;
		    _engine = engine;
		    _consoleLogFilter = consoleLogFilter;
		    _applicationLifetimeCreated = applicationLifetimeCreated;
	    }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
	        services.Add(new ServiceDescriptor(typeof(IP2PNetwork), _network));
	        services.Add(new ServiceDescriptor(typeof(IEngine), _engine));
	        
			services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
	        _applicationLifetimeCreated(applicationLifetime);

			loggerFactory.AddConsole(_consoleLogFilter, false);
	        loggerFactory.AddDebug(_consoleLogFilter);
			if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
