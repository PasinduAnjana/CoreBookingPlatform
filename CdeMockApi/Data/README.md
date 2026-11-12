# CDE mock catalog

Edit `activities.json` to change the predefined activities, packages, passenger types, time slots, content, currency, prices and starting seat inventory.

- `activities`: the catalog returned by the existing activity API. Its public response shape is unchanged.
- `packages`: pricing/inventory settings keyed by package ID. Inventory is separate for each activity, package, date and time slot.
- `adultRate` and `childRate`: prices per participant. Child prices retain the previous half-price amounts but are now explicitly defined in JSON.
- `currencyCode`: the currency used for quotes and bookings.

The singleton `ActivityMockData` loads and validates this file at application startup using the content-root path. Restart CDE after editing the file. Missing/invalid JSON, missing time slots or missing package pricing prevents startup instead of silently using fallback data. JSON is copied to build and publish output automatically.

Bookings and idempotency keys remain in memory and are not written into this file. Restarting the mock clears them. Availability, booking and cancellation share one locked store.

To update already imported core products/content or time slots after changing the catalog, restart the mock and run CDE's catalog import from Management. Live quotes use the mock's rates directly.

Run `scripts/Test-MockSuppliers.ps1` from PowerShell 7 against fresh, isolated mock instances on ports 15102 (ABC) and 15101 (CDE). The script verifies catalog contracts, rates, inventory, idempotency and cancellation without accessing the core databases.

Image URLs in this dataset request compressed 600px-wide files. The frontend uses these URLs as returned, without rewriting them.
