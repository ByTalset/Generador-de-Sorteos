using System.Data;
using ConnectionManager;
using DbServicesProvider.Dto;
using DbServicesProvider.Interfaces;
using Microsoft.Data.SqlClient.Server;
using RaffleServicesProvider;
using WorkerService.Services;

namespace WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly LoadQueueService _queueService;
    private readonly IUnitOfWork _unitOfWork;
    public string TablaParticipantes { get; set; } = string.Empty;

    public Worker(ILogger<Worker> logger, LoadQueueService queueService, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _queueService = queueService;
        _unitOfWork = unitOfWork;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var carga in _queueService.DequeueAsync(stoppingToken))
        {
            _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
            await ProcessFileAsync(carga);
            var result = DeleteDirectory(carga.Path);
            if (!result.IsSuccess) _logger.LogError(result.Error);
        }
    }

    #region Method Private
    private async Task ProcessFileAsync(LoadFile load)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var zonasResult = await GetAreasAsync(load.IdSorteo);
            if (!zonasResult.IsSuccess)
                throw new ArgumentException(zonasResult.Error);

            using var reader = new StreamReader(load.Path);
            string? headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
                throw new ArgumentException("The file is empty or has no headers.");

            var delimiter = RaffleHelperServices.DetectDilimited(headerLine);
            if (!delimiter.IsSuccess)
                throw new ArgumentException(delimiter.Error);

            var metaDatas = GetMetadatas();
            var records = GetRecords(reader, metaDatas, zonasResult.Value, delimiter.Value);
            await _unitOfWork.ParticipantRepository.BulkInsertAsync(TablaParticipantes, load.ProcessId, load.IdSorteo, records);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: {Error}", ex.Message);
            await _unitOfWork.RollbackAsync();
        }
    }

    private static IEnumerable<SqlDataRecord> GetRecords(StreamReader reader, SqlMetaData[] metaDatas, Dictionary<string, int> zonas, char delimiter)
    {
        string? line = string.Empty;
        while ((line = reader.ReadLine()) != null)
        {
            string[] parts = line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToArray();
            if (parts.Length < 11 || parts.Length > 11) continue;
            var record = new SqlDataRecord(metaDatas);
            zonas.TryGetValue(parts[10], out int zonaId);
            record.SetInt64(0, long.Parse(parts[0]));
            record.SetString(1, parts[1]);
            record.SetString(2, parts[2]);
            record.SetString(3, parts[3]);
            record.SetString(4, parts[4]);
            record.SetString(5, parts[5]);
            record.SetString(6, parts[6]);
            record.SetString(7, parts[7]);
            record.SetString(8, parts[8]);
            record.SetString(9, parts[9]);
            record.SetInt32(10, zonaId);

            yield return record;
        }
    }

    private async Task<Result<Dictionary<string, int>>> GetAreasAsync(int idSorteo)
    {
        TablaParticipantes = $"{idSorteo}_Participantes";
        string nombreTablaZonas = await _unitOfWork.RaffleRepository.GetRaffleAsync(idSorteo);
        if (string.IsNullOrEmpty(nombreTablaZonas))
            return Result<Dictionary<string, int>>.Failure("Raffle not found or has no associated zones table.");
        await _unitOfWork.ParticipantRepository.GenerateParticipantAsync(TablaParticipantes, nombreTablaZonas);
        var areas = await _unitOfWork.ParticipantRepository.GetAreasAsync(nombreTablaZonas);
        return Result<Dictionary<string, int>>.Success(areas);
    }

    private static Result<string> DeleteDirectory(string file)
    {
        try
        {
            Directory.Delete(Path.GetDirectoryName(file) ?? string.Empty, true);
            return Result<string>.Success(string.Empty);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }

    private static SqlMetaData[] GetMetadatas()
    {
        SqlMetaData[] metaDatas = new[]{
            new SqlMetaData("Folio", SqlDbType.BigInt),
            new SqlMetaData("CIF", SqlDbType.NVarChar, 100),
            new SqlMetaData("Nombre", SqlDbType.NVarChar, 100),
            new SqlMetaData("SegundoNombre", SqlDbType.NVarChar, 100),
            new SqlMetaData("PrimerApellido", SqlDbType.NVarChar, 100),
            new SqlMetaData("SegundoApellido", SqlDbType.NVarChar, 100),
            new SqlMetaData("Telefono", SqlDbType.NVarChar, 20),
            new SqlMetaData("Domicilio", SqlDbType.NVarChar, 100),
            new SqlMetaData("Estado", SqlDbType.NVarChar, 50),
            new SqlMetaData("Plaza", SqlDbType.NVarChar, 50),
            new SqlMetaData("IdZona", SqlDbType.Int)
        };
        return metaDatas;
    }
    #endregion
}
