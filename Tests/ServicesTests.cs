using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cloudy
{
    public class ServicesTests
    {
        [Fact]
        public void WhenResolvingEventStream_ThenGetsInstanceFromScope()
        {
            var services = new ServiceCollection();
            new Startup().Configure(services, new Environment());
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            var scopes = provider.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopes.CreateScope();
            var stream = scope.ServiceProvider.GetRequiredService<IEventStream>();

            Assert.NotNull(stream);
        }
    }
}
