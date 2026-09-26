
using System.Net;
using System.Text.Json;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.WebApi.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Services;

public class PollyResilienceOrderTest
{
    [Fact]
    public async Task FlakyRequest_ShouldSucceedOnRetry_WhenPollyWrapsSimulationHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<PaymentApiSimulationHandler>();

        var pb = services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.external-payment-gateway.com/");
        });

        pb.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(50);
        });

        pb.AddHttpMessageHandler<PaymentApiSimulationHandler>();

        var sp = services.BuildServiceProvider();
        var gateway = sp.GetRequiredService<IPaymentGateway>();

        var result = await gateway.ProcessPaymentAsync(new PaymentRequest(
            Guid.NewGuid(), 100, "TRY", "1234", "flaky"));

        result.IsSuccess.Should().BeTrue();
    }
}
