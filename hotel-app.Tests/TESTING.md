# Testing Guide — hotel-app

This project ships with an automated grading-style test suite in `hotel-app.Tests`.
It mirrors the exam harness: each test is worth points, results are grouped by
category, and a fancy summary is printed at the end of every run.

> All commands below are meant to be run from a **Windows terminal** (PowerShell),
> from the repository root (`hotel-app\`). `dotnet` is expected to be on your PATH.

> **Privacy note:** the original exam harness uploaded a zip of your solution to a
> FINKI server. That behaviour has been **removed**. Results are only written to a
> local JSON file and printed to the console. Nothing leaves your machine.

---

## 1. One-time setup

### Set your index
Open `GlobalTestFixture.cs` and replace the placeholder:

```csharp
studentId: "YOUR_INDEX_HERE", // <-- put your index here
```

It shows up on the report banner and in `TestOutput\test_results.json`.

### Tools
- **.NET 10 SDK** (`net10.0`). Verify with `dotnet --version`.
- **Playwright browsers** (only needed for the UI tests). After the first build:
  ```powershell
  pwsh hotel-app.Tests\bin\Debug\net10.0\playwright.ps1 install chrome
  ```
  (or `playwright install chrome` if you have the global CLI). The UI tests use the
  installed Chrome via `Channel = "chrome"`.

---

## 2. The two kinds of tests

| Folder | Type | How it runs the app |
|---|---|---|
| `ControllersTests/` | Integration (HTTP) | In-process via `WebApplicationFactory<Program>` with an **in-memory** DB seeded by `TestDatabaseHelper`. No external server needed. |
| `PlaywrightTests/` | Browser UI | Drives a **real Chrome** against the app on `http://localhost:5210`. `AppFixture` boots the app automatically. |

The controller tests are self-contained — run them anytime. The Playwright tests
need the app to be reachable on port `5210` (the `AppFixture` starts it for you, or
you can run it yourself).

---

## 3. Running the tests

### Recommended: the clean report (`run-tests.ps1`)

```powershell
.\run-tests.ps1
# or filter, e.g.
.\run-tests.ps1 --filter "FullyQualifiedName~ControllersTests"
```

This runs the suite **quietly** and prints **only** the fancy summary — no restore,
build, or xUnit runner noise. Any extra args are passed straight to `dotnet test`.
It works by saving the rendered report to `TestOutput\test_summary.txt` during the
run and printing that file afterwards. (If the build fails and no report is produced,
the script prints the relevant error lines instead.)

> Running under WSL/bash instead? Use the equivalent `./run-tests.sh`.

### Manual `dotnet test`

If you run `dotnet test` directly, add `--logger "console;verbosity=detailed"` to see
the summary inline — but you'll also get all the runner/build output around it. The
test host's stdout is filtered by the runner's verbosity, so there's no way to show
*only* the summary this way; that's what `run-tests.ps1` is for.

**Everything**
```powershell
dotnet test hotel-app.Tests\hotel-app.Tests.csproj --logger "console;verbosity=detailed"
```

**Only the controller (HTTP) tests — fast, no browser**
```powershell
dotnet test hotel-app.Tests\hotel-app.Tests.csproj --filter "FullyQualifiedName~ControllersTests" --logger "console;verbosity=detailed"
```

**Only the Playwright (UI) tests**
```powershell
dotnet test hotel-app.Tests\hotel-app.Tests.csproj --filter "FullyQualifiedName~PlaywrightTests" --logger "console;verbosity=detailed"
```

**A single test**
```powershell
dotnet test hotel-app.Tests\hotel-app.Tests.csproj --filter "FullyQualifiedName~HotelControllerTests.Index_ReturnsAllHotels"
```

> **`--logger "console;verbosity=detailed"` is important.** The fancy summary is
> printed with `Console.WriteLine` from the test fixture, and xUnit only shows that
> output at detailed verbosity. Without it, tests still run but you won't see the
> summary box. (Or just use `.\run-tests.ps1`.)

---

## 4. Reading the output

At the end of a run you get a colorized report:

```
╔════════════════════════════════════════════════════════════════╗
║                  🏨  HOTEL APP — TEST RESULTS                  ║
╚════════════════════════════════════════════════════════════════╝
  Student  123456    Run  2026-06-02 09:29:58
  ✗ FAILED TESTS (12)
  ────────────────────────────────────────────────────────────────
  • [HotelController] HotelControllerTests.Index_ReturnsAllHotels (1 pts)
  ...

  CATEGORY BREAKDOWN
  ────────────────────────────────────────────────────────────────
  HotelController █░░░░░░░░░░░░░░░░░░░░░░░   5.9%  2/14 tests · 2/34 pts
╔════════════════════════════════════════════════════════════════╗
  ✔ 2 passed    ✗ 12 failed    of 14 total
  SCORE  ██░░░░░░░░░░░░░░░░░░░░░░░░░░
  ➜  2 / 34 points   (5.9%)
╚════════════════════════════════════════════════════════════════╝
```

- **FAILED TESTS** — each failure with its category and point value.
- **CATEGORY BREAKDOWN** — per-area progress bar (green ≥80%, yellow ≥50%, red below).
- **SCORE** — total points earned vs. available.

A machine-readable copy is saved to `TestOutput\test_results.json`.

---

## 5. How the suite is wired

- `Program.cs` ends with `public partial class Program { }` so the test host can boot it.
- `TestWebApplicationFactoryExtensions.WithTestDatabase` swaps the real Sqlite
  `ApplicationDbContext` for an in-memory one and seeds it. (It removes **all** EF
  registrations for the context — leaving any behind makes EF reject two providers.)
- `TestAuthHandler` provides a fake logged-in user via the `Test-User` header, so
  `[Authorize]` actions can be exercised. Use `CreateAuthenticatedClient("admin")`
  or `CreateAnonymousClient()`.
- `HttpResponseExtensions.GetAntiForgeryTokenAsync` scrapes the `__RequestVerificationToken`
  out of rendered HTML so POST tests pass anti-forgery validation.
- `[LoggedFact(Category, Points)]` replaces `[Fact]` and feeds the points/category
  into the report. `LoggedTestBase.RunTestAsync` wraps the body to log pass/fail.

---

## 6. Why are tests failing?

If you haven't implemented the controllers/views yet, failures are expected and the
error messages tell you what's missing:

| Error | Meaning |
|---|---|
| `404 (Not Found)` | The controller/action or route doesn't exist yet. |
| `Antiforgery token not found` | The Create/Edit **GET** view (with the form) isn't rendering. |
| `Sequence contains no elements` | A GET succeeded but returned no usable HTML/links the test looked for. |

Implement the `Hotel`, `Room`, and `Guest` controllers + views to turn these green.

---

## 7. Troubleshooting

- **Summary not showing** → use `.\run-tests.ps1`, or add `--logger "console;verbosity=detailed"`.
- **Box characters show as `?` / mojibake** → the scripts already force UTF-8. If you
  run `dotnet test` manually in an old console, run `chcp 65001` first.
- **`.\run-tests.ps1` is blocked ("running scripts is disabled")** → launch it with
  `powershell -ExecutionPolicy Bypass -File .\run-tests.ps1`, or allow scripts once
  with `Set-ExecutionPolicy -Scope Process Bypass`.
- **Playwright: "Executable doesn't exist" / browser not found** → run the
  `playwright install chrome` step in section 1.
- **Playwright tests can't reach the app** → confirm nothing else is on port `5210`,
  or start the app manually: `dotnet run --project hotel-app`.
- **A test DLL is "locked by another process"** → a previous test host is still
  running; close it (`taskkill /F /IM testhost.exe /T`) and re-run.
