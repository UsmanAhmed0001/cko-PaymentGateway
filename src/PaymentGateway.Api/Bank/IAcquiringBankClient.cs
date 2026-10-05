namespace PaymentGateway.Api.Bank;


public record BankAuthorizationRequest(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    long Amount,
    string Cvv);

public record BankAuthorizationResult(bool Authorized, string? AuthorizationCode);

public interface IAcquiringBankClient
{
    Task<BankAuthorizationResult?> AuthorizeAsync(
        BankAuthorizationRequest request,
        CancellationToken cancellationToken);
}