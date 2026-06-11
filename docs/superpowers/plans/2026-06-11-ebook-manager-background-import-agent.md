# Ebook Manager Background Import Agent Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a background import agent that shows live progress, refreshes the library while large imports run, supports cancellation, and warns before closing the app during an active import.

**Architecture:** Extend the existing import pipeline with progress snapshots after each processed file. Add a presentation-layer `ImportAgent` that owns a single active job and exposes an observable `ImportJobViewModel` to `LibraryViewModel`. Keep persistence and per-file import behavior in `ImportService`; keep WPF-specific dialogs and closing behavior in `EbookManager.App`.

**Tech Stack:** .NET 10, C#, WPF, CommunityToolkit.Mvvm, xUnit, FluentAssertions, existing SQLite import repositories and WPF resource localization.

**Implementation status:** Completed inline on `main`.

- `d11a809` Add import progress reporting
- `6f92d15` Add import job view model
- `cf06b2b` Add background import agent
- `3b854e4` Refresh library during background imports
- `26a8bfa` Show background import progress

---

## File Structure

- Create `src/EbookManager.Domain/Importing/ImportProgress.cs`
  - Immutable progress snapshot and helper methods for counting outcomes.
- Modify `src/EbookManager.Application/Importing/ImportService.cs`
  - Add progress-aware overload and report one progress snapshot per processed item.
- Create `src/EbookManager.Presentation/ViewModels/ImportJobViewModel.cs`
  - Observable job state for the main UI.
- Create `src/EbookManager.Presentation/Importing/ImportAgent.cs`
  - Starts scan/import jobs in the background, owns cancellation, exposes progress events.
- Modify `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
  - Use `ImportAgent` for add, drag/drop, and scan flows; refresh periodically during import.
- Modify `src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs`
  - Add close confirmation and non-modal/result display shape where needed.
- Modify `src/EbookManager.App/Services/UserInteractionService.cs`
  - Implement active-import close confirmation.
- Modify `src/EbookManager.App/App.xaml.cs`
  - Register `ImportAgent`.
- Modify `src/EbookManager.App/MainWindow.xaml`
  - Add compact import progress card/status surface.
- Modify `src/EbookManager.App/MainWindow.xaml.cs`
  - Add closing warning and call the viewmodel/agent cancellation path.
- Modify `src/EbookManager.App/Resources/Strings/AppResources.resx`
- Modify `src/EbookManager.App/Resources/Strings/AppResources.nl.resx`
  - Add English and Dutch strings for background import.
- Modify `tests/EbookManager.Tests/Importing/ImportServiceTests.cs`
  - Add progress callback tests.
- Create `tests/EbookManager.Tests/App/ViewModels/ImportJobViewModelTests.cs`
  - Test progress state formatting/counts.
- Modify `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
  - Test background start, refresh threshold, completion refresh, cancel exposure.
- Create `docs/manual-tests/milestone-3-1-checklist.md`
  - Manual test checklist for large import behavior.
- Modify `README.md`
  - Document background import status and manual checklist.

---

### Task 1: Import Progress Snapshots In ImportService

**Files:**
- Create: `src/EbookManager.Domain/Importing/ImportProgress.cs`
- Modify: `src/EbookManager.Application/Importing/ImportService.cs`
- Test: `tests/EbookManager.Tests/Importing/ImportServiceTests.cs`

- [ ] **Step 1: Write failing progress callback test**

Add to `ImportServiceTests`:

```csharp
[Fact]
public async Task Import_async_reports_progress_after_each_processed_item()
{
    await using var fixture = await ImportServiceFixture.CreateAsync();
    var service = fixture.CreateService();
    var first = fixture.WriteBytesFile(@"incoming\First - Author.pdf", Encoding.UTF8.GetBytes("first"));
    var second = fixture.WriteBytesFile(@"incoming\Second - Author.pdf", Encoding.UTF8.GetBytes("second"));
    var progress = new List<ImportProgress>();

    var result = await service.ImportAsync([first, second], new Progress<ImportProgress>(progress.Add), default);

    result.Items.Should().HaveCount(2);
    progress.Should().HaveCount(2);
    progress[0].TotalCount.Should().Be(2);
    progress[0].ProcessedCount.Should().Be(1);
    progress[0].AddedCount.Should().Be(1);
    progress[0].LatestItem!.SourcePath.Should().Be(first);
    progress[1].ProcessedCount.Should().Be(2);
    progress[1].AddedCount.Should().Be(2);
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~Import_async_reports_progress_after_each_processed_item
```

