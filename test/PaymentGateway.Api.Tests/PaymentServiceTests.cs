using Microsoft.Extensions.Logging.Abstractions;
using PaymentGateway.Api.Bank;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentServiceTests
{
    private readonly InMemoryPaymentsRepository _repository = new();

    private static readonly PostPaymentRequest ValidRequest = new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = 2027,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    private PaymentService CreateService(FakeBankClient bank) =>
        new(new PaymentRequestValidator(
                new FixedTimeProvider(new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero))),
            bank,
            _repository,
            NullLogger<PaymentService>.Instance);
    
    [Fact]
    public async Task RejectsAnInvalidRequestWithoutCallingTheBank()
    {
        var bank = new FakeBankClient(new BankAuthorizationResult(true, "abc"));
        var invalidRequest = new PostPaymentRequest { CardNumber = "123" };

        var result = await CreateService(bank).ProcessAsync(invalidRequest, CancellationToken.None);

        Assert.IsType<ProcessPaymentResult.Rejected>(result);
        Assert.Equal(0, bank.CallCount);
    }
    
    [Fact]
    public async Task StoresAnAuthorizedPaymentWithOnlyTheLastFourDigits()
    {
        var bank = new FakeBankClient(new BankAuthorizationResult(true, "abc"));

        var result = await CreateService(bank).ProcessAsync(ValidRequest, CancellationToken.None);

        var processed = Assert.IsType<ProcessPaymentResult.Processed>(result);
        Assert.Equal(PaymentStatus.Authorized, processed.Payment.Status);
        Assert.Equal("8877", processed.Payment.CardNumberLastFour);
        Assert.NotNull(_repository.Get(processed.Payment.Id));
    }
    
    [Fact]
    public async Task StoresADeclinedPaymentAsDeclined()
    {
        var bank = new FakeBankClient(new BankAuthorizationResult(false, ""));

        var result = await CreateService(bank).ProcessAsync(ValidRequest, CancellationToken.None);

        var processed = Assert.IsType<ProcessPaymentResult.Processed>(result);
        Assert.Equal(PaymentStatus.Declined, processed.Payment.Status);
        Assert.Null(processed.Payment.AuthorizationCode);
    }
    
    [Fact]
    public async Task ReturnsBankUnavailableWhenTheBankGivesNoAnswer()
    {
        var bank = new FakeBankClient(null);

        var result = await CreateService(bank).ProcessAsync(ValidRequest, CancellationToken.None);

        Assert.IsType<ProcessPaymentResult.BankUnavailable>(result);
    }
    
    [Fact]
    public async Task SendsTheFullCardDetailsToTheBank()
    {
        var bank = new FakeBankClient(new BankAuthorizationResult(true, "abc"));

        await CreateService(bank).ProcessAsync(ValidRequest, CancellationToken.None);

        Assert.Equal("2222405343248877", bank.LastRequest!.CardNumber);
        Assert.Equal("123", bank.LastRequest.Cvv);
    }
}