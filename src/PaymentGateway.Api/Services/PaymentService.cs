using PaymentGateway.Api.Bank;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
namespace PaymentGateway.Api.Services;

public class PaymentService
{
    private readonly PaymentRequestValidator _validator;
    private readonly IAcquiringBankClient _bankClient;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentRequestValidator validator,
        IAcquiringBankClient bankClient,
        IPaymentsRepository paymentsRepository,
        ILogger<PaymentService> logger)
    {
        _validator = validator;
        _bankClient = bankClient;
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    public async Task<ProcessPaymentResult> ProcessAsync(
        PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            _logger.LogInformation("Payment rejected with {ErrorCount} validation errors", errors.Count);
            return new ProcessPaymentResult.Rejected(errors);
        }
        
        var bankRequest = new BankAuthorizationRequest(
            request.CardNumber!,
            request.ExpiryMonth!.Value,
            request.ExpiryYear!.Value,
            request.Currency!,
            request.Amount!.Value,
            request.Cvv!);
        
        var bankResult = await _bankClient.AuthorizeAsync(bankRequest, cancellationToken);
        if (bankResult is null)
        {
            return new ProcessPaymentResult.BankUnavailable();
        }
        
        var payment = new Payment(
            Id: Guid.NewGuid(),
            Status: bankResult.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            CardNumberLastFour: request.CardNumber![^4..],
            ExpiryMonth: request.ExpiryMonth.Value,
            ExpiryYear: request.ExpiryYear.Value,
            Currency: request.Currency!,
            Amount: request.Amount.Value,
            AuthorizationCode: bankResult.Authorized ? bankResult.AuthorizationCode : null);

        _paymentsRepository.Add(payment);

        _logger.LogInformation("Payment {PaymentId} processed with status {Status}", payment.Id, payment.Status);
        
        return new ProcessPaymentResult.Processed(payment);

    }
    public Payment? GetPayment(Guid id) => _paymentsRepository.Get(id);
}