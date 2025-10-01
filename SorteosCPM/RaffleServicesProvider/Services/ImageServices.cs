using ConnectionManager;
using DbServicesProvider.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RaffleServicesProvider.Dto;
using RaffleServicesProvider.Interfaces;

namespace RaffleServicesProvider.Services;

public class ImageServices : IImageFileManagement
{
    private readonly ILogger<ImageServices> _logger;
    private readonly string _path;
    public ImageServices(IConfiguration configuration, ILogger<ImageServices> logger)
    {
        _logger = logger;
        _path = Path.Combine(configuration.GetSection("Directory:PathImages").Value ?? "C:\\SorteosCPM");
        if (!Directory.Exists(_path))
            Directory.CreateDirectory(_path);
    }
    public async Task<Result<string>> StoreImageAsync(FileDto image)
    {
        try
        {
            var route = Path.Combine(_path, $"{image.FileName}{image.Extension}");
            await File.WriteAllBytesAsync(route, image.Contenido);
            _logger.LogInformation("It is obtained {route} from the response.", route);
            return Result<string>.Success(route);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<string>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<DescriptionRaffleDto>>> GetImageAsync(List<Raffle> raffles)
    {
        List<DescriptionRaffleDto> descriptionRaffles = new();
        foreach (Raffle raffle in raffles)
        {
            try
            {
                string imagePath = Path.Combine(_path, raffle.RutaImagen);
                byte[]? imageBytes = File.Exists(imagePath) ? await File.ReadAllBytesAsync(imagePath) : null;
                string base64Image = imageBytes != null ? Convert.ToBase64String(imageBytes) : string.Empty;
                descriptionRaffles.Add(new DescriptionRaffleDto
                {
                    IdSorteo = raffle.IdSorteo,
                    NombreSorteo = raffle.NombreSorteo,
                    PermisoSegob = raffle.PermisoSegob,
                    ImagenSorteo = base64Image
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
                return Result<List<DescriptionRaffleDto>>.Failure(ex.Message);
            }
        }
        _logger.LogInformation("It is obtained {@descriptionRaffles} from the response.", descriptionRaffles.Select(d => new{IdSorteo = d.IdSorteo, Nombre = d.NombreSorteo}));
        return Result<List<DescriptionRaffleDto>>.Success(descriptionRaffles);
    }

    public async Task<Result<string>> UpdateImageRaffleAsync(string oldFile, FileDto image)
    {
        try
        {
            if (File.Exists(oldFile))
            {
                File.Delete(oldFile);
            }
            var route = Path.Combine(_path, $"{image.FileName}{image.Extension}");
            await File.WriteAllBytesAsync(route, image.Contenido);
            _logger.LogInformation("It is obtained {route} from the response.", route);
            return Result<string>.Success(route);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<string>.Failure(ex.Message);
        }
    }
}