Expected: compile failure because `ImportProgress` and the progress-aware overload do not exist.

- [ ] **Step 3: Create `ImportProgress`**

Create `src/EbookManager.Domain/Importing/ImportProgress.cs`:

```csharp
namespace EbookManager.Domain.Importing;

public sealed record ImportProgress(
    Guid RunId,
    int TotalCount,
    int ProcessedCount,
    int AddedCount,
    int ExactDuplicateCount,
    int PossibleDuplicateCount,
    int FailedCount,
    ImportItemResult? LatestItem)
{
    public int SkippedCount => ExactDuplicateCount + PossibleDuplicateCount;

    public static ImportProgress FromItems(
        Guid runId,
        int totalCount,
        IReadOnlyList<ImportItemResult> processedItems)
    {
        return new ImportProgress(
            runId,
            totalCount,
            processedItems.Count,
            processedItems.Count(item => item.Outcome == ImportOutcome.Added),
            processedItems.Count(item => item.Outcome == ImportOutcome.ExactDuplicate),
            processedItems.Count(item => item.Outcome == ImportOutcome.PossibleDuplicate),
            processedItems.Count(item => item.Outcome == ImportOutcome.Failed),
            processedItems.LastOrDefault());
    }
}
```

- [ ] **Step 4: Add progress-aware import overload**

In `ImportService`, keep the existing method and delegate to a new overload:

```csharp
public Task<ImportBatchResult> ImportAsync(
    IReadOnlyList<string> sourcePaths,
    CancellationToken cancellationToken = default) =>
    ImportAsync(sourcePaths, progress: null, cancellationToken);

public async Task<ImportBatchResult> ImportAsync(
    IReadOnlyList<string> sourcePaths,
    IProgress<ImportProgress>? progress,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(sourcePaths);

    var startedUtc = DateTimeOffset.UtcNow;
    var runId = await importRepository.StartRunAsync(startedUtc, cancellationToken);
    var results = new List<ImportItemResult>(sourcePaths.Count);

    try
    {
        for (var sequence = 0; sequence < sourcePaths.Count; sequence++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = await ImportSingleAsync(runId, sequence, sourcePaths[sequence], cancellationToken);
            results.Add(item);
            progress?.Report(ImportProgress.FromItems(runId, sourcePaths.Count, results));
        }
    }
    finally
    {
        try
        {
            await importRepository.CompleteRunAsync(runId, DateTimeOffset.UtcNow, CancellationToken.None);
        }
        catch when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    return new ImportBatchResult(runId, results);
}
```

- [ ] **Step 5: Run progress test**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~Import_async_reports_progress_after_each_processed_item
```

Expected: PASS.

- [ ] **Step 6: Run import tests and commit**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~EbookManager.Tests.Importing
git add src/EbookManager.Domain/Importing/ImportProgress.cs src/EbookManager.Application/Importing/ImportService.cs tests/EbookManager.Tests/Importing/ImportServiceTests.cs
git commit -m "Add import progress reporting"
```

Expected: importing tests pass.

---

### Task 2: Add Import Job ViewModel

**Files:**
- Create: `src/EbookManager.Presentation/ViewModels/ImportJobViewModel.cs`
- Test: `tests/EbookManager.Tests/App/ViewModels/ImportJobViewModelTests.cs`

- [ ] **Step 1: Write failing job viewmodel tests**

Create `ImportJobViewModelTests.cs`:

