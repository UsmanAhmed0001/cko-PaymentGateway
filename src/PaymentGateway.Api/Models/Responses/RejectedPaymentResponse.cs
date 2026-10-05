namespace PaymentGateway.Api.Models.Responses;

public record RejectedPaymentResponse(IReadOnlyList<string> Errors)
{
    public PaymentStatus Status => PaymentStatus.Rejected;
}