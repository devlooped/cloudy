using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: WebJobsStartup(typeof(Cloudy.Startup))]

namespace Cloudy
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            Configure(builder?.Services ?? throw new ArgumentNullException(nameof(builder)), new Environment());
        }

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

            if (env.IsDevelopment())
                services.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            else
                services.AddSingleton(CloudStorageAccount.Parse(env.GetVariable("AzureWebJobsStorage")));

            // Explicit registrationss:
            services.AddSingleton(env);

            // DI convention-based scanning and registration
            // 1. Candidates: types that implement at least one interface
            // 2. Looking at its attributes: if they don't have [Shared], they are registered as transient
            // 3. Optionally can have [Export] to force registration of a type without interfaces
            var candidateTypes = typeof(DomainObject).Assembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    !t.IsValueType &&
                    // Omit explicitly opted-out components
                    t.GetCustomAttribute<SkipServiceScanAttribute>(true) == null &&
                    // Omit generated types like local state capturing
                    t.GetCustomAttribute<CompilerGeneratedAttribute>() == null &&
                    // Omit generated types for async state machines
                    !t.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine)))
                .Where(t => t.GetInterfaces().Length > 0 || t.GetCustomAttribute<ServiceLifetimeAttribute>() != null);

            foreach (var implementationType in candidateTypes)
            {
                var lifetime = implementationType.GetCustomAttribute<ServiceLifetimeAttribute>()?.Lifetime ?? ServiceLifetime.Scoped;
                Func<Type, Type, IServiceCollection> addInterface = lifetime switch
                {
                    ServiceLifetime.Scoped => services.AddScoped,
                    ServiceLifetime.Singleton => services.AddSingleton,
                    ServiceLifetime.Transient => services.AddTransient,
                    _ => throw new NotSupportedException(),
                };

                // Register each of the implemented interfaces
                foreach (var serviceType in implementationType.GetInterfaces())
                {
                    addInterface(serviceType, implementationType);
                }

                // And also the concrete type
                _ = lifetime switch
                {
                    ServiceLifetime.Scoped => services.AddScoped(implementationType),
                    ServiceLifetime.Singleton => services.AddSingleton(implementationType),
                    ServiceLifetime.Transient => services.AddTransient(implementationType),
                    _ => throw new NotSupportedException(),
                };
            }
        }
    }
}