```csharp
using EbookManager.Domain.Importing;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class ImportJobViewModelTests
{
    [Fact]
    public void Start_and_progress_update_visible_status()
    {
        var viewModel = new ImportJobViewModel();
        var runId = Guid.NewGuid();

        viewModel.StartScanning();
        viewModel.StartImport(runId, totalCount: 100);
        viewModel.ApplyProgress(new ImportProgress(
            runId,
            100,
            25,
            20,
            3,
            1,
            1,
            new ImportItemResult("book.epub", ImportOutcome.Added, "added")));

        viewModel.IsVisible.Should().BeTrue();
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsIndeterminate.Should().BeFalse();
        viewModel.ProgressValue.Should().Be(25);
        viewModel.ProgressText.Should().Be("Processed 25 of 100");
        viewModel.AddedCount.Should().Be(20);
        viewModel.DuplicateCount.Should().Be(3);
        viewModel.PossibleDuplicateCount.Should().Be(1);
        viewModel.FailedCount.Should().Be(1);
    }

    [Fact]
    public void Complete_keeps_card_visible_and_enables_details()
    {
        var viewModel = new ImportJobViewModel();
        var result = new ImportBatchResult(Guid.NewGuid(), [new ImportItemResult("book.epub", ImportOutcome.Added, "added")]);

        viewModel.StartImport(result.RunId, totalCount: 1);
        viewModel.Complete(result);

        viewModel.IsVisible.Should().BeTrue();
        viewModel.IsActive.Should().BeFalse();
        viewModel.CanShowDetails.Should().BeTrue();
        viewModel.LatestResult.Should().BeSameAs(result);
    }
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~ImportJobViewModelTests
```

Expected: compile failure because `ImportJobViewModel` does not exist.

- [ ] **Step 3: Implement `ImportJobViewModel`**

Create `ImportJobViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Importing;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class ImportJobViewModel : ObservableObject
{
    [ObservableProperty] private bool isVisible;
    [ObservableProperty] private bool isActive;
    [ObservableProperty] private bool isIndeterminate;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int processedCount;
    [ObservableProperty] private int addedCount;
    [ObservableProperty] private int duplicateCount;
    [ObservableProperty] private int possibleDuplicateCount;
    [ObservableProperty] private int failedCount;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private ImportBatchResult? latestResult;

    public bool CanShowDetails => LatestResult is not null;
    public double ProgressValue => TotalCount <= 0 ? 0 : ProcessedCount * 100.0 / TotalCount;
    public string ProgressText => TotalCount <= 0 ? StatusText : $"Processed {ProcessedCount} of {TotalCount}";

    public void StartScanning()
    {
        IsVisible = true;
        IsActive = true;
        IsIndeterminate = true;
        Title = "Scanning folder...";
        StatusText = "Finding ebook files...";
        ClearCounts();
    }

    public void StartImport(Guid runId, int totalCount)
    {
        IsVisible = true;
        IsActive = true;
        IsIndeterminate = false;
        Title = "Importing books...";
        TotalCount = totalCount;
        ProcessedCount = 0;
        OnPropertyChanged(nameof(ProgressValue));
        OnPropertyChanged(nameof(ProgressText));
    }

    public void ApplyProgress(ImportProgress progress)
    {
        TotalCount = progress.TotalCount;
        ProcessedCount = progress.ProcessedCount;
        AddedCount = progress.AddedCount;
        DuplicateCount = progress.ExactDuplicateCount;
        PossibleDuplicateCount = progress.PossibleDuplicateCount;
        FailedCount = progress.FailedCount;
        StatusText = progress.LatestItem is null
            ? string.Empty
            : Path.GetFileName(progress.LatestItem.SourcePath);
        OnPropertyChanged(nameof(ProgressValue));
        OnPropertyChanged(nameof(ProgressText));
    }

    public void Complete(ImportBatchResult result)
    {
        LatestResult = result;
        IsActive = false;
        IsIndeterminate = false;
        Title = "Import complete";
        OnPropertyChanged(nameof(CanShowDetails));
    }

    public void Cancelled()
    {
        IsActive = false;
        IsIndeterminate = false;
        Title = "Import cancelled";
    }

    public void Failed(string message)
    {
        IsVisible = true;
        IsActive = false;
        IsIndeterminate = false;
        Title = "Import failed";
        StatusText = message;
    }

    private void ClearCounts()
    {
        TotalCount = 0;
        ProcessedCount = 0;
        AddedCount = 0;
        DuplicateCount = 0;
        PossibleDuplicateCount = 0;
        FailedCount = 0;
        LatestResult = null;
        OnPropertyChanged(nameof(CanShowDetails));
        OnPropertyChanged(nameof(ProgressValue));
        OnPropertyChanged(nameof(ProgressText));
    }
}
```

