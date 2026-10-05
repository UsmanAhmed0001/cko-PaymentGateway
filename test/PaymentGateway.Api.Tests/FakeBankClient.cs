using PaymentGateway.Api.Bank;

namespace PaymentGateway.Api.Tests;

public class FakeBankClient : IAcquiringBankClient
{
    private readonly BankAuthorizationResult? _result;

    public FakeBankClient(BankAuthorizationResult? result) => _result = result;

    public int CallCount { get; private set; }
    public BankAuthorizationRequest? LastRequest { get; private set; }

    public Task<BankAuthorizationResult?> AuthorizeAsync(
        BankAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        return Task.FromResult(_result);
    }
}