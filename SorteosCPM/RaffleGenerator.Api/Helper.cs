using RaffleServicesProvider;

namespace RaffleGenerator.Api;

public static class Helper
{
    public static async Task<FileDto> ConvertFile(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        FileDto dto = new()
        {
            FileName = Path.GetFileNameWithoutExtension(file.FileName),
            Extension = Path.GetExtension(file.FileName),
            Contenido = ms.ToArray()
        };

        return dto;
    }
}
