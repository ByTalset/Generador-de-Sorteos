using ConnectionManager;
using DbServicesProvider;
using DbServicesProvider.Dto;

namespace RaffleServicesProvider.Interfaces;

public interface IFileGenerator
{
    Task<Result<List<Awards>>> LoadPrizesAreasAsync(FileDto file);
    Task<Result<string>> SaveFileTemp(FileDto file);
}
