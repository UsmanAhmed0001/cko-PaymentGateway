using Microsoft.AspNetCore.Identity;

namespace PaymentGateway.Api.Tests;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

public class PaymentRequestValidatorTests
{
    // 15th October 2026
    private readonly PaymentRequestValidator _validator = new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 15, 00, 00, 00, TimeSpan.Zero)));

    public static PostPaymentRequest ValidRequest(
        string? cardNumber = "2222405343248877",
        int? expiryMonth = 4,
        int? expiryYear = 2027,
        string? currency = "GBP",
        long? amount = 100,
        string? cvv = "123") => new()
    {
        CardNumberLastFour = cardNumber,
        ExpiryMonth = expiryMonth,
        ExpiryYear = expiryYear,
        Currency = currency,
        Amount = amount,
        Cvv = cvv
    };
    
    [Fact]
    public void AcceptsAValidRequest()
    {
        var errors = _validator.Validate(ValidRequest());
        Assert.Empty(errors);
    }
    
    [Fact]
    public void ReportsEveryMissingFieldAtOnce()
    {
        var errors = _validator.Validate(new PostPaymentRequest());
        
        Assert.Equal(6, errors.Count);
    }

    [Theory]
    [InlineData("")] //empty
    [InlineData("1234567890123")] //13 digits: too short
    [InlineData("12345678901234567890")] //20 digits: 200 long
    [InlineData("2222 4053 4324 8877")] //contains spaces
    [InlineData("2222abcd43248877")] //contains letters
    [InlineData("٢٢٢٢405343248877")] //Arabic-Indic digits: not ASCII 0-9
    public void RejectsInvalidCardNumber(string cardNumber)
    {
        var errors = _validator.Validate(ValidRequest(cardNumber: cardNumber));
        
        var error = Assert.Single(errors);
        Assert.Contains("Card number", error);
    }
    
    [Theory]
    [InlineData("12345678901234")]      // 14 digits: shortest allowed
    [InlineData("1234567890123456789")] // 19 digits: longest allowed
    public void AcceptsCardNumberAtLengthBoundaries(string cardNumber)
    {
        Assert.Empty(_validator.Validate(ValidRequest(cardNumber: cardNumber)));
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void RejectsExpiryMonthOutOfRange(int month)
    {
        var error = Assert.Single(_validator.Validate(ValidRequest(expiryMonth: month)));
        Assert.Contains("Expiry month", error);
    }
    
    [Theory]
    [InlineData(9, 2026)]  // last month
    [InlineData(12, 2025)] // last year
    public void RejectsExpiredCard(int month, int year)
    {
        var error = Assert.Single(_validator.Validate(ValidRequest(expiryMonth: month, expiryYear: year)));
        Assert.Contains("future", error);
    }
    
    [Theory]
    [InlineData(10, 2026)] // this month: a card is valid until the end of its expiry month
    [InlineData(11, 2026)] // next month
    [InlineData(1, 2027)]  // next year
    public void AcceptsCardThatHasNotExpired(int month, int year)
    {
        Assert.Empty(_validator.Validate(ValidRequest(expiryMonth: month, expiryYear: year)));
    }
    
    [Theory]
    [InlineData("gbp")] // lowercase: ISO codes are uppercase
    [InlineData("JPY")] // real code, but not one we support
    [InlineData("GB")]  // wrong length
    [InlineData("")]
    public void RejectsUnsupportedCurrency(string currency)
    {
        var error = Assert.Single(_validator.Validate(ValidRequest(currency: currency)));
        Assert.Contains("Currency", error);
    }
    
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void RejectsAmountThatIsNotPositive(long amount)
    {
        var error = Assert.Single(_validator.Validate(ValidRequest(amount: amount)));
        Assert.Contains("Amount", error);
    }
    
    [Theory]
    [InlineData("12")]    // too short
    [InlineData("12345")] // too long
    [InlineData("12a")]   // contains a letter
    [InlineData("")]
    public void RejectsInvalidCvv(string cvv)
    {
        var error = Assert.Single(_validator.Validate(ValidRequest(cvv: cvv)));
        Assert.Contains("CVV", error);
    }
    
    [Theory]
    [InlineData("012")]  // leading zero must survive
    [InlineData("1234")] // 4 digits, e.g. Amex
    public void AcceptsValidCvv(string cvv)
    {
        Assert.Empty(_validator.Validate(ValidRequest(cvv: cvv)));
    }
}