using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Dynamic;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

[assembly: WebJobsStartup(typeof(Cloudy.Startup))]

namespace Cloudy
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder) 
            => Configure(builder?.Services ?? throw new ArgumentNullException(nameof(builder)), new Environment());

#pragma warning disable CA1822 // Mark members as static. We'll need this for tests
        internal void Configure(IServiceCollection services, IEnvironment env)
        {
            services.AddLogging(builder => builder.AddConsole());

            if (!env.IsDevelopment())
            {
                //Add ApplicationInsights in production only
                services.AddLogging(builder => builder.AddApplicationInsights(env.GetVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
                services.AddApplicationInsightsTelemetry();
            }
            else
            {
                // To satisfy direct dependencies on the client.
                services.AddSingleton(_ => new TelemetryConfiguration(Guid.NewGuid().ToString("n")));
                services.AddSingleton(x => new TelemetryClient(x.GetRequiredService<TelemetryConfiguration>()));
            }

            if (env.IsDevelopment())
                services.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            else
                services.AddSingleton(CloudStorageAccount.Parse(env.GetVariable("AzureWebJobsStorage")));

            // Explicit registrationss:
            services.AddSingleton(env);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.RegisterServices(Assembly.GetExecutingAssembly(), typeof(DomainObject).Assembly);
        }
    }
}
