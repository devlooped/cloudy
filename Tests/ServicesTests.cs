using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cloudy
{
    public class ServicesTests
    {
        readonly ITestOutputHelper output;

        public ServicesTests(ITestOutputHelper output) => this.output = output;

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

        [Fact]
        public async Task GetRequestAddsEndToEndTracing()
        {
            using var config = new TelemetryConfiguration(new Environment().GetVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));
            var telemetry = new ActivityTelemetry(new TelemetryClient(config), new Serializer());

            var activity = telemetry.ActivitySource.StartActivity(nameof(GetRequestAddsEndToEndTracing), ActivityKind.Client).Start();

            try
            {
                var http = new HttpClient();
                var builder = new StringBuilder();

                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:7071/inbox?message=heya!");
                builder = builder.Append(request.Method.ToString().ToUpperInvariant()).Append(' ').AppendLine(request.RequestUri.ToString());
                foreach (var header in request.Headers)
                {
                    builder = builder.Append(header.Key).Append(": ").AppendLine(string.Join(' ', header.Value));
                }

                var response = await http.SendAsync(request);
                builder = builder
                    .AppendLine()
                    .AppendLine(response.StatusCode.ToString());

                foreach (var header in response.Headers)
                {
                    builder = builder.Append(header.Key).Append(": ").AppendLine(string.Join(' ', header.Value));
                }

                builder = builder
                    .AppendLine()
                    .Append(await response.Content.ReadAsStringAsync());

                output.WriteLine(builder.ToString());
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
