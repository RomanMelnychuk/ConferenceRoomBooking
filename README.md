# Conference Room Booking API

REST API for managing conference rooms, searching for available rooms and booking them with automatic price calculation based on time-of-day tariffs.

Built as a test task. The task description is in Ukrainian; this README covers the business tasks, the technical decisions and the assumptions made where the task was ambiguous.

## Tech stack

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10 + SQL Server (LocalDB by default)
- Swagger UI on top of the built-in .NET OpenAPI document
- Built-in ASP.NET Core rate limiting

## Getting started

**Prerequisites:** .NET 10 SDK and SQL Server LocalDB (installed together with Visual Studio). To use another SQL Server instance, change `ConnectionStrings:DefaultConnection` in `appsettings.json`.

```bash
git clone https://github.com/RomanMelnychuk/ConferenceRoomBooking.git
cd ConferenceRoomBooking
dotnet run --project ConferenceRoomBooking.Api
```

Then open **http://localhost:5062/swagger**.

On the first run the application creates the database, applies migrations and seeds the initial data from the task: rooms A (50 people, 2000 UAH/h), B (100, 3500) and C (30, 1500), and services Projector (500), Wi-Fi (300) and Sound (700).

Ready-to-run request examples for every scenario are in `ConferenceRoomBooking.Api/ConferenceRoomBooking.Api.http` (can be executed directly from Visual Studio or Rider).

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/rooms` | All rooms with their available services |
| GET | `/api/rooms/{id}` | One room |
| GET | `/api/rooms/available?date=&startTime=&endTime=&capacity=` | Rooms that fit the capacity and are free for the whole time slot |
| POST | `/api/rooms` | Create a room, returns 201 with the new id |
| PUT | `/api/rooms/{id}` | Update a room and replace its list of services |
| DELETE | `/api/rooms/{id}` | Delete a room (not allowed if it has bookings) |
| POST | `/api/bookings` | Book a room, returns the booking with the calculated total price |
| GET | `/api/bookings/{id}` | One booking |
| GET | `/api/reports/rooms?from=&to=` | Revenue and utilization per room |
| GET | `/api/reports/services?from=&to=` | Popularity and revenue per service |

> **Dates must be in the future.** The example in the task uses 2024-09-01, which the API rejects with 400 because booking in the past is not allowed. Use any future date, e.g. `2026-12-01`.

> **Times are local, without a timezone.** Send `2026-12-01T10:00:00`, not `2026-12-01T10:00:00Z`.

**Booking example:**

```json
POST /api/bookings
{
  "roomId": 1,
  "startTime": "2026-12-01T11:00:00",
  "durationHours": 2,
  "serviceIds": [1]
}
```

Response `201 Created` with `"totalPrice": 4800.00`: 2000 for 11:00 (standard) + 2300 for 12:00 (peak, +15%) + 500 for the projector.

## Pricing

The price is calculated **hour by hour**, because one booking can span several tariff zones:

| Hours | Tariff | Room price multiplier |
|---|---|---|
| 06:00–09:00 | Morning | ×0.9 (−10%) |
| 09:00–12:00, 14:00–18:00 | Standard | ×1.0 |
| 12:00–14:00 | Peak | ×1.15 (+15%) |
| 18:00–23:00 | Evening | ×0.8 (−20%) |

Services are charged **once per booking**, not per hour. The total is rounded to 2 decimals (`MidpointRounding.AwayFromZero`).

## Assumptions (gaps in the task)

| Question the task leaves open | Decision |
|---|---|
| Peak 12:00–14:00 lies inside standard 09:00–18:00 | Peak replaces the standard tariff for these hours; multipliers are not combined |
| Hours 23:00–06:00 are not described | Bookings are not allowed; the API returns 400 with an explanation |
| Are services paid per hour or once? | Once per booking: "per hour" is stated only for the room |
| Can a booking start at 10:30? | No, bookings are made in whole hours only |
| Can a booking cross midnight? | No, a booking must start and end on the same day |
| Booking in the past | Not allowed |
| Are services global or per room? | One shared catalog of services; each room has a set of available services (many-to-many). A booking may only include services available in that room |
| Which services do the initial rooms have? | All three |
| Deleting a room that has bookings | Not allowed (409). Otherwise cascade delete would silently remove bookings and corrupt reports |
| Cancelling or changing a booking | Not in the task, not implemented (see future improvements) |

## Technical decisions

**Architecture.** Layers: controller → service → `DbContext`. Controllers only handle HTTP; business logic lives in services behind interfaces (`IRoomService`, `IBookingService`, `IReportService`) registered in DI. There is no separate repository layer: `DbContext` already implements the repository and unit-of-work patterns, and an extra wrapper around it would only duplicate it in a project of this size.

**Pricing isolated in one class.** `PriceCalculator` contains only the tariff logic and does not depend on the database, so tariffs can be changed or covered with unit tests in one place. It is registered as a singleton because it is stateless.

**Price is stored in the booking.** `Booking.TotalPrice` is calculated once at booking time. If the room price changes later, existing bookings keep the price the client agreed to.

**`decimal` for money.** `double` stores values in binary and cannot represent most decimal fractions exactly, so sums drift. In the database money columns are `decimal(18,2)`.

**DTOs instead of entities.** The API never exposes EF entities: this prevents clients from setting fields they should not (over-posting) and avoids serialization cycles (Room → Services → Rooms).

**Availability check.** Two time intervals overlap when `start1 < end2 && start2 < end1`. Bookings 10:00–12:00 and 12:00–14:00 do not overlap. The search is translated by EF into a SQL `NOT EXISTS` subquery, so filtering happens in the database.

**Protection against double booking.** Checking that the slot is free and inserting the booking run in one transaction with `Serializable` isolation. Without it, two parallel requests could both see the slot as free and both create a booking.

**Consistent errors.** A global middleware converts domain exceptions to HTTP statuses (`NotFoundException` → 404, `BadRequestException` → 400, `ConflictException` → 409) and returns them in the standard `ProblemDetails` format (RFC 7807). Unexpected errors return 500 with a generic message; details go only to the log, never to the client.

## Security and fault tolerance

- **Input validation** with data annotations on every request DTO; invalid requests get 400 with a list of errors before reaching business logic.
- **Business rule validation** in `BookingTimeRules`: working hours, whole hours, same day, no past dates.
- **Rate limiting:** 100 requests per minute per client IP; exceeding it returns 429 with an explanation.
- **Double-booking protection** with a serializable transaction.
- **No internal details in error responses.**
- **SQL injection** is prevented by EF Core, which always uses parameterized queries.
- **Report period is limited to 366 days** so a single request cannot load the whole history.

## Reports

| Report | Shows | Business value |
|---|---|---|
| `/api/reports/rooms` | Per room: number of bookings, booked hours, revenue, utilization % | Which rooms earn money and which stand idle — a basis for discounts or repurposing |
| `/api/reports/services` | Per service: times ordered, revenue | Which services are worth investing in and which can be dropped |

Utilization = booked hours / available working hours (17 per day, 06:00–23:00) for the period.

## Tests

Unit tests for `PriceCalculator` (xUnit) cover every tariff zone, a booking that crosses tariff zones, one-time service fees, rounding and invalid input. The calculator does not depend on the database, so the tests need no setup.

```bash
dotnet test
```

## Known limitations and future improvements

- **Authentication and roles.** Room management endpoints are currently open to any client. Next step: JWT with an admin role for creating, editing and deleting rooms.
- **Booking cancellation** endpoint.
- **Service price snapshot.** Service prices are not stored per booking, so the services report uses current prices. Storing the price in the booking–service link would make historical reports exact.
- **Soft delete for rooms**, so rooms with a booking history can be archived instead of blocked from deletion.
- **Configurable tariffs.** Tariff zones are defined in code; if the business needs to change them without redeploying, they can be moved to configuration or the database.
- **Report aggregation in SQL** for large data volumes (the room report currently aggregates in memory).
- **More tests:** unit tests for `BookingTimeRules` and integration tests for the API endpoints.
- **Retry on concurrency conflicts.** Under heavy parallel load on the same room, a serializable transaction can be chosen as a deadlock victim; such requests could be retried automatically.
- **Time zones.** All times are treated as local time of the venue; a multi-location setup would need explicit time zones.
- **Naming.** The entity `Service` (an amenity like a projector) can be confused with the service layer; `Amenity` would be a clearer name.
- **Migrations on startup** are convenient for a demo; in production they would be applied as a separate deployment step.

## Project structure

```
ConferenceRoomBooking.Api/
├── Controllers/     HTTP endpoints: Rooms, Bookings, Reports
├── Services/        Business logic, PriceCalculator, BookingTimeRules
├── Dtos/            Request and response contracts
├── Models/          EF Core entities: ConferenceRoom, Service, Booking
├── Data/            AppDbContext and DbSeeder
├── Exceptions/      NotFound, BadRequest and Conflict exceptions
├── Middleware/      Global exception handling
├── Migrations/      EF Core migrations
└── ConferenceRoomBooking.Api.http   Request examples for all scenarios

ConferenceRoomBooking.Tests/
└── PriceCalculatorTests.cs   Unit tests for the pricing logic
```