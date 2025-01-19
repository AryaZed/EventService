using Carter;
using EventService.Application.Services.Payments;
using Microsoft.AspNetCore.Mvc;

namespace EventService.WebApi.Features.Payments;

public class PaymentModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapPost("/request", async ([FromServices] PaymentService paymentService, [FromServices] IConfiguration config,
            [FromBody] PaymentRequest request) =>
        {
            var authority = await paymentService.RequestPaymentAsync(request.Amount, config["Zarinpal:CallbackUrl"], request.Description);

            return authority != null
                ? Results.Ok(new { PaymentUrl = $"https://www.zarinpal.com/pg/StartPay/{authority}" })
                : Results.BadRequest("Payment request failed.");
        });

        group.MapGet("/verify", async ([FromServices] PaymentService paymentService, [FromQuery] string authority, [FromQuery] decimal amount) =>
        {
            var success = await paymentService.VerifyPaymentAsync(authority, amount);
            return success ? Results.Ok("Payment successful.") : Results.BadRequest("Payment verification failed.");
        });
    }
}

public record PaymentRequest(decimal Amount, string Description);
