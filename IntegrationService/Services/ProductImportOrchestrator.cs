namespace IntegrationService.Services
{
    public class ProductImportOrchestrator
    {
        private readonly SupplierIntegrationService _supplierService;
        private readonly ProductServiceClient _productClient;
        private readonly SupplierAdapterRegistry _registry;

        public ProductImportOrchestrator(
            SupplierIntegrationService supplierService,
            ProductServiceClient productClient,
            SupplierAdapterRegistry registry)
        {
            _supplierService = supplierService;
            _productClient = productClient;
            _registry = registry;
        }

        public async Task<ImportSummary> ImportAsync(
            string supplierCode,
            CancellationToken cancellationToken = default)
        {
            var adapter = _registry.GetAdapter(supplierCode);
            await _productClient.RegisterSupplierAsync(
                new() { Code = adapter.SupplierCode, Name = adapter.SupplierName }, cancellationToken);
            var products = await _supplierService.GetProductsAsync(adapter.SupplierCode, cancellationToken);

            var summary = new ImportSummary
            {
                SupplierCode = adapter.SupplierCode,
                Total = products.Count
            };

            foreach (var product in products)
            {
                try
                {
                    if (!string.Equals(product.SupplierCode, adapter.SupplierCode, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Catalog product does not belong to the selected supplier.");
                    product.SupplierCode = adapter.SupplierCode;
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
