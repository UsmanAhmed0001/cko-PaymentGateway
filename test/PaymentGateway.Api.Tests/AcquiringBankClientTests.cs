using System.Net;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentGateway.Api.Bank;
namespace PaymentGateway.Api.Tests;

public class AcquiringBankClientTests
{
    private static readonly BankAuthorizationRequest Request = new(
        CardNumber: "2222405343248877",
        ExpiryMonth: 4,
        ExpiryYear: 2027,
        Currency: "GBP",
        Amount: 100,
        Cvv: "012"
    );

    private static AcquiringBankClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://bank.test/") },
            NullLogger<AcquiringBankClient>.Instance);
    
    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SendsTheRequestInTheBanksFormat()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"authorized":true,"authorization_code":"abc"}"""));

        await CreateClient(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.Equal("http://bank.test/payments", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"card_number\":\"2222405343248877\"", handler.LastRequestBody);
        Assert.Contains("\"expiry_date\":\"04/2027\"", handler.LastRequestBody); // month zero-padded
        Assert.Contains("\"cvv\":\"012\"", handler.LastRequestBody);              // leading zero kept
    }
    
    [Fact]
    public async Task ReturnsAuthorizedWithTheAuthorizationCode()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"authorized":true,"authorization_code":"abc"}"""));

        var result = await CreateClient(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Authorized);
        Assert.Equal("abc", result.AuthorizationCode);
    }
    
    [Fact]
    public async Task ReturnsDeclinedWhenTheBankDoesNotAuthorize()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"authorized":false,"authorization_code":""}"""));

        var result = await CreateClient(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.Authorized);
    }
    
    [Fact]
    public async Task ReturnsNullWhenTheBankIsUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await CreateClient(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task ReturnsNullWhenTheBankCannotBeReached()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));

        var result = await CreateClient(handler).AuthorizeAsync(Request, CancellationToken.None);

        Assert.Null(result);
    }
    
}