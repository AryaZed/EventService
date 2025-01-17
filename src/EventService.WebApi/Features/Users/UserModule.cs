using Carter;
using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Users;

namespace EventService.WebApi.Features.Users;

public class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");
        group.MapPost("/", async (IUserRepository repo, UserRequest request) =>
        {
            var business = Business.CreateExisting(request.BusinessId);
            var user = User.Create(request.Name, request.Email, request.PhoneNumber, business);
            await repo.AddAsync(user);
            return Results.Created($"/api/users/{user.Id}", UserResponse.FromEntity(user));
        });

        group.MapGet("/business/{businessId:guid}", async (IUserRepository repo, Guid businessId) =>
        {
            var users = await repo.GetAllByBusinessIdAsync(businessId);
            return users.Any() ? Results.Ok(users.Select(UserResponse.FromEntity)) : Results.NotFound();
        });

        group.MapGet("/{id:guid}", async (IUserRepository repo, Guid id) =>
        {
            var user = await repo.GetUsersByIdsAsync(new List<Guid> { id });
            return user.Any() ? Results.Ok(UserResponse.FromEntity(user.First())) : Results.NotFound();
        });

        group.MapPut("/{id:guid}", async (IUserRepository repo, Guid id, UserRequest request) =>
        {
            var existingUsers = await repo.GetUsersByIdsAsync(new List<Guid> { id });
            if (!existingUsers.Any()) return Results.NotFound();

            var existingUser = existingUsers.First();
            existingUser.Update(request.Name, request.Email, request.PhoneNumber);
            await repo.UpdateAsync(existingUser);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (IUserRepository repo, Guid id) =>
        {
            var existingUsers = await repo.GetUsersByIdsAsync(new List<Guid> { id });
            if (!existingUsers.Any()) return Results.NotFound();

            await repo.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}