# Core Booking frontend

React, TypeScript and Vite frontend for the existing Core Booking Platform. Uses Tailwind CSS, shadcn/ui (Radix), React Router, Axios, Lucide and Sonner.

## Setup

1. Install Node.js (a version supported by the installed Vite release).
2. From `Frontend`, run `npm install`.
3. Copy `.env.example` to `.env.local`. The default gateway is `https://localhost:7000`.
4. Trust the local ASP.NET HTTPS certificate with `dotnet dev-certs https --trust`.
5. Configure `ConnectionStrings:DefaultConnection` for ProductService and OrderService using user secrets or environment variables. Apply their EF migrations. No database credentials are stored in this frontend.
6. Start all six backend projects with their `https` launch profiles (or the solution's multiple-project launch configuration). The services' HTTP ports must also be available for their existing internal clients.
7. In Development, open Management and use **Sync supplier catalog** to import ABC and CDE. Imports remain development-only. Reimport CDE if older products do not contain supported time slots.
8. Run `npm run dev`, then open `http://localhost:5173`.

All React API calls go exclusively through Ocelot. The gateway allows the localhost/127.0.0.1 Vite origins on port 5173. Set an appropriate CORS origin in the gateway when changing the frontend origin. The Vite port is fixed to avoid silently changing origins.

## Pages and flow

- `/`: active products with hotel/activity filters, supplier images and content.
- `/products/:id`: details, room/package selection, occupancy validation and dates. Activity time slots are read from imported attributes.
- `/cart`: mixed supplier selections, removal and fresh live quotes.
- `/checkout`: latest quote, customer name and supplier booking confirmation.
- `/orders/:orderId`: order and supplier booking statuses/references, cancellation and retry.
- `/bookings`: recent order references on this browser and manual order lookup.

Only IDs are stored locally; cart contents, prices and order statuses are fetched from the backend. Quotes are refreshed on page entry and before checkout by the backend. Checkout handles HTTP 409 order bodies as failed orders and HTTP 202 bodies as unconfirmed orders. Once any order is returned, the active cart is detached because the backend allows one order per cart. A network failure retains the same cart for an idempotent retry.

## Verification

`npm run build` runs TypeScript checking followed by a Vite production build. `npm run lint` runs Oxlint. For a separate type check: `npx tsc -b`.

Manual acceptance flow (requires running services and SQL Server):

1. Open an ABC hotel, choose valid room occupancy and future check-in/out dates, and add it.
2. Open a CDE activity, choose a package and a returned time slot, and add it.
3. Open the cart, confirm both items have live availability and prices, and refresh the quote.
4. Continue to checkout, enter a name, confirm once, and verify the returned order and both supplier confirmations.
5. Reload the order page, cancel through the confirmation dialog, and verify every booking status.
6. Test removal, empty cart, missing references, supplier outages and unavailable inventory. Confirm unavailable/mixed-currency quotes block checkout.

## Small backend integration changes

- Ocelot exposes GET/PUT/DELETE `/api/products/{productId}/content`, category CRUD, order listing and supplier imports. Catalog routes remain singular `/api/product`.
- Ocelot allows CORS for the two local Vite origins.
- CDE publishes its actual supported time slots in its catalog; its availability and booking validation use the same list. The adapter preserves that list in imported attributes.

## Current backend limitations

- No cart-item update endpoint: remove an item and add a revised selection.
- No customer accounts: customer order lookup uses locally saved references. Management uses the paginated order-list API.
- Order responses contain supplier booking summaries, not the original dates/guest selections. Those are displayed during cart/checkout only.
- Uncertain bookings require backend/operator reconciliation; the UI refreshes their actual status and does not claim automatic recovery.
- Supplier mocks keep bookings in memory and lose inventory reservations on restart.
- Missing activity time slots require reimport, rather than frontend defaults.
- Browser storage can be cleared or disabled; retain order references externally when needed.

For a production host, configure SPA fallback to `index.html` for the client routes. No public hosting is configured for this localhost backend.

## Verified during implementation

- TypeScript check and Vite production build passed.
- Oxlint passed without warnings.
- The complete .NET solution builds successfully. A clean rebuild reports existing nullable-reference warnings in ProductService.
- Browser verification against the real local services covered: ABC Deluxe Room selection, CDE Morning Group Tour selection, one mixed cart, live LKR 41,000 quote, successful checkout with two supplier confirmations, cancellation of both mock bookings, order lookup and mobile order layout.
- The verification order was created as `Frontend verification` and then cancelled. CDE's two existing catalog entries were reimported to populate time slots.

Failure states are implemented, but supplier timeout, compensation and failed-cancellation scenarios were not fault-injected during this browser run.


## Management application

Use **Management** in the main navigation, or open `/management`.

- `/management`: all imported products, including inactive ones; supplier references, categories, timestamps and catalog import.
- `/management/products/:id`: category assignment, active state, read-only supplier attributes and product content editing. Content supports descriptions, image URLs/captions/primary selection, highlights, facilities, inclusions, exclusions and additional JSON. Content deletion has an explicit confirmation.
- `/management/categories`: create, edit and delete internal categories. Assigned categories return HTTP 409 until products are reassigned.
- `/management/orders`: server-paginated order list, newest first.
- `/management/orders/:orderId`: associated supplier bookings, confirmation references, statuses and the existing supported cancellation/retry workflow. Viewing management orders does not add them to the customer's local recent-order history.

Uses the official shadcn Table, Dialog, AlertDialog, Switch, Textarea, Select, Card, Badge, Button, Label and Input components. No dashboard statistics or fake catalog/order data are used.

### Backend additions for management

- `GET /api/orders?page=1&pageSize=20`: returns `{ items, totalCount, page, pageSize }`. Page size is limited to 100; invalid/overflowing pagination returns HTTP 400. Data is ordered by creation time and ID for stable pagination. No migration is required.
- Product category validation returns HTTP 400 instead of an unhandled error.
- Category deletion returns HTTP 409 when the category is assigned to products.
- The gateway forwards the management API routes to their owning services. Imports retain the existing Development-only checks.

Reimport updates supplier-managed catalog fields and overwrites local content. It preserves existing category assignments and active states. The import dialog explains this before a run and displays created, updated and failed counts for each supplier. No arbitrary booking-status editing or order-customer editing is added; the existing cancellation API remains the supported status transition.

The management area shares the existing Vite application and runs as a local assignment demonstration. Authentication and user roles are not implemented; this is not a secured administration portal for public deployment.

### Management verification

Browser checks against the real local gateway passed for category creation/editing, assignment, product activation, content save/create/delete, deletion confirmation, assigned-category protection, order listing/details and both supplier imports (two updates each, zero failures). Mobile navigation and table layout were inspected. Read-only API checks verified distinct pagination results and HTTP 400 for invalid pagination. The temporary category/content were removed and the original product description/category/active state were restored. Supplier imports refreshed the four supplier catalog entries.
