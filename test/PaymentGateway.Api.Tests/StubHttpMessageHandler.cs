namespace PaymentGateway.Api.Tests;

public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
    
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }
    
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody {get; private set;}

    async protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody =request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return _respond(request);
    }
    
}