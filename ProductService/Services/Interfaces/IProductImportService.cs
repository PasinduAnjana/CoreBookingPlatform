using ProductService.DTOs;

namespace ProductService.Services.Interfaces
{
    public interface IProductImportService
    {
        Task<ImportProductResultDto> ImportAsync(ImportProductDto dto);
    }
    public class ImportProductResultDto
    {
        public int ProductId { get; set; }
        public bool Created { get; set; }
    }
}
