namespace IntegrationService.Services
{
    public class ProductImportOrchestrator
    {
        private readonly SupplierIntegrationService _supplierService;
        private readonly ProductServiceClient _productClient;

        public ProductImportOrchestrator(
            SupplierIntegrationService supplierService,
            ProductServiceClient productClient)
        {
            _supplierService = supplierService;
            _productClient = productClient;
        }

        public async Task<ImportSummary> ImportAsync(
            string supplierCode,
            CancellationToken cancellationToken = default)
        {
            var products = await _supplierService.GetProductsAsync(
                supplierCode, cancellationToken);

            var summary = new ImportSummary
            {
                SupplierCode = supplierCode,
                Total = products.Count
            };

            foreach (var product in products)
            {
                try
                {
                    var result = await _productClient
                        .ImportProductAsync(product, cancellationToken);

                    if (result == null)
                        throw new InvalidOperationException(
                            "ProductService returned an empty response.");

                    if (result.Created)
                        summary.Created++;
                    else
                        summary.Updated++;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    summary.Errors.Add(
                        $"{product.SupplierProductId}: {ex.Message}");
                }
            }

            return summary;
        }

        public class ImportSummary
        {
            public string SupplierCode { get; set; } = string.Empty;
            public int Total { get; set; }
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Failed { get; set; }
            public List<string> Errors { get; set; } = [];
        }
    }
}
