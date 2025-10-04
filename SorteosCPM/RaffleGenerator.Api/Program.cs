using ConnectionManager;
using DbServicesProvider.Dto;
using DbServicesProvider.Interfaces;
using DbServicesProvider.Repositories.Sql;
using DbServicesProvider.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RaffleGenerator.Api;
using RaffleGenerator.Api.Dto;
using RaffleServicesProvider;
using RaffleServicesProvider.Interfaces;
using RaffleServicesProvider.Services;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using WorkerService;
using WorkerService.Services;

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
builder.Services.AddSingleton<LoadQueueService>();
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<FormOptions>(opt =>
{
    opt.MultipartBodyLengthLimit = long.MaxValue;
});
builder.Services.Configure<IISServerOptions>(opt =>
{
    opt.MaxRequestBodySize = long.MaxValue;
});

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
    options.GetLevel = GetLogLevel;
});

static LogEventLevel GetLogLevel(HttpContext httpContext, double elapsed, Exception? ex)
{
    if (ex != null || httpContext.Response.StatusCode >= 500)
        return LogEventLevel.Error;
    return LogEventLevel.Information;
}

app.UseStaticFiles();
app.UseCors("PolicyCPM");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();


app.MapGet("/RafflesList", async ([FromServices] RafflesManagement management) =>
{
    var result = await management.GetRegisteredRaffles();
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("GetRafflesAsync")
// .RequireAuthorization("AdminPolicy")
.WithOpenApi();


app.MapPost("/RegisteredRaffle", async ([FromServices] RafflesManagement management, [FromForm] RegisteredRequest request) =>
{
    FileDto? image = null;
    if (request.Image != null)
        image = await Helper.ConvertFile(request.Image);
    var result = await management.RegisteredRaffle(request.NameRaffle, image);
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("GenerateRaffleAsync")
.DisableAntiforgery()
//.RequireAuthorization("AdminPolicy")
.WithOpenApi();


app.MapPost("/EditRaffle", async ([FromServices] RafflesManagement management, [FromForm] EditRequest request) =>
{
    FileDto? image = null;
    if (request.Image != null)
        image = await Helper.ConvertFile(request.Image);
    var result = await management.EditRaffleAsync(request.IdSorteo, request.NombreSorteo, request.PermisoSegob, image);
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("EditRaffleAsync")
.DisableAntiforgery()
//.RequireAuthorization("AdminPolicy")
.WithOpenApi();



app.MapPost("/AwardsUploads", async ([FromServices] RafflesManagement management, [FromForm] LoadRequest carga) =>
{
    if (carga.File != null)
    {
        FileDto cargas = await Helper.ConvertFile(carga.File);
        var result = await management.PrizeLoadingAsync(carga.IdSorteo, cargas);
        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);
        return Results.Ok(result.Value);
    }
    return Results.BadRequest("No file was received.");
})
.WithName("LoadsAwardsAsync")
.DisableAntiforgery()
//.RequireAuthorization("AdminPolicy")
.WithOpenApi();


app.MapPost("/ParticipantsUploads", [DisableRequestSizeLimit] async ([FromServices] IFileGenerator fileServices, [FromServices] LoadQueueService queueService, [FromForm] LoadRequest carga) =>
{
    if (carga.File != null)
    {
        FileDto cargas = await Helper.ConvertFile(carga.File);
        var result = await fileServices.SaveFileTemp(cargas);
        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);
        var load = new LoadFile
        {
            ProcessId = Guid.NewGuid(),
            Path = result.Value,
            IdSorteo = carga.IdSorteo,
            Status = LoadStatus.Pending, // Estado inicial
            CreatedAt = DateTime.UtcNow
        };
        await queueService.EnqueueAsync(load);
        return Results.Ok(new { load.ProcessId });
    }
    return Results.BadRequest("No file was received.");
})
.WithName("LoadsParticipantsAsync")
.DisableAntiforgery()
//.RequireAuthorization("AdminPolicy")
.WithOpenApi();


app.MapGet("/DrawSettings/{IdSorteo}/{Option}", async (int IdSorteo, int Option, [FromServices] RafflesManagement management) =>
{
    var result = await management.RaffleSettingAsync(IdSorteo, Option);
    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);
    return Results.Ok(result.Value);
})
.WithName("SettingsAsync")
// .RequireAuthorization("AdminPolicy")
.WithOpenApi();

app.MapGet("/GetProcess/{IdSorteo}/{ProcessId}", async (int IdSorteo, Guid ProcessId, [FromServices] RafflesManagement rafflesManagement) =>
{
    var results = await rafflesManagement.ProcessConsultantAsync(IdSorteo, ProcessId);
    if (!results.IsSuccess)
        return Results.BadRequest(results.Error);
    return Results.Ok(new
    {
        ProcessId = results.Value.ProcessId,
        IdSorteo = results.Value.IdSorteo,
        Estatus = results.Value.Status.ToString(),
        CreadoA = results.Value.CreatedAt,
        CompletadoA = results.Value.CompletedAt,
        FilasProcesadas = results.Value.RowsProcessed,
        Error = results.Value.ErrorMessage
    });
})
.WithName("GetProcessAsync")
.DisableAntiforgery()
.WithOpenApi();

app.MapFallbackToFile("index.html");

await app.RunAsync();
