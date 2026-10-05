using PaymentGateway.Api.Services;
using System.Text.Json.Serialization;
using PaymentGateway.Api.Bank;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Responses;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPaymentsRepository, InMemoryPaymentsRepository>();
builder.Services.AddSingleton<PaymentRequestValidator>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new RejectedPaymentResponse(
                new[] { "The request body is not valid JSON or a field has the wrong type." })));

builder.Services.AddHttpClient<IAcquiringBankClient, AcquiringBankClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AcquiringBank:BaseUrl"]
        ?? throw new InvalidOperationException("AcquiringBank:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.Run();
