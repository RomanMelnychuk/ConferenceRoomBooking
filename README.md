# Conference Room Booking API

REST API for managing conference rooms, searching for available rooms and booking them with automatic price calculation based on time-of-day tariffs.

Built as a test task. The task description is in Ukrainian; this README covers the business tasks, the technical decisions and the assumptions made where the task was ambiguous.

## Business tasks

A company rents out conference rooms to businesses. The API serves two groups of users:

| Who | Business task | Endpoints |
|---|---|---|
| Clients | Find a room that fits the number of people and is free at the needed time | `GET /api/rooms/available` |
| Clients | Book a room with the selected services and see the exact price | `POST /api/bookings` |
| Company | Manage rooms: add them, change price or capacity, attach services, remove them | `POST/PUT/DELETE /api/rooms` |
| Company | Manage the catalog of extra services (projector, Wi-Fi, sound, ...) | `POST /api/services` |
| Company | See which rooms and services earn money and which stand idle | `/api/reports/*` |

Prices follow demand: morning and evening hours are cheaper to attract clients, the lunch-time peak is more expensive.

## Tech stack

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10 + SQL Server (LocalDB by default)
- Swagger UI on top of the built-in .NET OpenAPI document
- xUnit for unit tests
- Built-in ASP.NET Core rate limiting and health checks

## Getting started

**Prerequisites:** .NET 10 SDK and SQL Server LocalDB (installed together with Visual Studio). To use another SQL Server instance, change `ConnectionStrings:DefaultConnection` in `appsettings.json`.

```bash
git clone https://github.com/RomanMelnychuk/ConferenceRoomBooking.git
cd ConferenceRoomBooking
dotnet run --project ConferenceRoomBooking.Api
```

Then open **http://localhost:5062/swagger**. When started from Visual Studio, Swagger opens automatically.

On the first run the application creates the database, applies migrations and seeds the initial data from the task: rooms A (50 people, 2000 UAH/h), B (100, 3500) and C (30, 1500), and services Projector (500), Wi-Fi (300) and Sound (700).

Ready-to-run request examples for every scenario are in `ConferenceRoomBooking.Api/ConferenceRoomBooking.Api.http` (can be executed directly from Visual Studio or Rider). Run them from top to bottom on a fresh database.

### Admin API key

Managing rooms and services and viewing reports require the admin API key in the `X-Api-Key` header.

- In Development the key is **`dev-admin-key-change-me`** (from `appsettings.Development.json`).
- In Swagger UI click **Authorize** and enter the key once; it will be sent with every request.
- In production the key is never stored in the repository: set the environment variable `Security__AdminApiKey`. If no key is configured, admin operations are unavailable.

