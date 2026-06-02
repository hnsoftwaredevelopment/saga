# Ebook Manager WPF Shell Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generated MAUI shell with a native Windows WPF shell while preserving the completed portable library core and the supplied Ebook Manager branding assets.

**Architecture:** Keep `Domain`, `Application`, `Infrastructure`, and `Presentation` as testable class libraries. Convert only `EbookManager.App` to `net10.0-windows` WPF. Centralize the user-selected `26.6.0.0` assembly and file version in `Directory.Build.props`.

**Tech Stack:** .NET 10 WPF, C#, CommunityToolkit.Mvvm `8.4.2`, Syncfusion.SfGrid.WPF `33.2.7`

---

## Task 1: Replace the MAUI Shell with WPF

**Files:**
- Modify: `Directory.Build.props`
- Modify: `Directory.Packages.props`
- Replace: `src/EbookManager.App/EbookManager.App.csproj`
- Replace: `src/EbookManager.App/App.xaml`
- Replace: `src/EbookManager.App/App.xaml.cs`
- Create: `src/EbookManager.App/MainWindow.xaml`
- Create: `src/EbookManager.App/MainWindow.xaml.cs`
- Delete: `src/EbookManager.App/AppShell.xaml`
- Delete: `src/EbookManager.App/AppShell.xaml.cs`
- Delete: `src/EbookManager.App/MainPage.xaml`
- Delete: `src/EbookManager.App/MainPage.xaml.cs`
- Delete: `src/EbookManager.App/MauiProgram.cs`
- Delete: `src/EbookManager.App/Platforms/Android/*`
- Delete: `src/EbookManager.App/Platforms/iOS/*`
- Delete: `src/EbookManager.App/Platforms/MacCatalyst/*`
- Delete: `src/EbookManager.App/Platforms/Tizen/*`
- Delete: `src/EbookManager.App/Platforms/Windows/*`

- [ ] **Step 1: Preserve branding sources**

Keep these supplied SVG files tracked under the app project:

```text
Resources/AppIcon/appicon.svg
Resources/AppIcon/appiconfg.svg
Resources/Splash/splash.svg
```

They are product source assets. Do not replace them with generated template art.

- [ ] **Step 2: Centralize version metadata**

Add to `Directory.Build.props`:

```xml
<AssemblyVersion>26.6.0.0</AssemblyVersion>
<FileVersion>26.6.0.0</FileVersion>
```

Remove duplicate version entries from individual project files.

- [ ] **Step 3: Replace MAUI packages**

Remove `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug`, and `Syncfusion.Maui.DataGrid`. Add centrally:

```xml
<PackageVersion Include="Syncfusion.SfGrid.WPF" Version="33.2.7" />
```

Reference `Syncfusion.SfGrid.WPF` only from `EbookManager.App`.

- [ ] **Step 4: Convert the app project**

Use:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>EbookManager.App</RootNamespace>
    <AssemblyName>EbookManager</AssemblyName>
  </PropertyGroup>
</Project>
```

Retain project references to Domain, Application, Infrastructure, and Presentation.

- [ ] **Step 5: Create the minimal WPF shell**

`App.xaml` starts `MainWindow.xaml`. `MainWindow` shows a localized-ready placeholder title, `Ebook Manager`, and a short message that the native Windows shell is ready. The approved full workspace follows in Milestone Task 11.

- [ ] **Step 6: Verify and commit**

Run:

```powershell
dotnet restore EbookManager.sln
dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj
dotnet build EbookManager.sln
git diff --check
```

Expected: tests and build succeed without MAUI workloads or mobile targets.

Commit:

```powershell
git add Directory.Build.props Directory.Packages.props src/EbookManager.App src/EbookManager.Application src/EbookManager.Domain src/EbookManager.Infrastructure src/EbookManager.Presentation
git commit -m "Replace the MAUI shell with a Windows WPF app"
```
