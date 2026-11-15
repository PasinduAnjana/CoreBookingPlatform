using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Common;
using System.Text.RegularExpressions;

namespace IntegrationService.Services
{
    public class SupplierAdapterRegistry
    {
        private readonly Dictionary<string, ISupplierAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

        public SupplierAdapterRegistry(IEnumerable<ISupplierAdapter> adapters)
        {
            foreach (var adapter in adapters)
            {
                if (string.IsNullOrWhiteSpace(adapter.SupplierCode) ||
                    !Regex.IsMatch(adapter.SupplierCode, "^[A-Z0-9][A-Z0-9_-]{0,49}$") ||
                    string.IsNullOrWhiteSpace(adapter.SupplierName) || adapter.SupplierName.Length > 200)
                    throw new InvalidOperationException("Adapters require an uppercase supplier code and a name of at most 200 characters.");

                if (!_adapters.TryAdd(adapter.SupplierCode, adapter))
                    throw new InvalidOperationException($"Duplicate supplier adapter code '{adapter.SupplierCode}'.");
            }
        }

        public IEnumerable<ISupplierAdapter> Adapters => _adapters.Values;

        public List<SupplierInfo> GetSuppliers() => _adapters.Values
            .OrderBy(a => a.SupplierCode)
            .Select(a => new SupplierInfo { Code = a.SupplierCode, Name = a.SupplierName }).ToList();

        public ISupplierAdapter GetAdapter(string supplierCode)
        {
            if (string.IsNullOrWhiteSpace(supplierCode))
                throw new ArgumentException("Supplier code is required.");

            return _adapters.TryGetValue(supplierCode.Trim(), out var adapter)
                ? adapter
                : throw new KeyNotFoundException($"Supplier '{supplierCode}' is not supported.");
        }
    }
}
