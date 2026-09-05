using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Infrastructure;
using VerifyDriversAPI.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Title = "Request validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more request fields are invalid."
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });
builder.Services.AddVerifyDriverDatabase(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("VerifyDriverFrontend", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? (builder.Environment.IsDevelopment()
                ? ["https://localhost:7172", "http://localhost:5172", "http://localhost:5000"]
                : []);

        if (origins.Length == 0)
        {
            policy.DisallowCredentials();
            return;
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        options => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModeratorOnly", policy => policy.RequireRole("Moderator", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.InitializeVerifyDriverDatabaseAsync(app.Environment);

app.UseHttpsRedirection();

app.UseCors("VerifyDriverFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
