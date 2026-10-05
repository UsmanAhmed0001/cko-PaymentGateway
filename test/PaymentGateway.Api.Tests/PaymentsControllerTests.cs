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
using Microsoft.AspNetCore.TestHost;



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
        // Arrange: a payment stored in the test's own repository
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

        // Put the test's repository on the IPaymentsRepository "shelf", after the app's own
        var client = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse.Id);
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