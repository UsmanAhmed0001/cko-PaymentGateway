using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public class PaymentRequestValidator
{
    private static readonly HashSet<string> SupportedCurrencies = new() {"GBP", "USD", "EUR"};

    private readonly TimeProvider _timeprovider;

    public PaymentRequestValidator(TimeProvider timeprovider)
    {
        _timeprovider = timeprovider;
    }

    public IReadOnlyList<string> Validate(PostPaymentRequest request)
    {
        var errors = new List<string>();
        
        ValidateCardNumber(request.CardNumberLastFour, errors);
        ValidateExpiry(request.ExpiryMonth, request.ExpiryYear, errors);
        ValidateCurrency(request.Currency, errors);
        ValidateAmount(request.Amount, errors);
        ValidateCvv(request.Cvv, errors);
        
        return errors;
    }

    private static void ValidateCardNumber(string? cardNumber, List<string> errors)
    {
        if (string.IsNullOrEmpty(cardNumber))
            errors.Add("Card number is required");
        else if (cardNumber.Length is <14 or >19 || !IsDigitsOnly(cardNumber))
            errors.Add("Card number must be 14-19 numeric characters");
    }

    private void ValidateExpiry(int? month, int? year, List<string> errors)
    {
        if (month is null)
            errors.Add("Expiry month is required");
        else if (month is < 1 or > 12)
            errors.Add("Expiry month must be between 1 and 12");
        
        if (year is null)
            errors.Add("Expiry year is required");

        if (month is >= 1 && month <= 12 && year is not null)
        {
            var now = _timeprovider.GetUtcNow();
            var isFuture = year > now.Year || (year == now.Year && month >= now.Month);
            
            if (!isFuture)
                errors.Add("Card expiry date must be in future");

        }
    }

    private static void ValidateCurrency(string? currency, List<string> errors)
    {
        if (string.IsNullOrEmpty(currency))
            errors.Add("Currency is required");
        else if(!SupportedCurrencies.Contains(currency))
            errors.Add("Currency is not supported, must be one of: GBP, USD, EUR");
    }

    private static void ValidateAmount(long? amount, List<string> errors)
    {
        if (amount is null)
            errors.Add("Amount is required");
        else if (amount <= 0)
            errors.Add("Amount must be greater than zero");
    }

    private static void ValidateCvv(string? cvv, List<string> errors)
    {
        if (string.IsNullOrEmpty(cvv))
            errors.Add("CVV is required");
        else if (cvv.Length is < 3 or > 4 || !IsDigitsOnly(cvv))
            errors.Add("CVV must be between 3-4 numeric characters");
    }
    
    private static bool IsDigitsOnly(string value) => value.All(char.IsAsciiDigit);
}