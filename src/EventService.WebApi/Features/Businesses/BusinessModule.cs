using Carter;
using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Businesses;

namespace EventService.WebApi.Features.Businesses;

public class BusinessModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses").WithTags("Businesses");

        group.MapPost("/", async (IBusinessRepository repo, BusinessRequest request) =>
        {
            var business = Business.Create(request.Name, request.ContactEmail, request.PhoneNumber, request.SubscriptionPlan);
            await repo.AddAsync(business);
            return Results.Created($"/api/businesses/{business.Id}", BusinessResponse.FromEntity(business));
        });

        group.MapGet("/", async (IBusinessRepository repo, HttpContext context) =>
        {
            if (context.Items["TenantId"] is not string tenantIdString || !Guid.TryParse(tenantIdString, out var tenantId))
            {
                return Results.BadRequest("Tenant ID is missing or invalid.");
            }

            var business = await repo.GetBusinessByTenantAsync(tenantId);
            return business is not null ? Results.Ok(BusinessResponse.FromEntity(business)) : Results.NotFound();
        });

        group.MapGet("/{id:guid}", async (IBusinessRepository repo, Guid id) =>
        {
            var business = await repo.GetByIdAsync(id);
            return business is not null ? Results.Ok(BusinessResponse.FromEntity(business)) : Results.NotFound();
        });

        group.MapPut("/{id:guid}", async (IBusinessRepository repo, Guid id, BusinessRequest request) =>
        {
            var existingBusiness = await repo.GetByIdAsync(id);
            if (existingBusiness is null) return Results.NotFound();

            existingBusiness = Business.Create(request.Name, request.ContactEmail, request.PhoneNumber, request.SubscriptionPlan);
            await repo.UpdateAsync(existingBusiness);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (IBusinessRepository repo, Guid id) =>
        {
            await repo.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}