## API

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/rooms` | Public | All rooms with their available services |
| GET | `/api/rooms/{id}` | Public | One room |
| GET | `/api/rooms/available?date=&startTime=&endTime=&capacity=` | Public | Rooms that fit the capacity and are free for the whole time slot |
| POST | `/api/rooms` | Admin | Create a room, returns 201 with the new id |
| PUT | `/api/rooms/{id}` | Admin | Update a room and replace its list of services |
| DELETE | `/api/rooms/{id}` | Admin | Delete a room (not allowed if it has bookings) |
| GET | `/api/services` | Public | Catalog of services |
| POST | `/api/services` | Admin | Add a service with a name and price to the catalog |
| POST | `/api/bookings` | Public | Book a room, returns the booking with the calculated total price |
| GET | `/api/bookings/{id}` | Public | One booking |
| GET | `/api/reports/rooms?from=&to=` | Admin | Revenue and utilization per room |
| GET | `/api/reports/services?from=&to=` | Admin | Popularity and revenue per service |
| GET | `/api/reports/tariff-zones?from=&to=` | Admin | Booked hours and their share per tariff zone |
| GET | `/health` | Public | `Healthy` / `Unhealthy`, including a database check |

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

**Adding a service to a room, as in the task example:** create the service with `POST /api/services` (`{ "name": "...", "price": ... }`), then send its id in `serviceIds` of `PUT /api/rooms/{id}`.

## Pricing

The price is calculated **hour by hour**, because one booking can span several tariff zones:

| Hours | Tariff | Room price multiplier |
|---|---|---|
| 06:00–09:00 | Morning | ×0.9 (−10%) |
| 09:00–12:00, 14:00–18:00 | Standard | ×1.0 |
| 12:00–14:00 | Peak | ×1.15 (+15%) |
| 18:00–23:00 | Evening | ×0.8 (−20%) |

Services are charged **once per booking**, not per hour. The total is rounded to 2 decimals (`MidpointRounding.AwayFromZero`).

In code the tariff zones are a data table in `PriceCalculator`. The first zone that contains the hour wins, so peak is listed before standard: it lies inside standard hours and must override them.

## Assumptions (gaps in the task)

| Question the task leaves open | Decision |
|---|---|
| Who uses the API? | Clients search and book; the company manages rooms and services and views reports. Company operations are protected with an API key |
| Are clients identified? | No, the task has no user accounts, so bookings are anonymous (see future improvements) |
| Peak 12:00–14:00 lies inside standard 09:00–18:00 | Peak replaces the standard tariff for these hours; multipliers are not combined |
| Hours 23:00–06:00 are not described | Bookings are not allowed; the API returns 400 with an explanation |
| Are services paid per hour or once? | Once per booking: "per hour" is stated only for the room |
| Can a booking start at 10:30? | No, bookings are made in whole hours only |
| Can a booking cross midnight? | No, a booking must start and end on the same day |
| Booking in the past | Not allowed |
| Are services global or per room? | One shared catalog of services (a new service with a name and price is added via `POST /api/services`); each room has a set of available services (many-to-many). A booking may only include services available in that room |
| Which services do the initial rooms have? | All three |
| Deleting a room that has bookings | Not allowed (409). Otherwise cascade delete would silently remove bookings and corrupt reports |
| Cancelling or changing a booking | Not in the task, not implemented (see future improvements) |

## Technical decisions

**Architecture.** Layers: controller → service → `DbContext`. Controllers only handle HTTP; business logic lives in services behind interfaces (`IRoomService`, `IServiceCatalogService`, `IBookingService`, `IReportService`) registered in DI. There is no separate repository layer: `DbContext` already implements the repository and unit-of-work patterns, and an extra wrapper around it would only duplicate it in a project of this size.

**Business rules in one place each.** `PriceCalculator` holds only the tariff logic and does not depend on the database, so it is easy to change and is covered by unit tests. `BookingTimeRules` holds the time rules (working hours, whole hours, same day, no past dates) shared by search and booking.

**Price is stored in the booking.** `Booking.TotalPrice` is calculated once at booking time. If the room price changes later, existing bookings keep the price the client agreed to.

**`decimal` for money.** `double` stores values in binary and cannot represent most decimal fractions exactly, so sums drift. In the database money columns are `decimal(18,2)`.

**DTOs instead of entities.** The API never exposes EF entities: this prevents clients from setting fields they should not (over-posting) and avoids serialization cycles (Room → Services → Rooms). The conversion of a service to its DTO lives in one extension method, `ToResponse()`.

**Availability check.** Two time intervals overlap when `start1 < end2 && start2 < end1`, so bookings 10:00–12:00 and 12:00–14:00 do not overlap. EF translates the search into a SQL `NOT EXISTS` subquery, so filtering happens in the database, supported by a composite index on `Bookings(RoomId, StartTime, EndTime)`.

**Protection against double booking.** Checking that the slot is free and inserting the booking run in one transaction with `Serializable` isolation. Without it, two parallel requests could both see the slot as free and both create a booking.

**Consistent errors.** A global middleware converts domain exceptions to HTTP statuses (`NotFoundException` → 404, `BadRequestException` → 400, `ConflictException` → 409) and returns them in the standard `ProblemDetails` format (RFC 7807). Validation errors (400), missing API key (401) and rate limiting (429) use the same format. Unexpected errors return 500 with a generic message; details go only to the log.

## Security

- **Admin API key** for managing rooms and services and for reports. Checked by an authorization filter before the request reaches the controller; keys are compared in constant time so they cannot be guessed by measuring response time. The real key is not stored in the repository.
- **Input validation** with data annotations on every request DTO; invalid requests get 400 with a list of errors.
- **Business rule validation** in `BookingTimeRules`.
- **Rate limiting:** 100 requests per minute per client IP; exceeding it returns 429 with an explanation.
- **No internal details in error responses.**
- **SQL injection** is prevented by EF Core, which always uses parameterized queries.
- **Report period is limited to 366 days**, so a single request cannot load the whole history.
- **HTTPS:** HTTP requests are redirected to HTTPS when HTTPS is configured. In production the API must be served over HTTPS only, because the API key travels in a request header.

## Fault tolerance

- **Automatic retries** of transient database failures (dropped connection, timeout, deadlock) with `EnableRetryOnFailure`. The booking transaction runs inside the EF execution strategy, so on a failure the whole operation (check + insert) is repeated, and the change tracker is cleared first so a retry cannot insert the booking twice.
- **Health check** at `/health` reports whether the API is alive and can reach the database, for monitoring tools and load balancers.
- **Global exception handling:** one failing request never exposes internals and never breaks the others.

## Scalability

**Easy to extend:** layers with interfaces and DI (a new feature is a new service, existing ones stay untouched); tariffs and time rules each live in one place; DTOs decouple the database from the API contract; EF migrations keep the schema history; unit tests make refactoring safe (the tariff logic was refactored into a table with all tests staying green).

**Ready for more load:** the API keeps no state in memory, so several instances can run behind a load balancer; `async` everywhere; filtering happens in SQL; the composite index serves the most frequent query; read-only queries use `AsNoTracking`; report periods are limited.

## Reports

| Report | Shows | Business value |
|---|---|---|
| `/api/reports/rooms` | Per room: number of bookings, booked hours, revenue, utilization % | Which rooms earn money and which stand idle — a basis for discounts or repurposing |
| `/api/reports/services` | Per service: times ordered, revenue | Which services are worth investing in and which can be dropped |
| `/api/reports/tariff-zones` | Per tariff zone (morning, standard, peak, evening): booked hours and their share | Whether the discounts actually attract clients and how busy the peak is — a basis for adjusting the multipliers |

Utilization = booked hours / available working hours (17 per day, 06:00–23:00) for the period. The tariff zone report uses the same zone table as the price calculation, so it can never disagree with the prices.

## Tests

18 unit tests (xUnit) cover the two pillars of the business logic. Neither depends on the database, so the tests need no setup.

- `PriceCalculator`: every tariff zone, a booking that crosses tariff zones, one-time service fees, rounding and invalid input.
- `BookingTimeRules`: a valid slot, the whole working day, end before start, not whole hours, before opening, crossing midnight and the past.

```bash
dotnet test
```

## Known limitations and future improvements

- **User accounts and roles.** A single admin key fits one company role. With several staff members and client accounts: JWT authentication with roles, and bookings linked to clients.
- **Booking cancellation** endpoint.
- **Service price snapshot.** Service prices are not stored per booking, so the services report uses current prices. Storing the price in the booking–service link would make historical reports exact.
- **Soft delete for rooms**, so rooms with a booking history can be archived instead of blocked from deletion.
- **Configurable tariffs.** Tariff zones are a table in code; moving them to configuration or the database would let the business change them without redeploying.
- **Distributed rate limiting.** Limits are counted in memory of each instance; with several instances a shared store such as Redis is needed.
- **Report aggregation in SQL** for large data volumes (the room report currently aggregates in memory).
- **Pagination** for list endpoints and **API versioning** (`/api/v1`) as the API grows.
- **Integration tests** for the endpoints and services with a test database (`WebApplicationFactory`), covering overlap checks, the API key and error handling.
- **Time zones.** All times are treated as local time of the venue; a multi-location setup would need explicit time zones.
- **Naming.** The entity `Service` (an amenity like a projector) can be confused with the service layer; `Amenity` would be a clearer name.
- **Migrations on startup** are convenient for a demo; in production they would be applied as a separate deployment step.

## Project structure

```
ConferenceRoomBooking.Api/
├── Controllers/     HTTP endpoints: Rooms, Services, Bookings, Reports
├── Services/        Business logic, PriceCalculator, BookingTimeRules
├── Dtos/            Request and response contracts, service mapping
├── Models/          EF Core entities: ConferenceRoom, Service, Booking
├── Data/            AppDbContext and DbSeeder
├── Security/        Admin API key filter and its Swagger description
├── Exceptions/      NotFound, BadRequest and Conflict exceptions
├── Middleware/      Global exception handling
├── Migrations/      EF Core migrations
└── ConferenceRoomBooking.Api.http   Request examples for all scenarios

ConferenceRoomBooking.Tests/
├── PriceCalculatorTests.cs    Unit tests for the pricing logic
└── BookingTimeRulesTests.cs   Unit tests for the booking time rules
```