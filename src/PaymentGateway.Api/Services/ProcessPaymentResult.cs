using PaymentGateway.Api.Models;
namespace PaymentGateway.Api.Services;

public abstract record ProcessPaymentResult
{

    public sealed record Processed(Payment Payment) : ProcessPaymentResult;
        public sealed record Rejected(IReadOnlyList<string> Errors) : ProcessPaymentResult;
        public sealed record BankUnavailable() : ProcessPaymentResult;
        
    
}