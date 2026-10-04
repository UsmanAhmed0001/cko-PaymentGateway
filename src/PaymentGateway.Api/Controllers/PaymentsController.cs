using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly InMemoryPaymentsRepository _paymentsRepository;

    public PaymentsController(InMemoryPaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PaymentResponse> GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);
        
        if (payment is null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }
}