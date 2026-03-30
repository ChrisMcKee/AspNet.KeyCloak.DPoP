using AspNetCore.Authentication.Api;

using Microsoft.AspNetCore.Authentication.JwtBearer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds KeyCloak JWT validation to the API
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
                   ?? throw new InvalidOperationException("KeyCloak:Audience is required")
    };
    options.Domain = builder.Configuration["KeyCloak:Domain"]
                     ?? throw new InvalidOperationException("KeyCloak:Domain is required");
    options.JwtBearerOptions.Events = new JwtBearerEvents
    {
        // Custom event just to log the token received in the request.
        OnMessageReceived = context =>
        {
            Console.WriteLine($"Token extracted? : {(!string.IsNullOrEmpty(context.Token) ? "yes" : "no")}");
            return Task.CompletedTask;
        }
    };
}).WithDPoP();

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/open-endpoint", () =>
    {
        var responseMessage = "This endpoint is available to all users.";
        return responseMessage;
    })
    .WithName("AccessOpenEndpoint")
    .WithOpenApi();

app.MapGet("/restricted-endpoint", () =>
    {
        var responseMessage = "You are special. This endpoint is available only to select users.";
        return responseMessage;
    })
    .WithName("AccessRestrictedEndpoint")
    .WithOpenApi().RequireAuthorization();

app.Run();
