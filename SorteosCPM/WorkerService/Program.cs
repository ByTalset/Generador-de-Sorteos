using ConnectionManager;
using DbServicesProvider.Interfaces;
using DbServicesProvider.Repositories.Sql;
using WorkerService;
using WorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<LoadQueueService>();
builder.Services.AddSingleton<IUnitOfWork, SqlUnitOfWork>(provider =>
{
    string variable = builder.Configuration.GetValue<string>("Variable:Name") ?? "";
    var logger = provider.GetRequiredService<ILogger<Program>>();
    Result<string> connectionString = ConnectionHelper.GetConnectionString(logger, variable);
    if (!connectionString.IsSuccess)
        throw new ArgumentException(connectionString.Error);
    return new SqlUnitOfWork(logger, connectionString.Value);
});

var host = builder.Build();
host.Run();
