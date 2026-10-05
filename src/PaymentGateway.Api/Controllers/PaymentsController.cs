using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RejectedPaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ProcessPayment(
        PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessAsync(request, cancellationToken);

        return result switch
        {
            ProcessPaymentResult.Processed processed => CreatedAtAction(
                nameof(GetPayment),
                new { id = processed.Payment.Id },
                PaymentResponse.From(processed.Payment)),

            ProcessPaymentResult.Rejected rejected =>
                BadRequest(new RejectedPaymentResponse(rejected.Errors)),

            ProcessPaymentResult.BankUnavailable => Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The acquiring bank is currently unavailable. Please try again later."),

            _ => throw new UnreachableException()
        };
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PaymentResponse> GetPayment(Guid id)
    {
        var payment = _paymentService.GetPayment(id);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(PaymentResponse.From(payment));
    }
}