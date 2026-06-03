using Microsoft.Playwright;

namespace HotelApp.Tests;

public class PlaywrightFixture : GlobalTestFixture, IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new() { Channel = "chrome", Headless = true });
    }

    public override async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
        await base.DisposeAsync();
    }
}