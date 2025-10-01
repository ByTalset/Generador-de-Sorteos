using System.Security.Claims;
using ConnectionManager;
using DbServicesProvider.Interfaces;
using DbServicesProvider.Repositories.Sql;
using DbServicesProvider.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RaffleServicesProvider;
using RaffleServicesProvider.Interfaces;
using RaffleServicesProvider.Services;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;

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

builder.Services.AddSingleton<IUnitOfWork, SqlUnitOfWork>(provider =>
{
    string variable = builder.Configuration.GetValue<string>("Variable:Name") ?? "";
    var logger = provider.GetRequiredService<ILogger<Program>>();
    Result<string> connectionString = ConnectionHelper.GetConnectionString(logger, variable);
    if (!connectionString.IsSuccess)
        throw new ArgumentException(connectionString.Error);
    return new SqlUnitOfWork(logger, connectionString.Value);
});
builder.Services.AddScoped<DbServices>();
builder.Services.AddScoped<IFileGenerator, FileServices>();
builder.Services.AddScoped<IImageFileManagement, ImageServices>();
builder.Services.AddTransient<RafflesManagement>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin", "TI", "Sistemas")); // Acepta varios roles
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
});

builder.Services.AddCors(policys =>
{
    policys.AddPolicy("PolicyCPM", p =>
    {
        // p.WithOrigins(origins)
        p.AllowAnyOrigin()
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

app.UseCors("PolicyCPM");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapGet("/GetAaward/{IdSorteo}", async (int idSorteo, [FromServices] RafflesManagement rafflesManagement) =>
{
    var results = await rafflesManagement.GetAwardCurrentAync(idSorteo);
    if (!results.IsSuccess)
        return Results.BadRequest(results.Error);
    if (results.Value is null)
        return Results.Accepted(null, "No hay mas premios que sortear.");
    return Results.Ok(new
    {
        results.Value.IdZona,
        results.Value.Zona,
        results.Value.NumPremio,
        results.Value.Descripcion
    });
})
.WithName("GetAsync")
.DisableAntiforgery()
.WithOpenApi();

app.MapGet("/Execute/{IdSorteo}", async (int idSorteo, [FromServices] RafflesManagement rafflesManagement) =>
{
    var results = await rafflesManagement.ExcuteRaffleAync(idSorteo);
    if (!results.IsSuccess)
        return Results.BadRequest(results.Error);
    if (results.Value is null)
        return Results.Accepted(null, "No hay mas premios que sortear.");
    return Results.Ok(new
        {
            results.Value.CIF,
            Nombre = $"{results.Value.Nombre}{results.Value.SegundoNombre}{results.Value.PrimerApellido}",
            results.Value.Telefono,
            results.Value.Domicilio,
            results.Value.Estado,
            results.Value.Plaza
        });
})
.WithName("ExecuteRaffleAsync")
.DisableAntiforgery()
.WithOpenApi();

app.MapGet("/ListWinner/{IdSorteo}", async (int idSorteo, int? idZona, [FromServices] RafflesManagement rafflesManagement) =>
{
    var results = await rafflesManagement.PrintListWinnerAsync(idSorteo, idZona);
    if (!results.IsSuccess)
        return Results.BadRequest(results.Error);
    return Results.Ok(results.Value);
})
.WithName("PrintWinnersAsync")
.DisableAntiforgery()
.WithOpenApi();

app.MapGet("/ListAreas/{IdSorteo}", async (int idSorteo, [FromServices] RafflesManagement rafflesManagement) =>
{
    var results = await rafflesManagement.PrintListAreasAsync(idSorteo);
    if (!results.IsSuccess)
        return Results.BadRequest(results.Error);
    return Results.Ok(results.Value);
})
.WithName("PrintAreasAsync")
.DisableAntiforgery()
.WithOpenApi();

app.Run();