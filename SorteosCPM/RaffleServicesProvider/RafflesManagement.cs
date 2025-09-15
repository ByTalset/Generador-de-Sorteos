using ConnectionManager;
using DbServicesProvider;
using DbServicesProvider.Dto;
using DbServicesProvider.Services;
using Microsoft.Extensions.Configuration;
using RaffleServicesProvider.Dto;
using RaffleServicesProvider.Interfaces;

namespace RaffleServicesProvider;

public class RafflesManagement
{
    private readonly DbServices _dbServices;
    private readonly IFileGenerator _fileGenerator;
    private readonly IImageFileManagement _imageFile;
    public RafflesManagement(DbServices dbServices, IFileGenerator fileGenerator, IImageFileManagement imageFile, IConfiguration configuration)
    {
        _dbServices = dbServices;
        _fileGenerator = fileGenerator;
        _imageFile = imageFile;
    }

    public async Task<Result<List<DescriptionRaffleDto>>> GetRegisteredRaffles()
    {
        Result<List<Raffle>> result = await _dbServices.GetAllRafflesAsync();
        if (!result.IsSuccess)
        {
            return Result<List<DescriptionRaffleDto>>.Failure(result.Error);
        }
        Result<List<DescriptionRaffleDto>> descriptors = await _imageFile.GetImageAsync(result.Value);
        return descriptors;
    }

    public async Task<Result<bool>> RegisteredRaffle(string nombreSorteo, FileDto? image)
    {
        Result<string> route = new(string.Empty, false, string.Empty, 0);
        if (image != null)
        {
            route = await _imageFile.StoreImageAsync(image);
        }
        return await _dbServices.InsertRaffleAsync(nombreSorteo, route.Value);
    }

    public async Task<Result<bool>> PrizeLoadingAsync(int idSorteo, FileDto file)
    {
        var premios = await _fileGenerator.LoadPrizesAreasAsync(file);
        if (!premios.IsSuccess)
            return Result<bool>.Failure(premios.Error);
        var result = await _dbServices.SaveLoadAsync(idSorteo, premios.Value);
        if (!result.IsSuccess)
            return Result<bool>.Failure(result.Error);
        return Result<bool>.Success(result.Value);
    }

    public async Task<Result<Awards>> GetAwardCurrentAync(int idSorteo)
    {
        var premios = await _dbServices.GetAwardsAync(idSorteo);
        if (!premios.IsSuccess)
            return Result<Awards>.Failure(premios.Error);
        return Result<Awards>.Success(premios.Value!);
    }

    public async Task<Result<Participants>> ExcuteRaffleAync(int idSorteo)
    {
        try
        {
            var cacheValue = await GetAwardCurrentAync(idSorteo);
            if (cacheValue.Value is null)
                return Result<Participants>.Success(default!);
            var participants = await _dbServices.GetParticipantAsync(idSorteo, cacheValue.Value.IdZona);
            if (!participants.IsSuccess)
                return Result<Participants>.Failure(participants.Error);
            if (participants.Value.IdParticipante > 0)
            {
                var insertSuccess = await _dbServices.WinnerAsync(participants.Value.IdParticipante, participants.Value.CIF, cacheValue.Value.IdZona, cacheValue.Value.IdPremio, idSorteo);
                if (!insertSuccess.IsSuccess)
                    return Result<Participants>.Failure(insertSuccess.Error);
            }
            return Result<Participants>.Success(participants.Value);
        }
        catch (Exception ex)
        {
            return Result<Participants>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<Participants>>> PrintListWinnerAsync(int idSorteo, int? idZona)
    {
        var winners = await _dbServices.GetWinnersAsync(idSorteo, idZona);
        if (!winners.IsSuccess)
            return Result<List<Participants>>.Failure(winners.Error);
        return Result<List<Participants>>.Success(winners.Value);
    }

    public async Task<Result<List<Zona>>> PrintListAreasAsync(int idSorteo)
    {
        var zonas = await _dbServices.GetAreasAsync(idSorteo);
        if (!zonas.IsSuccess)
            return Result<List<Zona>>.Failure(zonas.Error);
        return Result<List<Zona>>.Success(zonas.Value);
    }

    public async Task<Result<bool>> RaffleSettingAsync(int idSorteo, int options)
    {
        Result<bool> result = new(false, false, string.Empty, 0);
        if (options != 1)
        {
            result = await _dbServices.DeleteAsync(idSorteo);
            if (!result.IsSuccess) return Result<bool>.Failure(result.Error);
            return result;
        }
        result = await _dbServices.ResetAsync(idSorteo);
        if (!result.IsSuccess) return Result<bool>.Failure(result.Error);
        return Result<bool>.Success(result.Value);
    }

    public async Task<Result<bool>> EditRaffleAsync(int idSorteo, string? nombreSorteo, string? permiso, FileDto? image)
    {
        Result<string> routeImageNew = new(null!, false, string.Empty, 0);
        if (image != null)
        {
            Result<string> routeImageOld = await _dbServices.GetImageAsync(idSorteo);
            if (!routeImageOld.IsSuccess) return Result<bool>.Failure(routeImageOld.Error);
            routeImageNew = await _imageFile.UpdateImageRaffleAsync(routeImageOld.Value, image);
            if (!routeImageNew.IsSuccess) return Result<bool>.Failure(routeImageNew.Error);
        }
        var update = await _dbServices.EditAsync(idSorteo, nombreSorteo, permiso, routeImageNew.Value);
        if (!update.IsSuccess) return Result<bool>.Failure(update.Error);
        return update;
    }

    public async Task<Result<LoadFile>> ProcessConsultantAsync(int idSorteo, Guid processId)
    {
        return await _dbServices.ProcessAsync(idSorteo, processId);
    }
}
