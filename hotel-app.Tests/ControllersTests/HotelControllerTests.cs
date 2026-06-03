using System.Net;
using hotel_app.Models;
using HotelApp.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotelApp.Tests.ControllersTests;

[Collection("Test Suite")]
public class HotelControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HotelControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 1: CRUD ──────────────────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Index_ReturnsAllHotels()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Гранд Хотел", content);
            Assert.Contains("Хотел Метропол", content);
        });
    }

    // ── Req 1b: Hotel name is a link to Details ────────────────

    [LoggedFact(Category = "HotelController", Points = 5)]
    public async Task Index_HotelNameIsLinkToDetails()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);

            var response = await _client.GetAsync("/Hotel");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // The hotel name must appear inside an <a> tag pointing to Details
            Assert.Contains($"/Hotel/Details/{hotel.Id}", content);
            Assert.Contains(hotel.Name, content);

            // Name must be the link text, not just plain text
            var detailsLinkIndex = content.IndexOf($"/Hotel/Details/{hotel.Id}");
            var nameIndex = content.IndexOf(hotel.Name, detailsLinkIndex);
            Assert.True(nameIndex > detailsLinkIndex,
                $"Hotel name '{hotel.Name}' should appear as a link to Details, not plain text.");
        });
    }

    // ── Req 1c: Room count column ──────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 5)]
    public async Task Index_ShowsRoomCountPerHotel()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // hotel1 has 2 rooms, hotel2 has 1 room — assert the room-count column exists
            Assert.Contains("TotalRooms", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ── Req 4a: Filter by Name ─────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 5)]
    public async Task Index_FilterByName_ReturnsMatchingHotel()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel?name=Гранд+Хотел");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("Гранд Хотел", content);
            Assert.DoesNotContain("Хотел Метропол", content);
        });
    }

    // ── Req 4a: Filter by City ─────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 5)]
    public async Task Index_FilterByCity_ReturnsMatchingHotels()
    {
        await RunTestAsync(async () =>
        {
            // "Охрид" has only Хотел Метропол
            var response = await _client.GetAsync("/Hotel?city=Охрид");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("Хотел Метропол", content);
            // Гранд and Универзитетски are in Скопје — should not appear
            Assert.DoesNotContain("Универзитетски Хотел", content);
        });
    }

    // ── Req 4b: Filter values persist in form ─────────────────

    [LoggedFact(Category = "HotelController", Points = 5)]
    public async Task Index_FilterValues_PersistInFormAfterSearch()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel?name=Гранд+Хотел&city=Скопје");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // The form inputs should show the searched values
            Assert.Contains("Гранд Хотел", content);
            Assert.Contains("Скопје", content);
            // The values should be inside input value attributes
            Assert.Contains("value=\"Гранд Хотел\"", content);
        });
    }

    // ── CRUD: Create ───────────────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Create_ValidHotel_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);

            var getResponse = await _client.GetAsync("/Hotel/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", "Тест Хотел"),
                new KeyValuePair<string, string>("Address", "ул. Тест 1"),
                new KeyValuePair<string, string>("City", "Тетово"),
                new KeyValuePair<string, string>("Country", "Македонија"),
                new KeyValuePair<string, string>("Rating", "7.5"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Hotel/Create", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Hotel", response.Headers.Location?.ToString());

            var newCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);
            Assert.Equal(initialCount + 1, newCount);
        });
    }

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Create_InvalidHotel_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);

            var getResponse = await _client.GetAsync("/Hotel/Create");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", ""), // Required — blank fails validation
                new KeyValuePair<string, string>("Address", "ул. Тест 1"),
                new KeyValuePair<string, string>("City", "Тетово"),
                new KeyValuePair<string, string>("Country", "Македонија"),
                new KeyValuePair<string, string>("Rating", "7.5"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync("/Hotel/Create", formContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var newCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);
            Assert.Equal(initialCount, newCount);
        });
    }

    // ── CRUD: Details ──────────────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Details_ValidId_ReturnsHotel()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);

            var response = await _client.GetAsync($"/Hotel/Details/{hotel.Id}");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(hotel.Name, content);
        });
    }

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel/Details/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── CRUD: Edit ─────────────────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Edit_ValidHotel_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);
            var editedName = hotel.Name + " - Изменет";

            var getResponse = await _client.GetAsync($"/Hotel/Edit/{hotel.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", hotel.Id.ToString()),
                new KeyValuePair<string, string>("Name", editedName),
                new KeyValuePair<string, string>("Address", hotel.Address),
                new KeyValuePair<string, string>("City", hotel.City),
                new KeyValuePair<string, string>("Country", hotel.Country),
                new KeyValuePair<string, string>("Rating", hotel.Rating.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Hotel/Edit/{hotel.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Hotel", response.Headers.Location?.ToString());

            var edited = TestDatabaseHelper.GetById<Hotel>(_factory.Services, x => x.Id == hotel.Id);
            Assert.NotNull(edited);
            Assert.Equal(editedName, edited.Name);
        });
    }

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Edit_MismatchedId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);

            var getResponse = await _client.GetAsync($"/Hotel/Edit/{hotel.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", "99999"), // mismatched
                new KeyValuePair<string, string>("Name", hotel.Name),
                new KeyValuePair<string, string>("Address", hotel.Address),
                new KeyValuePair<string, string>("City", hotel.City),
                new KeyValuePair<string, string>("Country", hotel.Country),
                new KeyValuePair<string, string>("Rating", hotel.Rating.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Hotel/Edit/{hotel.Id}", formContent);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Delete_ValidHotel_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);
            var hotel = await TestDatabaseHelper.GetFirst<Hotel>(_factory.Services);

            var getResponse = await _client.GetAsync($"/Hotel/Delete/{hotel.Id}");
            var antiForgeryToken = await getResponse.GetAntiForgeryTokenAsync();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
            });

            var response = await _client.PostAsync($"/Hotel/Delete/{hotel.Id}", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Hotel", response.Headers.Location?.ToString());

            var deleted = TestDatabaseHelper.GetById<Hotel>(_factory.Services, x => x.Id == hotel.Id);
            Assert.Null(deleted);

            var newCount = await TestDatabaseHelper.GetCount<Hotel>(_factory.Services);
            Assert.Equal(initialCount - 1, newCount);
        });
    }

    [LoggedFact(Category = "HotelController", Points = 1)]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Hotel/Delete/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}