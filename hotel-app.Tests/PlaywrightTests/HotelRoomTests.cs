using HotelApp.Tests.Utils;
using Microsoft.Playwright;

namespace HotelApp.Tests.PlaywrightTests;

// Playwright tests require the app running on http://localhost:5210
// Start first: dotnet run --project hotel-app
[Collection("Playwright Suite")]
public class HotelRoomTests : LoggedTestBase
{
    private const string BaseUrl = "http://localhost:5210";
    private readonly PlaywrightFixture _playwright;

    public HotelRoomTests(PlaywrightFixture playwrightFixture, AppFixture _) : base(playwrightFixture)
    {
        _playwright = playwrightFixture;
    }

    private async Task<IPage> NewPageAsync() => await _playwright.Browser.NewPageAsync();

    // ── Req 6a: Hotel/Details has id="add-room" link ──────────

    [LoggedFact(Category = "PlaywrightUI", Points = 5)]
    public async Task HotelDetails_HasAddRoomLinkWithCorrectId()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Hotel");
                var firstDetailsLink = page.Locator("a[href*='/Hotel/Details/']").First;
                var href = await firstDetailsLink.GetAttributeAsync("href");
                Assert.NotNull(href);

                await page.GotoAsync($"{BaseUrl}{href}");

                var addRoomLink = page.Locator("#add-room");
                await Assertions.Expect(addRoomLink).ToBeVisibleAsync();

                var linkText = await addRoomLink.InnerTextAsync();
                Assert.Contains("Додади соба", linkText);
            }
            finally { await page.CloseAsync(); }
        });
    }

    // ── Req 6b: Hotel/Details rooms table has id="rooms-table" ─

    [LoggedFact(Category = "PlaywrightUI", Points = 5)]
    public async Task HotelDetails_RoomsTableHasCorrectId()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Hotel");
                var href = await page.Locator("a[href*='/Hotel/Details/']").First.GetAttributeAsync("href");
                await page.GotoAsync($"{BaseUrl}{href}");

                await Assertions.Expect(page.Locator("#rooms-table")).ToBeVisibleAsync();
            }
            finally { await page.CloseAsync(); }
        });
    }

    // ── Req 6b: Details links have class="details-btn" ────────

    [LoggedFact(Category = "PlaywrightUI", Points = 2)]
    public async Task HotelDetails_DetailsBtnHasCorrectClass()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Hotel");
                var href = await page.Locator("a[href*='/Hotel/Details/']").First.GetAttributeAsync("href");
                await page.GotoAsync($"{BaseUrl}{href}");

                var count = await page.Locator(".details-btn").CountAsync();
                Assert.True(count > 0, "Expected at least one element with class 'details-btn'.");
            }
            finally { await page.CloseAsync(); }
        });
    }

    // ── Req 6b: Reserve links have class="reserve-btn" ────────

    [LoggedFact(Category = "PlaywrightUI", Points = 2)]
    public async Task HotelDetails_ReserveBtnHasCorrectClass()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Hotel");
                var href = await page.Locator("a[href*='/Hotel/Details/']").First.GetAttributeAsync("href");
                await page.GotoAsync($"{BaseUrl}{href}");

                var count = await page.Locator(".reserve-btn").CountAsync();
                Assert.True(count > 0, "Expected at least one element with class 'reserve-btn'.");
            }
            finally { await page.CloseAsync(); }
        });
    }

    // ── Req 7c: Guest/Details checkout button class ───────────

    [LoggedFact(Category = "PlaywrightUI", Points = 5)]
    public async Task GuestDetails_CheckoutBtnHasCorrectClass()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Guest");
                var href = await page.Locator("a[href*='/Guest/Details/']").First.GetAttributeAsync("href");
                await page.GotoAsync($"{BaseUrl}{href}");

                var count = await page.Locator(".checkout-btn").CountAsync();
                Assert.True(count > 0, "Expected at least one element with class 'checkout-btn'.");
            }
            finally { await page.CloseAsync(); }
        });
    }

    // ── Req 5c: Guest/Tabulator loads data ────────────────────

    [LoggedFact(Category = "PlaywrightUI", Points = 3)]
    public async Task GuestTabulator_LoadsDataInTable()
    {
        await RunTestAsync(async () =>
        {
            var page = await NewPageAsync();
            try
            {
                await page.GotoAsync($"{BaseUrl}/Guest/Tabulator");

                await Assertions.Expect(page.Locator("#tabulator-table")).ToBeVisibleAsync();
                await page.WaitForSelectorAsync(".tabulator-row", new() { Timeout = 5000 });

                var rowCount = await page.Locator(".tabulator-row").CountAsync();
                Assert.True(rowCount > 0, "Tabulator table should have at least one row.");
            }
            finally { await page.CloseAsync(); }
        });
    }
}

[CollectionDefinition("Playwright Suite", DisableParallelization = true)]
public class PlaywrightSuiteCollection : ICollectionFixture<PlaywrightFixture>, ICollectionFixture<AppFixture> { }