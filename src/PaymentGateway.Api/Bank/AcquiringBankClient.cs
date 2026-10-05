using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Bank;

public class AcquiringBankClient : IAcquiringBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AcquiringBankClient> _logger;

    public AcquiringBankClient(HttpClient httpClient, ILogger<AcquiringBankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BankAuthorizationResult?> AuthorizeAsync(
        BankAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        // Translate our request into the bank's format, e.g. expiry 4 and 2027 become "04/2027".
        var bankRequest = new BankPaymentRequest(
            request.CardNumber,
            $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            request.Currency,
            request.Amount,
            request.Cvv);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("payments", bankRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Acquiring bank returned status {StatusCode}", (int)response.StatusCode);
                return null;
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken);

            if (bankResponse is null)
            {
                _logger.LogWarning("Acquiring bank returned an empty response");
                return null;
            }

            return new BankAuthorizationResult(bankResponse.Authorized, bankResponse.AuthorizationCode);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Could not reach the acquiring bank");
            return null;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Acquiring bank request timed out");
            return null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Acquiring bank returned a response we could not read");
            return null;
        }
    }
}

// The bank's wire format: snake_case names and "MM/yyyy" expiry. Only this file knows about it.
internal sealed record BankPaymentRequest(
    [property: JsonPropertyName("card_number")] string CardNumber,
    [property: JsonPropertyName("expiry_date")] string ExpiryDate,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("cvv")] string Cvv);

internal sealed record BankPaymentResponse(
    [property: JsonPropertyName("authorized")] bool Authorized,
    [property: JsonPropertyName("authorization_code")] string? AuthorizationCode);