using ConnectionManager;
using DbServicesProvider;
using DbServicesProvider.Dto;
using RaffleServicesProvider.Dto;

namespace RaffleServicesProvider.Interfaces;

public interface IImageFileManagement
{
    Task<Result<string>> StoreImageAsync(FileDto image);
    Task<Result<List<DescriptionRaffleDto>>> GetImageAsync(List<Raffle> raffles);
    Task<Result<string>> UpdateImageRaffleAsync(string oldFile, FileDto image);
}
