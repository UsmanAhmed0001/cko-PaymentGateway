using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: "8877",
            ExpiryMonth: 4,
            ExpiryYear: 2027,
            Currency: "GBP",
            Amount: 100,
            AuthorizationCode: "test-auth-code");
        
        
        var paymentsRepository = new InMemoryPaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}