- [ ] **Step 4: Run job viewmodel tests and commit**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~ImportJobViewModelTests
git add src/EbookManager.Presentation/ViewModels/ImportJobViewModel.cs tests/EbookManager.Tests/App/ViewModels/ImportJobViewModelTests.cs
git commit -m "Add import job view model"
```

Expected: tests pass.

---

### Task 3: Add Background Import Agent

**Files:**
- Create: `src/EbookManager.Presentation/Importing/ImportAgent.cs`
- Modify: `src/EbookManager.Presentation/EbookManager.Presentation.csproj` only if needed by SDK item inclusion
- Test: `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] **Step 1: Write agent lifecycle tests**

Add a focused test fixture or nested fake services to `LibraryViewModelTests` only after Task 4 wires the viewmodel. For this task, test `ImportAgent` through a small direct test file if preferred:

Create `tests/EbookManager.Tests/App/ViewModels/ImportAgentTests.cs`:

```csharp
using EbookManager.Application.Importing;
using EbookManager.Domain.Importing;
using EbookManager.Presentation.Importing;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class ImportAgentTests
{
    [Fact]
    public async Task StartImportAsync_runs_job_and_exposes_completion()
    {
        var job = new ImportJobViewModel();
        var service = new FakeImportService();
        var agent = new ImportAgent(service, job);

        await agent.StartImportAsync(["a.epub", "b.epub"], _ => Task.CompletedTask, CancellationToken.None);
        await agent.ActiveTask!;

        job.IsActive.Should().BeFalse();
        job.CanShowDetails.Should().BeTrue();
        job.LatestResult!.Items.Should().HaveCount(2);
    }
}
```

If `ImportService` is not easy to fake because it is concrete, introduce an interface in Task 3:

```csharp
public interface IImportRunner
{
    Task<ImportBatchResult> ImportAsync(
        IReadOnlyList<string> sourcePaths,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default);
}
```

Then have `ImportService` implement `IImportRunner`, and make `ImportAgent` depend on `IImportRunner`.

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~ImportAgentTests
```

Expected: compile failure because `ImportAgent` and possibly `IImportRunner` do not exist.

- [ ] **Step 3: Implement import runner interface if needed**

Create `src/EbookManager.Domain/Abstractions/IImportRunner.cs`:

```csharp
using EbookManager.Domain.Importing;

namespace EbookManager.Domain.Abstractions;

public interface IImportRunner
{
    Task<ImportBatchResult> ImportAsync(
        IReadOnlyList<string> sourcePaths,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default);
}
```

Modify `ImportService` declaration:

```csharp
public sealed class ImportService(...) : IImportRunner
```

- [ ] **Step 4: Implement `ImportAgent`**

Create `ImportAgent.cs`:

```csharp
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Importing;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.Presentation.Importing;

public sealed class ImportAgent(IImportRunner importRunner, ImportJobViewModel job)
{
    private CancellationTokenSource? activeCancellation;

    public ImportJobViewModel Job { get; } = job;
    public Task? ActiveTask { get; private set; }
    public bool IsActive => ActiveTask is { IsCompleted: false };

    public Task StartImportAsync(
        IReadOnlyList<string> sourcePaths,
        Func<ImportProgress, Task> onProgress,
        CancellationToken cancellationToken)
    {
        if (IsActive)
        {
            throw new InvalidOperationException("An import job is already active.");
        }

        activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Job.StartImport(Guid.Empty, sourcePaths.Count);

        ActiveTask = RunImportAsync(sourcePaths, onProgress, activeCancellation.Token);
        return Task.CompletedTask;
    }

    public void StartScanning() => Job.StartScanning();

    public void CancelActiveJob() => activeCancellation?.Cancel();

    private async Task RunImportAsync(
        IReadOnlyList<string> sourcePaths,
        Func<ImportProgress, Task> onProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            var progress = new Progress<ImportProgress>(snapshot =>
            {
                Job.ApplyProgress(snapshot);
                _ = onProgress(snapshot);
            });
            var result = await importRunner.ImportAsync(sourcePaths, progress, cancellationToken);
            Job.Complete(result);
        }
        catch (OperationCanceledException)
        {
            Job.Cancelled();
        }
        catch
        {
            Job.Failed("The import job failed.");
        }
        finally
        {
            activeCancellation?.Dispose();
            activeCancellation = null;
        }
    }
}
```

If tests need deterministic progress awaiting, replace fire-and-forget `_ = onProgress(snapshot);` with a queue in Task 4. The first implementation may keep the refresh callback simple, because UI refresh is throttled in `LibraryViewModel`.

- [ ] **Step 5: Run tests and commit**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~ImportAgentTests
git add src/EbookManager.Domain/Abstractions/IImportRunner.cs src/EbookManager.Application/Importing/ImportService.cs src/EbookManager.Presentation/Importing/ImportAgent.cs tests/EbookManager.Tests/App/ViewModels/ImportAgentTests.cs
git commit -m "Add background import agent"
```

