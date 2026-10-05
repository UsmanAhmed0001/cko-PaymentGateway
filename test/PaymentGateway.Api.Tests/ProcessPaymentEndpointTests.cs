using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Api.Bank;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Tests;

public class ProcessPaymentEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly object ValidRequest = new
    {
        cardNumber = "2222405343248877",
        expiryMonth = 4,
        expiryYear = 2030,
        currency = "GBP",
        amount = 100,
        cvv = "123"
    };
    
    private static HttpClient CreateClient(FakeBankClient bank) =>
        new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IAcquiringBankClient>(bank)))
            .CreateClient();
    
    [Fact]
    public async Task AuthorizedPaymentReturns201AndCanBeRetrieved()
    {
        var client = CreateClient(new FakeBankClient(new BankAuthorizationResult(true, "abc")));

        var response = await client.PostAsJsonAsync("/api/payments", ValidRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        Assert.Equal(PaymentStatus.Authorized, payment!.Status);
        Assert.Equal("8877", payment.CardNumberLastFour);

        var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeclinedPaymentReturns201WithDeclinedStatus()
    {
        var client = CreateClient(new FakeBankClient(new BankAuthorizationResult(false, "")));

        var response = await client.PostAsJsonAsync("/api/payments", ValidRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        Assert.Equal(PaymentStatus.Declined, payment!.Status);
    }
    
    [Fact]
    public async Task InvalidRequestReturns400RejectedWithoutCallingTheBank()
    {
        var bank = new FakeBankClient(new BankAuthorizationResult(true, "abc"));
        var client = CreateClient(bank);

        var response = await client.PostAsJsonAsync("/api/payments", new { cardNumber = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"status\":\"Rejected\"", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, bank.CallCount);
    }
    
    [Fact]
    public async Task MalformedJsonReturns400Rejected()
    {
        var client = CreateClient(new FakeBankClient(new BankAuthorizationResult(true, "abc")));
        var body = new StringContent("""{"amount":"abc"}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/payments", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"status\":\"Rejected\"", await response.Content.ReadAsStringAsync());
    }
    
    [Fact]
    public async Task BankUnavailableReturns502()
    {
        var client = CreateClient(new FakeBankClient(null));

        var response = await client.PostAsJsonAsync("/api/payments", ValidRequest);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

}