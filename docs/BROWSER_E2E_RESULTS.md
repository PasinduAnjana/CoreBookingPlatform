# Browser end-to-end verification

Date: 15 November 2026. Target: the running Docker application at `http://127.0.0.1:3000`, using its real gateway, core services, SQL Server and ABC/CDE mock suppliers. Browser interactions used a separate in-app browser session, preserving the existing Chrome cart.

Result: the tested customer and management flows passed. No blocking application defects were found in this run. No source changes were needed.

| Flow | Observed result |
| --- | --- |
| Supplier discovery and catalog import | Both ABC and CDE imported through Management: two updated products each, zero new products and zero failures. |
| Categories | Created and edited a temporary category, assigned it to a product, and verified that deleting it while assigned shows the conflict message. |
| Product settings | Deactivated Mountain Retreat, confirmed it disappeared from customer browsing, and confirmed its direct booking page prevented booking. Restored its original category and active state. |
| Product content | Saved a temporary description, verified it on the customer product page, and restored the original description. |
| Catalog filters | Activities filter displayed the two activity products and hid hotels. |
| Room inventory input | Mountain Suite showed one room available for 23–25 November. Typing 99 stayed capped at one. |
| Occupancy validation | Four adults in one Mountain Suite were rejected; a valid selection could be added. |
| Hotel cart editing | Cancelling an edit preserved the original guests. Saving an edit persisted two adults and zero children. |
| Mixed cart and pricing | A two-night Mountain Suite cost LKR 60,000. Morning Group Tour initially cost LKR 5,000 for one adult. Editing it to two adults and one child returned LKR 12,500, making the total LKR 72,500. |
| Cart removal | Removing the activity left one hotel selection and a LKR 60,000 total. Re-adding it restored the mixed cart. |
| Checkout | Confirmed the LKR 72,500 order with separate ABC and CDE confirmation references. The browser cart was released after checkout. |
| Persistence and lookup | Confirmed order survived a full reload, appeared under My bookings and Management, and opened using its order reference. |
| Booking details | Both customer and management screens showed saved dates, room/package selection, guests, rooms/time slot, supplier product/option references, currencies, prices and booking statuses. |
| Order customer editing | A whitespace-only name was rejected. A valid name saved successfully while supplier references, statuses and the total remained unchanged. |
| Sold-out protection | While the hotel booking was confirmed, the same stay showed no rooms available; the Rooms input and Add to cart were disabled. |
| Cancellation | Cancelling the test order returned Cancelled for the main order and both supplier bookings. |
| Inventory release | After cancellation, the same hotel dates again showed one room available and allowed selection. |
| Mobile order details | Inspected at a 390 × 844 viewport. Booking details stacked vertically without horizontal overflow. The temporary viewport override was reset. |

## Test order

- Order: `822a247b-46ce-4a3e-af7f-71f3f49b4e32`
- Customer label: `E2E browser verification — edited`
- Total: LKR 72,500
- Final order and supplier booking statuses: Cancelled
- Hotel: Mountain Retreat / SUITE, 23–25 November 2026, two adults, zero children, one room.
- Activity: Colombo City Walking Tour / MORNING, 24 November 2026 at 09:00, two adults and one child.

The cancelled test order remains in the database as an audit record. Its supplier reservations were cancelled. The temporary category was removed after restoring the product assignment. Product settings and all four products' content were compared with the pre-test backup and preserved. Import/settings update timestamps changed as expected.

## Scope

This was a browser acceptance run against the local assignment/demo stack. It did not fault-inject supplier outages, uncertain booking responses, compensation failures or failed cancellation retries. Those scenarios have separate automated checks. Authentication, payments, load testing and production deployment were outside this run.