Expected: tests pass.

---

### Task 4: Wire LibraryViewModel To Background Agent And Live Refresh

**Files:**
- Modify: `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- Modify: `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] **Step 1: Write failing live-refresh test**

Add a test using a fake agent or real `ImportAgent` with fake runner:

```csharp
[Fact]
public async Task ImportFilesAsync_starts_background_import_and_refreshes_during_progress()
{
    var initial = CreateBook("Existing", ["Author"]);
    var imported = CreateBook("Imported", ["Author"]);
    var repository = new RefreshingBookRepository([initial], [initial, imported]);
    var agent = new ScriptedImportAgent();
    var viewModel = CreateViewModel([initial], repository: repository, importAgent: agent);

    await viewModel.RefreshAsync();
    await viewModel.ImportFilesAsync(["book.epub"]);
    await agent.ReportProgressAsync(25);

    viewModel.VisibleBooks.Select(book => book.Title).Should().Contain("Imported");
}
```

If creating a fake concrete `ImportAgent` is awkward, extract `IImportAgent` with `Job`, `IsActive`, `StartScanning`, `StartImportAsync`, and `CancelActiveJob`.

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~ImportFilesAsync_starts_background_import
```

Expected: compile failure because `LibraryViewModel` has no import agent integration.

- [ ] **Step 3: Add `IImportAgent` if needed and wire constructor**

Create `src/EbookManager.Presentation/Abstractions/IImportAgent.cs`:

```csharp
using EbookManager.Domain.Importing;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.Presentation.Abstractions;

public interface IImportAgent
{
    ImportJobViewModel Job { get; }
    bool IsActive { get; }
    void StartScanning();
    Task StartImportAsync(
        IReadOnlyList<string> sourcePaths,
        Func<ImportProgress, Task> onProgress,
        CancellationToken cancellationToken);
    void CancelActiveJob();
}
```

Have `ImportAgent` implement `IImportAgent`.

Add to `LibraryViewModel` constructor:

```csharp
IImportAgent? importAgent = null
```

Store it in a field and expose:

```csharp
public ImportJobViewModel? ImportJob => importAgent?.Job;
public bool HasActiveImport => importAgent?.IsActive == true;
public IRelayCommand CancelImportCommand => cancelImportCommand ??= new RelayCommand(() => importAgent?.CancelActiveJob());
public IAsyncRelayCommand ShowImportDetailsCommand => showImportDetailsCommand ??= new AsyncRelayCommand(ShowImportDetailsAsync);
```

- [ ] **Step 4: Change import and scan flows**

In `ImportFilesAsync`, replace direct import with:

```csharp
if (importAgent is null)
{
    var result = await importService.ImportAsync(paths, cancellationToken);
    LastImportResult = new ImportResultViewModel(result);
    await userInteraction.ShowImportResultAsync(LastImportResult, cancellationToken);
    await RefreshAsync(cancellationToken);
    return;
}

await importAgent.StartImportAsync(paths, OnImportProgressAsync, cancellationToken);
```

In `ScanFolderAsync`, after folder selection:

```csharp
importAgent?.StartScanning();
var includeSubdirectories = settingsStore is null ||
    (await settingsStore.LoadAsync(cancellationToken)).IncludeScanSubdirectories;
var files = await Task.Run(() => directoryScanner.Scan(folder, includeSubdirectories, cancellationToken), cancellationToken);
await ImportFilesAsync(files, cancellationToken);
```

Add refresh throttling:

```csharp
private const int ImportRefreshInterval = 25;
private int lastImportRefreshAt;

