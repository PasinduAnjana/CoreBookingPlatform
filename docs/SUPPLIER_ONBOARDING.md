# Supplier onboarding

## Existing product types

A new supplier offering Hotel or Activity products needs an adapter, DI/API configuration, and catalog sync. It does not need changes to core business logic, React components, gateway routes, or supplier seed migrations.

Implement `ISupplierAdapter` with a unique uppercase `SupplierCode`, a `SupplierName`, and the existing catalog, availability, booking, and cancellation methods. Set each normalized product's `ProductType` to `Hotel` or `Activity` and populate the common attributes:

| Type | Imported attributes |
| --- | --- |
| Hotel | `options`: `supplierOptionId`, `name`, `maxAdults`, `maxChildren`, optional `bedType`. Capacity is per room. |
| Activity | `timeSlots` and `options`: `supplierOptionId`, `label`, `maxParticipants`, `passengerTypes` such as `Adult` and `Child`. |

Map supplier-specific dates, amounts, references, and errors inside the adapter. Product type belongs to each product; one supplier may provide both types. Changing an existing supplier product reference's type is rejected to protect saved selections. New product types or requirements outside these contracts still require explicit support.

Return `SupplierContracts.SupplierBookingStatus` for booking/cancellation results. Map only the new supplier's documented statuses; do not pass native status strings through or assume another supplier's aliases. Missing/unrecognized statuses must remain `Unknown`, and confirmed/cancelled results need a supplier reference.

Map HTTP/business errors to `SupplierError` and throw `SupplierOperationException`. Set `Outcome = Rejected` only when the supplier contract establishes that the operation was rejected, using `InvalidRequest`, `NotFound`, or `Conflict`. Otherwise retain the default `Unknown` outcome. Use `SupplierResponseReader.SendAsync` for transport, timeout, and JSON error normalization and provide the adapter's own HTTP-error mapping. The reader intentionally discards upstream error bodies. Cancellation rejection does not mean the original booking has been cancelled. See the root README's supplier status/error contract and the ABC/CDE adapters for examples.

## Register and configure the adapter

Follow the existing registration style in IntegrationService `Program.cs`:

```csharp
builder.Services.AddHttpClient<XyzSupplierAdapter>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SupplierApis:XYZ"]
        ?? throw new InvalidOperationException("XYZ API URL is missing."));
});
builder.Services.AddTransient<ISupplierAdapter>(services =>
    services.GetRequiredService<XyzSupplierAdapter>());
```

Configure `SupplierApis:XYZ` locally or `SupplierApis__XYZ` in the IntegrationService environment in Compose. If the supplier is another container, configure its service/network as well. The registry validates adapter metadata and rejects duplicate codes at startup. No reflection or runtime plugin loading is used.

Rebuild IntegrationService, open **Management → Sync supplier catalog**, select the discovered supplier, and run import. The API alternative is `POST /api/integration/import/XYZ`.

`GET /api/integration/suppliers` discovers adapters independently of imported products, so a new supplier appears before its first sync. The orchestrator calls ProductService's Development-only `POST /api/internal/suppliers/register`, which creates a supplier by code if missing. Concurrent registration returns the same supplier ID. Existing names and inactive states are preserved; an inactive supplier returns 409. Reimports retain product IDs, category assignments, and product active states, with the existing content-overwrite behavior.

## One-time database upgrade

Apply `AddProductType` to ProductDB before running the updated services. It backfills existing ABC products as Hotel and CDE products as Activity without replacing records or changing supplier/product IDs, content, categories, active states, carts, or orders. An unknown legacy supplier causes the migration to stop and roll back rather than guess a product type.

With Compose SQL Server running, use PowerShell 7 from the repository root:

```powershell
$upgradePassword = Read-Host -MaskInput "Enter the SQL password from .env"
$env:ConnectionStrings__DefaultConnection = "Server=tcp:127.0.0.1,14333;Database=ProductDB;User Id=sa;Password=$upgradePassword;Encrypt=True;TrustServerCertificate=True;"
dotnet ef database update --project ProductService/ProductService.csproj --startup-project ProductService/ProductService.csproj
Remove-Item Env:\ConnectionStrings__DefaultConnection
Remove-Variable upgradePassword

docker compose up -d --build product-service integration-service order-service api-gateway frontend
```

Use EF CLI 10.x as described in the root README. If using the repository-local tool installed during development, substitute `./artifacts/tools/dotnet-ef.exe` for `dotnet ef`.

No OrderDB migration or catalog reimport is required for the upgrade. Keep the ABC/CDE mock containers running when rebuilding the application services so their in-memory reservations are retained. Future supplier onboarding requires neither this migration again nor another seed edit.

## Verification

`tests/SupplierOnboardingChecks` uses real controllers/services, isolated ABC/CDE mock APIs, and generated disposable SQL databases. Its test-only XYZ adapter implements the existing hotel API contract. XYZ is not registered in the production application.

```powershell
$testPassword = Read-Host -MaskInput "Enter the SQL password from .env"
$env:SUPPLIER_CHECK_SQL_CONNECTION = "Server=tcp:127.0.0.1,14333;Database=master;User Id=sa;Password=$testPassword;Encrypt=True;TrustServerCertificate=True;"
dotnet run --project tests/SupplierOnboardingChecks/SupplierOnboardingChecks.csproj
Remove-Item Env:\SUPPLIER_CHECK_SQL_CONNECTION
Remove-Variable testPassword
```

The test login needs permission to create and drop its generated databases. Application ProductDB and OrderDB are not used. Checks cover migration backfill/rollback, duplicate adapters, auto-registration, concurrent registration, repeated sync, inactive states, cart edits, mixed checkout, idempotency, compensation, cancellation retry, and uncertain outcomes.

Add `-- --serve` to keep the disposable fixture running on `http://localhost:15173`; stop it to clean up. For browser verification, point a separate Vite process at that API with `VITE_API_BASE_URL=http://localhost:15173` and port 5175.
