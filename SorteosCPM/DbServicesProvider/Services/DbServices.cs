using System.Security.Cryptography;
using ConnectionManager;
using DbServicesProvider.Dto;
using DbServicesProvider.Interfaces;
using Microsoft.Extensions.Logging;

namespace DbServicesProvider.Services;

public class DbServices
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DbServices> _logger;
    public DbServices(IUnitOfWork unitOfWork, ILogger<DbServices> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<Raffle>>> GetAllRafflesAsync()
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var raffles = await _unitOfWork.RaffleRepository.GetAllAsync();
            _logger.LogInformation("It is obtained {@raffles} from the response.", raffles);
            return Result<List<Raffle>>.Success(raffles);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<List<Raffle>>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> InsertRaffleAsync(string nombreSorteo, string? route)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var success = await _unitOfWork.RaffleRepository.InsertAsync(nombreSorteo, route);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {success} from the response.", success);
            return Result<bool>.Success(success);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> SaveLoadAsync(int idSorteo, List<Awards> premios)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            string nombreTablaZonas = $"{idSorteo}_Zonas";
            string nombreTablaPremios = $"{idSorteo}_Premios";
            await _unitOfWork.AwardsRepository.GenerateAreasAndAwardsAsync(nombreTablaPremios, nombreTablaZonas);
            Dictionary<string, int> zonasId = await _unitOfWork.AwardsRepository.InsertAreasAsync(premios, nombreTablaZonas);
            await _unitOfWork.AwardsRepository.InsertAwardsAsync(premios, zonasId, nombreTablaPremios);
            await _unitOfWork.RaffleRepository.UpdateRaffleAsync(idSorteo, nombreTablaZonas);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {nombreTablaZonas}\n{nombreTablaPremios}\n And the following prizes and zones are inserted {@zonasId}.", nombreTablaZonas, nombreTablaPremios, zonasId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<Awards>> GetAwardsAync(int idSorteo)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var awards = await _unitOfWork.RaffleRepository.GetAreasAndAwardsAsync(idSorteo);
            _logger.LogInformation("It is obtained {@awards} from the response.", awards);
            return Result<Awards>.Success(awards);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<Awards>.Failure(ex.Message);
        }
    }

    public async Task<Result<Participants>> GetParticipantAsync(int idSorteo, int idZona)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var idsZona = await _unitOfWork.ParticipantRepository.GetIdsParticipantsAsync(idSorteo, idZona);
            if (idsZona.Count == 0) return Result<Participants>.Failure("No participants assigned to this area were found.");
            int randomIndex = RandomNumberGenerator.GetInt32(0, idsZona.Count);
            int folio = idsZona[randomIndex];
            Console.WriteLine($"Folio: {folio}\nIdZona: {idZona}");
            var participant = await _unitOfWork.ParticipantRepository.GetParticipantsWithoutWinningAsync(idSorteo, idZona, folio);
            _logger.LogInformation("It is obtained {@participant} from the response.", participant);
            return Result<Participants>.Success(participant!);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<Participants>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> WinnerAsync(int IdParticipante, string cif, int idZona, int idPremio, int idSorteo)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var rowAffected = await _unitOfWork.ParticipantRepository.InsertWinnerAsync(IdParticipante, cif, idZona, idPremio, idSorteo);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {rowAffected} from the response.", rowAffected);
            return Result<bool>.Success(rowAffected);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<Participants>>> GetWinnersAsync(int idSorteo, int? idZona)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var winners = await _unitOfWork.ParticipantRepository.GetWinnersAsync(idSorteo, idZona);
            _logger.LogInformation("It is obtained {@winners} from the response.", winners);
            return Result<List<Participants>>.Success(winners);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<List<Participants>>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<Zona>>> GetAreasAsync(int idSorteo)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var zonas = await _unitOfWork.RaffleRepository.GetAreasAllAsync(idSorteo);
            _logger.LogInformation("It is obtained {@zonas} from the response.", zonas);
            return Result<List<Zona>>.Success(zonas);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<List<Zona>>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(int idSorteo)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var delete = await _unitOfWork.RaffleRepository.DeleteRaffleAsync(idSorteo);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {delete} from the response.", delete);
            return Result<bool>.Success(delete);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> ResetAsync(int idSorteo)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var reset = await _unitOfWork.RaffleRepository.ResetRaffleAsync(idSorteo);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {reset} from the response.", reset);
            return Result<bool>.Success(reset);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<string>> GetImageAsync(int idSorteo)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            string routeOld = await _unitOfWork.RaffleRepository.GetRouteImageAsync(idSorteo);
            _logger.LogInformation("It is obtained {routeOld} from the response.", routeOld);
            return Result<string>.Success(routeOld);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<string>.Failure(ex.Message);
        }
    }

    public async Task<Result<bool>> EditAsync(int idSorteo, string? nombreSorteo, string? permiso, string? routeImage)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var reset = await _unitOfWork.RaffleRepository.UpdateRaffleAsync(idSorteo, nombreSorteo, permiso, routeImage);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation("It is obtained {reset} from the response.", reset);
            return Result<bool>.Success(reset);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result<LoadFile>> ProcessAsync(int idSorteo, Guid processId)
    {
        await _unitOfWork.StartConnectionAsync();
        try
        {
            var process = await _unitOfWork.ParticipantRepository.GetProcessAsync(idSorteo, processId);
            _logger.LogInformation("It is obtained {@process} from the response.", process);
            return Result<LoadFile>.Success(process);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<LoadFile>.Failure(ex.Message);
        }
    }
}