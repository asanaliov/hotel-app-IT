using System.Net;
using hotel_app.Data;
using hotel_app.Models;
using HotelApp.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelApp.Tests.ControllersTests;

[Collection("Test Suite")]
public class RoomControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RoomControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 1a: ImageUrl rendered as <img> ──────────────────

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Index_ShowsImageAsImg()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Room");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Room images must appear as <img> tags, not plain URL text
            Assert.Contains("<img", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("example.com/rooms", content);
        });
    }

    // ── Req 7e: Room/Details shows active reservations ───────

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Details_ShowsCurrentReservationsWhereCheckOutDateIsNull()
    {
        await RunTestAsync(async () =>
        {
            // room1 (seeded) has 2 active reservations (CheckOutDate == null)
            var room = await GetRoomWithActiveReservationsAsync();

            var response = await _client.GetAsync($"/Room/Details/{room.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Active guests' names should appear
            Assert.Contains("Александар", content);
            Assert.Contains("Марија", content);
        });
    }

    [LoggedFact(Category = "RoomController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Room/Details/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── Req 7a: Room/Reserve GET ───────────────────────────────

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Reserve_GET_ShowsRoomNumber()
    {
        await RunTestAsync(async () =>
        {
            var room = await TestDatabaseHelper.GetFirst<Room>(_factory.Services);

            var response = await _client.GetAsync($"/Room/Reserve/{room.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"room-title\"", content);
            Assert.Contains(room.RoomNumber, content);
        });
    }

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Reserve_GET_ShowsHotelName()
    {
        await RunTestAsync(async () =>
        {
            var room = await TestDatabaseHelper.GetFirst<Room>(_factory.Services);

            var response = await _client.GetAsync($"/Room/Reserve/{room.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"hotel-name\"", content);
        });
    }

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Reserve_GET_ShowsGuestDropdown()
    {
        await RunTestAsync(async () =>
        {
            var room = await TestDatabaseHelper.GetFirst<Room>(_factory.Services);

            var response = await _client.GetAsync($"/Room/Reserve/{room.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("<select", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ── Req 7b: Room/Reserve POST ──────────────────────────────

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task Reserve_POST_SavesReservationAndRedirectsToRoomDetails()
    {
        await RunTestAsync(async () =>
        {
            var room = await TestDatabaseHelper.GetFirst<Room>(_factory.Services);
            var guest = await TestDatabaseHelper.GetFirst<Guest>(_factory.Services);
            var initialCount = await TestDatabaseHelper.GetCount<Reservation>(_factory.Services);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("RoomId", room.Id.ToString()),
                new KeyValuePair<string, string>("GuestId", guest.Id.ToString()),
            });

            var response = await _client.PostAsync("/Room/Reserve", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Room/Details/{room.Id}", response.Headers.Location?.ToString());

            var newCount = await TestDatabaseHelper.GetCount<Reservation>(_factory.Services);
            Assert.Equal(initialCount + 1, newCount);

            // CheckOutDate must be null for the new reservation
            var reservation = TestDatabaseHelper.GetById<Reservation>(
                _factory.Services,
                r => r.RoomId == room.Id && r.GuestId == guest.Id && r.CheckOutDate == null);
            Assert.NotNull(reservation);
        });
    }

    // ── Req 6a: After AddRoom, redirect to Hotel/Details ──────

    [LoggedFact(Category = "RoomController", Points = 5)]
    public async Task AddRoom_AfterCreate_RedirectsToHotelDetails()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);
            var initialCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);

            // AddRoom lives in HotelController (Hotel/AddRoom/{hotelId}) per the task spec.
            var getResponse = await _client.GetAsync($"/Hotel/AddRoom/{hotel.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("RoomNumber", "999"),
                new KeyValuePair<string, string>("Type", "Single"),
                new KeyValuePair<string, string>("Description", "Тест соба"),
                new KeyValuePair<string, string>("ImageUrl", "https://example.com/rooms/test.jpg"),
                new KeyValuePair<string, string>("Capacity", "2"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Hotel/AddRoom/{hotel.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Hotel/Details/{hotel.Id}", response.Headers.Location?.ToString());

            var newCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);
            Assert.Equal(initialCount + 1, newCount);
        });
    }

    // ── CRUD: Create ───────────────────────────────────────────

    [LoggedFact(Category = "RoomController", Points = 1)]
    public async Task Create_ValidRoom_SavesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);
            var initialCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);

            var getResponse = await _client.GetAsync("/Room/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("RoomNumber", "305"),
                new KeyValuePair<string, string>("Type", "Double"),
                new KeyValuePair<string, string>("Description", "Втора нова соба"),
                new KeyValuePair<string, string>("ImageUrl", "https://example.com/rooms/305.jpg"),
                new KeyValuePair<string, string>("Capacity", "2"),
                new KeyValuePair<string, string>("HotelId", hotel.Id.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Room/Create", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);
            Assert.Equal(initialCount + 1, newCount);
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "RoomController", Points = 1)]
    public async Task Delete_ValidRoom_RemovesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);
            var room = await TestDatabaseHelper.GetFirst<Room>(_factory.Services);

            var getResponse = await _client.GetAsync($"/Room/Delete/{room.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Room/Delete/{room.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var deleted = TestDatabaseHelper.GetById<Room>(_factory.Services, x => x.Id == room.Id);
            Assert.Null(deleted);

            var newCount = await TestDatabaseHelper.GetCount<Room>(_factory.Services);
            Assert.Equal(initialCount - 1, newCount);
        });
    }

    // ── Helpers ────────────────────────────────────────────────

    private async Task<Room> GetRoomWithActiveReservationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // room1 is seeded with 2 active (CheckOutDate == null) reservations
        return await db.Rooms
            .Where(r => r.Reservations.Any(res => res.CheckOutDate == null))
            .FirstAsync();
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}