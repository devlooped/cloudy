using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    /// <summary>
    /// Provides automatic service registration based on conventions, rather 
    /// than explicit registration.
    /// </summary>
    static class ServiceConvention
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services) => RegisterServices(services, Assembly.GetExecutingAssembly());

        public static IServiceCollection RegisterServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            var delegateType = typeof(Delegate);

            // DI convention-based scanning and registration
            // 1. Candidates: types that implement at least one interface
            // 2. Looking at its attributes: [ServiceLifetime], [SkipServiceConvention], [ServiceEnvironment]
            // 3. If no interfaces, register if it has [ServiceLifetime] or [ServiceEnvironment]
            var candidateTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsClass &&
                    !delegateType.IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    // Omit explicitly opted-out components
                    t.GetCustomAttribute<SkipServiceConventionAttribute>(true) == null &&
                    // Omit generated types like local state capturing
                    t.GetCustomAttribute<CompilerGeneratedAttribute>() == null &&
                    // Omit generated types for async state machines
                    !t.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine)))
                .Where(t => t.GetInterfaces().Length > 0 ||
                    t.GetCustomAttribute<ServiceLifetimeAttribute>() != null ||
                    t.GetCustomAttribute<ServiceEnvironmentAttribute>() != null);

            // Filter out services that don't target the current environment.
            // See https://github.com/Azure/azure-functions-host/issues/4491#issuecomment-500966160 
            // On why there are two variables.
            var environment = System.Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (string.IsNullOrEmpty(environment))
                environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environment))
                environment = "Production";

            candidateTypes = candidateTypes.Where(t =>
                t.GetCustomAttribute<ServiceEnvironmentAttribute>() == null ||
                environment.Equals(t.GetCustomAttribute<ServiceEnvironmentAttribute>()!.Environment, StringComparison.Ordinal));

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

            return services;
        }
    }
}
