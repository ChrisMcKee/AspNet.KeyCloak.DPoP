using System.Security.Claims;

using AspNet.KeyCloak.DPoP;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string corsPolicyName = "cors-app-policy";

builder.Services.AddCors(p => p.AddPolicy(corsPolicyName,
    corsPolicyBuilder => corsPolicyBuilder
                         .WithOrigins(builder.Configuration["BACKEND_CORS_ORIGIN"]
                                      ?? throw new InvalidOperationException("BACKEND_CORS_ORIGIN is required"))
                         .AllowAnyMethod()
                         .AllowAnyHeader()
));

builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"] ?? throw new InvalidOperationException("KeyCloak:Domain is required");
    options.Authority = builder.Configuration["KeyCloak:Authority"]; // http://localhost:8080/realms/test
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"] ?? throw new InvalidOperationException("KeyCloak:Audience is required"),
        // RequireHttpsMetadata = true,
        // SaveToken = true,
        IncludeErrorDetails = true,
        // TokenValidationParameters = new TokenValidationParameters
        // {
        //     ValidIssuer = options.Authority,
        //     ValidAudience = builder.Configuration["KeyCloak:Audience"] ?? throw new InvalidOperationException("KeyCloak:Audience is required"),
        //     ValidateIssuer = true,
        //     ValidateAudience = true,
        //     ValidateLifetime = true,
        //     ValidateIssuerSigningKey = true,
        //     ClockSkew = TimeSpan.FromMinutes(5),
        //     NameClaimType = ClaimTypes.NameIdentifier
        // },
        Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine($"Token extracted? : {(!string.IsNullOrEmpty(context.Token) ? "yes" : "no")}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        }
    };
}).WithDPoP();

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirect omitted — playground runs on plain HTTP (localhost:5059).
app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/open-endpoint", () => "This endpoint is available to all users.")
    .WithName("AccessOpenEndpoint");

app.MapGet("/restricted-endpoint", () => "You are special. This endpoint is available only to select users.")
    .WithName("AccessRestrictedEndpoint")
    .RequireAuthorization();

await app.RunAsync();

public partial class Program();
