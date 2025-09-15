using LoginValidator.Api.Dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using ServiceManager;
using ServiceManager.Interfaces;
using ServiceManager.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", 
        Environment.GetEnvironmentVariable("APP_NAME") ?? "RaffleGeneratorCPM")
    .Enrich.WithProperty("Environment",
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .Enrich.WithProperty("Version",
        Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0")
    .Enrich.WithProperty("DeploymentId",
        Environment.GetEnvironmentVariable("DEPLOYMENT_ID") ?? "localhost")
    .ReadFrom.Configuration(builder.Configuration) // LUEGO leer la configuración
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddTransient<IAuthentication, ADAuthenticationServices>();
builder.Services.AddTransient<IAuthorization, ADAuthorizationService>();
builder.Services.AddTransient<JWTAuthorizationService>();
builder.Services.AddTransient<LoginManagement>();

string[] origins = builder.Configuration.GetValue<string[]>("Origins") ?? Array.Empty<string>();
string secretKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new ArgumentException("Key not found in configuration (appsettings.json)");
byte[] key = System.Text.Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        RoleClaimType = ClaimTypes.Role
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(policys =>
{
    policys.AddPolicy("PolicyCPM", p =>
    {
        p.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex != null ? LogEventLevel.Error :
        httpContext.Response.StatusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;
});

// app.UseCors("PolicyCPM");
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapPost("/v1/Login", ([FromServices] LoginManagement login, LoginRequest request) =>
{
    Result<string> result = login.Login(request.Username, request.Password);
    if (result.CodeError == 401)
        return Results.Unauthorized();
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("ValidateCredentials")
.WithOpenApi();

app.MapGet("/v1/RenewToken", ([FromServices] LoginManagement login, HttpRequest request) => 
{
    string accessToken =  request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    Result<string> result = login.RenewToken(accessToken);
    if (result.CodeError == 401)
        return Results.Unauthorized();
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("RenewJwt")
.RequireAuthorization()
.WithOpenApi();

app.Run();
