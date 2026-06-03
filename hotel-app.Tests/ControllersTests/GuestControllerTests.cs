using System.Net;
using System.Text.Json;
using hotel_app.Data;
using hotel_app.Models;
using HotelApp.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelApp.Tests.ControllersTests;

[Collection("Test Suite")]
public class GuestControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GuestControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 3: Validation ──────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 3)]
    public async Task Create_MissingFirstName_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync("/Guest/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", ""), // Required
                new KeyValuePair<string, string>("LastName", "Петровски"),
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("RegistrationDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Guest/Create", formContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            Assert.Equal(initialCount, newCount);
        });
    }

    [LoggedFact(Category = "GuestController", Points = 3)]
    public async Task Create_MissingLastName_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync("/Guest/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Александар"),
                new KeyValuePair<string, string>("LastName", ""), // Required
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("RegistrationDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Guest/Create", formContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            Assert.Equal(initialCount, newCount);
        });
    }

    [LoggedFact(Category = "GuestController", Points = 5)]
    public async Task Create_InvalidPhoneNumber_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync("/Guest/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Александар"),
                new KeyValuePair<string, string>("LastName", "Петровски"),
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "123"), // Not 9 digits
                new KeyValuePair<string, string>("RegistrationDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Guest/Create", formContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            Assert.Equal(initialCount, newCount);
        });
    }

    // ── CRUD: Create ───────────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 1)]
    public async Task Create_ValidGuest_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync("/Guest/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Тест"),
                new KeyValuePair<string, string>("LastName", "Гостин"),
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("RegistrationDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Guest/Create", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Guest", response.Headers.Location?.ToString());

            var newCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            Assert.Equal(initialCount + 1, newCount);
        });
    }

    // ── CRUD: Details ──────────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 1)]
    public async Task Details_ValidId_ReturnsGuest()
    {
        await RunTestAsync(async () =>
        {
            var guest = await TestDatabaseHelper.GetFirst<Guest>(_factory.Services);

            var response = await _client.GetAsync($"/Guest/Details/{guest.Id}");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(guest.FirstName, content);
        });
    }

    [LoggedFact(Category = "GuestController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Guest/Details/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── CRUD: Edit ─────────────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 1)]
    public async Task Edit_ValidGuest_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var guest = await TestDatabaseHelper.GetFirst<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync($"/Guest/Edit/{guest.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", guest.Id.ToString()),
                new KeyValuePair<string, string>("FirstName", guest.FirstName + " ИЗМ"),
                new KeyValuePair<string, string>("LastName", guest.LastName),
                new KeyValuePair<string, string>("Email", guest.Email),
                new KeyValuePair<string, string>("PhoneNumber", guest.PhoneNumber),
                new KeyValuePair<string, string>("RegistrationDate", guest.RegistrationDate.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Guest/Edit/{guest.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Guest", response.Headers.Location?.ToString());
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 1)]
    public async Task Delete_ValidGuest_RemovesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            var guest = await TestDatabaseHelper.GetFirst<Guest>(_factory.Services);

            var getResponse = await _client.GetAsync($"/Guest/Delete/{guest.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Guest/Delete/{guest.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Guest>(_factory.Services);
            Assert.Equal(initialCount - 1, newCount);
        });
    }

    // ── Req 7d: CheckOut ───────────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 5)]
    public async Task CheckOut_SetsCheckOutDateAndRedirectsToGuestDetails()
    {
        await RunTestAsync(async () =>
        {
            // Get an active reservation (CheckOutDate == null)
            var reservation = await GetActiveReservationAsync();
            Assert.NotNull(reservation);

            // Per the task spec the action lives at Guests/CheckOut/{reservationId} (plural).
            var response = await _client.PostAsync($"/Guests/CheckOut/{reservation.Id}", new StringContent(""));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Guest/Details/{reservation.GuestId}", response.Headers.Location?.ToString());

            // Verify CheckOutDate is now set
            var updated = TestDatabaseHelper.GetById<Reservation>(
                _factory.Services,
                r => r.Id == reservation.Id);
            Assert.NotNull(updated);
            Assert.NotNull(updated.CheckOutDate);
        });
    }

    // ── Req 5: Tabulator page ──────────────────────────────────

    [LoggedFact(Category = "GuestController", Points = 3)]
    public async Task Tabulator_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Guest/Tabulator");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("tabulator-table", content);
        });
    }

    // ── Req 5b: API endpoint returns JSON ─────────────────────

    [LoggedFact(Category = "GuestController", Points = 5)]
    public async Task Api_GetGuests_ReturnsJsonList()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/api/GuestApi");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var guests = JsonSerializer.Deserialize<JsonElement[]>(content);

            Assert.NotNull(guests);
            Assert.True(guests.Length > 0);

            // Check expected fields exist
            var first = guests[0];
            Assert.True(first.TryGetProperty("firstName", out _) || first.TryGetProperty("FirstName", out _),
                "JSON must contain firstName field");
            Assert.True(first.TryGetProperty("lastName", out _) || first.TryGetProperty("LastName", out _),
                "JSON must contain lastName field");
            Assert.True(first.TryGetProperty("email", out _) || first.TryGetProperty("Email", out _),
                "JSON must contain email field");
        });
    }

    // ── Helpers ────────────────────────────────────────────────

    private async Task<Reservation?> GetActiveReservationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Reservations
            .Where(r => r.CheckOutDate == null)
            .FirstOrDefaultAsync();
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}