# ABC mock catalog

Edit `hotels.json` to change the predefined hotels, room options, content, currency, nightly rates and starting room inventory.

- `hotels`: the catalog returned by the existing hotel API. Its public response shape is unchanged.
- `roomTypes`: pricing/inventory settings keyed by room code. As before, each hotel has its own inventory for that code. Rates are per room per night.
- `currency`: the currency used for quotes and bookings.

The singleton `HotelMockData` loads and validates this file at application startup using the content-root path. Restart ABC after editing the file. Missing/invalid JSON or missing room pricing prevents startup instead of silently using fallback data. JSON is copied to build and publish output automatically.

Bookings and idempotency keys remain in memory and are not written into this file. Restarting the mock clears them. Availability, booking and cancellation share one locked store.

To update already imported core products/content after changing the catalog, restart the mock and run ABC's catalog import from Management. Live quotes use the mock's rates directly.

Run `scripts/Test-MockSuppliers.ps1` from PowerShell 7 against fresh, isolated mock instances on ports 15102 (ABC) and 15101 (CDE). The script verifies catalog contracts, rates, inventory, idempotency and cancellation without accessing the core databases.

Image URLs in this dataset request compressed 600px-wide files. The frontend uses these URLs as returned, without rewriting them.