private async Task OnImportProgressAsync(ImportProgress progress)
{
    if (progress.ProcessedCount - lastImportRefreshAt < ImportRefreshInterval &&
        progress.ProcessedCount < progress.TotalCount)
    {
        return;
    }

    lastImportRefreshAt = progress.ProcessedCount;
    await RefreshAsync(CancellationToken.None);
}
```

In the agent completion callback or after `ActiveTask` completes, ensure final refresh and `LastImportResult` are updated from `ImportJob.LatestResult`. If the agent does not expose a completion event, add:

```csharp
public event EventHandler<ImportBatchResult>? Completed;
```

and subscribe in `LibraryViewModel`.

- [ ] **Step 5: Run LibraryViewModel tests and commit**

Run:

```powershell
dotnet test tests/EbookManager.Tests --no-restore --filter FullyQualifiedName~LibraryViewModelTests
git add src/EbookManager.Presentation/Abstractions/IImportAgent.cs src/EbookManager.Presentation/Importing/ImportAgent.cs src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs
git commit -m "Refresh library during background imports"
```

Expected: tests pass.

---

### Task 5: Add Main Window Progress UI And Close Warning

**Files:**
- Modify: `src/EbookManager.App/App.xaml.cs`
- Modify: `src/EbookManager.App/MainWindow.xaml`
- Modify: `src/EbookManager.App/MainWindow.xaml.cs`
- Modify: `src/EbookManager.App/Services/UserInteractionService.cs`
- Modify: `src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs`
- Modify: `src/EbookManager.App/Resources/Strings/AppResources.resx`
- Modify: `src/EbookManager.App/Resources/Strings/AppResources.nl.resx`

- [ ] **Step 1: Register services**

In `App.xaml.cs`, add:

```csharp
services.AddSingleton<ImportJobViewModel>();
services.AddSingleton<IImportAgent, ImportAgent>();
services.AddSingleton<IImportRunner>(provider => provider.GetRequiredService<ImportService>());
```

Keep `ImportService` registered so existing consumers still resolve it.

- [ ] **Step 2: Add progress card XAML**

In `MainWindow.xaml`, add a compact card above the center library view or inside the bottom status area. Bind to `ImportJob`:

```xml
<Border Background="{DynamicResource AppBackgroundBrush}"
        CornerRadius="12"
        Padding="10"
        Margin="0,0,0,10"
        Visibility="{Binding ImportJob.IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}">
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*" />
      <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <StackPanel>
      <TextBlock Text="{Binding ImportJob.Title}" FontWeight="SemiBold" />
      <TextBlock Text="{Binding ImportJob.ProgressText}" Foreground="{DynamicResource TextSecondaryBrush}" />
      <ProgressBar Minimum="0"
                   Maximum="100"
                   Value="{Binding ImportJob.ProgressValue}"
                   IsIndeterminate="{Binding ImportJob.IsIndeterminate}"
                   Height="6" />
      <TextBlock Foreground="{DynamicResource TextSecondaryBrush}">
        <Run Text="{loc:Loc ImportAdded}" /><Run Text=": " /><Run Text="{Binding ImportJob.AddedCount}" />
        <Run Text="  " />
        <Run Text="{loc:Loc ImportDuplicates}" /><Run Text=": " /><Run Text="{Binding ImportJob.DuplicateCount}" />
        <Run Text="  " />
        <Run Text="{loc:Loc ImportPossibleShort}" /><Run Text=": " /><Run Text="{Binding ImportJob.PossibleDuplicateCount}" />
        <Run Text="  " />
        <Run Text="{loc:Loc Failed}" /><Run Text=": " /><Run Text="{Binding ImportJob.FailedCount}" />
      </TextBlock>
    </StackPanel>
    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
      <Button Content="{loc:Loc Details}" Command="{Binding ShowImportDetailsCommand}" Margin="8,0,0,0" />
      <Button Content="{loc:Loc Cancel}" Command="{Binding CancelImportCommand}" Margin="8,0,0,0" />
    </StackPanel>
  </Grid>
</Border>
```

If no `BooleanToVisibilityConverter` exists, add it as a window resource:

```xml
<Window.Resources>
  <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
