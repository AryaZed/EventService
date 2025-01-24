using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventService.WebApi.Swagger
{
    public class SwaggerTenantHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // ✅ Enforce `X-Tenant-Id` Header in Every API Call
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Required = true, // ✅ Required for multi-tenancy
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}