</Window.Resources>
```

- [ ] **Step 3: Add close confirmation**

In `MainWindow.xaml.cs`, subscribe to `Closing`:

```csharp
Closing += OnClosing;
```

Add:

```csharp
private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
{
    if (!viewModel.HasActiveImport)
    {
        return;
    }

    var result = System.Windows.MessageBox.Show(
        localizationService.GetString("ActiveImportCloseMessage"),
        localizationService.GetString("ActiveImportCloseTitle"),
        System.Windows.MessageBoxButton.YesNo,
        System.Windows.MessageBoxImage.Warning);

    if (result != System.Windows.MessageBoxResult.Yes)
    {
        e.Cancel = true;
        return;
    }

    viewModel.CancelImportCommand.Execute(null);
}
```

- [ ] **Step 4: Add resource strings**

Add to `AppResources.resx`:

```xml
<data name="ActiveImportCloseTitle" xml:space="preserve"><value>Import still running</value></data>
<data name="ActiveImportCloseMessage" xml:space="preserve"><value>An import job is still running. Do you want to cancel it and close the app?</value></data>
<data name="Details" xml:space="preserve"><value>Details</value></data>
```

Add to `AppResources.nl.resx`:

```xml
<data name="ActiveImportCloseTitle" xml:space="preserve"><value>Import is nog bezig</value></data>
<data name="ActiveImportCloseMessage" xml:space="preserve"><value>Er loopt nog een importtaak. Wilt u deze annuleren en de app sluiten?</value></data>
<data name="Details" xml:space="preserve"><value>Details</value></data>
```

- [ ] **Step 5: Build app and commit**

Run:

```powershell
dotnet build EbookManager.sln --no-restore
git add src/EbookManager.App/App.xaml.cs src/EbookManager.App/MainWindow.xaml src/EbookManager.App/MainWindow.xaml.cs src/EbookManager.App/Resources/Strings/AppResources.resx src/EbookManager.App/Resources/Strings/AppResources.nl.resx src/EbookManager.App/Services/UserInteractionService.cs src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs
git commit -m "Add background import progress UI"
```

Expected: build succeeds.

---

### Task 6: Documentation, Manual Checklist, And Full Verification

**Files:**
- Create: `docs/manual-tests/milestone-3-1-checklist.md`
- Modify: `README.md`

- [ ] **Step 1: Add manual checklist**

Create `docs/manual-tests/milestone-3-1-checklist.md`:

```markdown
# Milestone 3.1 Manual Test Checklist

Use this checklist for background import testing.

## Large Import

- Start the app with an existing library.
- Scan a folder with many ebook files.
- Confirm the import progress card appears.
- Confirm the progress card shows processed and total counts.
- Confirm added, duplicate, possible duplicate, and failed counts update.
- Confirm the library view refreshes while import is still running.
- Confirm search and filters remain usable during import.

## Completion And Details

- Let an import complete.
- Confirm final counts remain visible.
- Open the details/import result view.
- Confirm result counts match the progress card.

## Cancellation And Closing

- Start a large import and press Cancel.
- Confirm the app remains usable after cancellation.
- Start a large import and close the app.
- Confirm the close warning appears.
- Choose No and confirm the app stays open.
- Close again and choose Yes, then confirm the app closes.
```

- [ ] **Step 2: Update README**

Add Current Status bullets:

```markdown
- background import progress for large scans
- live library refresh during imports
- cancel and close-warning behavior for active imports
```

Add the new checklist link under Manual Verification.

- [ ] **Step 3: Run full tests and release build**

Run:

```powershell
dotnet test EbookManager.sln --no-restore
dotnet build EbookManager.sln -c Release --no-restore
```

Expected: tests pass and release build succeeds with zero errors.

- [ ] **Step 4: Commit docs**

Run:

```powershell
git add README.md docs/manual-tests/milestone-3-1-checklist.md
git commit -m "Document background import testing"
```

- [ ] **Step 5: Final status**

Run:

```powershell
git status --short
git log --oneline -8
Get-Item src\EbookManager.App\bin\Release\net10.0-windows\EbookManager.exe | Format-List FullName,LastWriteTime,Length
```

Expected: working tree clean except unrelated user/build-version changes.

---

## Self-Review

- Spec coverage: progress snapshots, background job lifecycle, UI progress card, live refresh, cancellation, close warning, localization, manual tests, and README updates are covered.
- Scope control: pause/resume, multiple simultaneous jobs, persistent job recovery, and separate service process are excluded.
- Type consistency: `ImportProgress`, `ImportJobViewModel`, `IImportAgent`, and optional `IImportRunner` provide the contract boundaries needed by the existing layers.
- Risk: `Progress<T>` posts to the captured synchronization context in WPF. Tests may need direct callbacks or a synchronous progress implementation to avoid timing flakes.
