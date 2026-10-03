# LBM Diffusion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zastąpić stary prototyp cząsteczek symulacją dyfuzji D2Q9 Lattice Boltzmann z UI WinForms (.NET 10), testami i CI.

**Architecture:** `LatticeBoltzmann.Core` (net10.0) zawiera całą fizykę i logikę testowalną bez okna (paleta, mapowanie ekranu, pędzel, renderowanie do bufora `int[]`). `LatticeBoltzmann.App` (net10.0-windows) to cienka warstwa WinForms. Testy rdzenia działają na Linuksie i Windows, testy UI tylko na Windows (CI).

**Tech Stack:** .NET 10 SDK, C# (latest), WinForms, xUnit 2.9.3, GitHub Actions (windows-latest).

**Spec:** `docs/superpowers/specs/2026-10-03-lbm-diffusion-design.md`

## Global Constraints

- SDK: `global.json` → `10.0.100`, `rollForward: latestFeature`.
- TFM: rdzeń i testy rdzenia `net10.0`; aplikacja i jej testy `net10.0-windows` z `EnableWindowsTargeting=true` w csproj (żeby budowały się na Linuksie).
- `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`, `LangVersion=latest` — w `Directory.Build.props`.
- Pakiety (centralnie, `Directory.Packages.props`): `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1.
- Identyfikatory po angielsku; napisy UI i komunikaty po polsku; liczby w UI formatowane `CultureInfo.GetCultureInfo("pl-PL")`.
- `LatticeBoltzmann.Core` nie referencuje WinForms ani System.Drawing.
- Zakazane: `goto`, `GC.Collect`, `CreateGraphics`, odczyt pikseli z ekranu.
- Wartości domyślne UI: siatka 200×120, otwór 20, D = 0,20, 10 kroków na klatkę, pędzel 2, start w pauzie.
- Każdy commit kończy się liniami:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01LUuzv9HvhYeC3waSt45KXE
  ```
- Na Linuksie testy `LatticeBoltzmann.App.Tests` tylko się kompilują (`dotnet build`); uruchamia je CI na Windows (Task 9).

## Review Focus

1. Zminimalizowane okno / widok o rozmiarze 0 — odświeżenie i malowanie nie rzucają wyjątku (test w Task 7).
2. Zmiana rozdzielczości przy suwaku otworu na maksimum — wartość mieści się w nowym zakresie, `TrackBar` nie rzuca (test w Task 8).
3. Pociągnięcie pędzlem wychodzące poza obraz i wracające w innym miejscu — nie rysuje linii przez obszar poza obrazem (test w Task 7).
4. Cała siatka zamalowana ścianą — `Advance` i renderowanie działają, masa 0, brak NaN (test w Task 3).
5. Wymiana symulacji (reset/rozdzielczość) w trakcie przeciągania myszą — brak wyjścia poza zakres starej siatki (test w Task 7).

---

### Task 1: Szkielet rozwiązania i sieć D2Q9

**Files:**
- Delete: `Lattice boltzmann/` (cały katalog), `Lattice boltzmann.sln`
- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `LatticeBoltzmann.slnx`
- Create: `src/LatticeBoltzmann.Core/LatticeBoltzmann.Core.csproj`, `src/LatticeBoltzmann.Core/D2Q9.cs`
- Create: `tests/LatticeBoltzmann.Core.Tests/LatticeBoltzmann.Core.Tests.csproj`, `tests/LatticeBoltzmann.Core.Tests/D2Q9Tests.cs`

**Interfaces:**
- Produces: `public static class D2Q9` w namespace `LatticeBoltzmann.Core`:
  `const int Q = 9`, `const double SoundSpeedSquared = 1.0 / 3.0`,
  `static ReadOnlySpan<int> Ex`, `static ReadOnlySpan<int> Ey`,
  `static ReadOnlySpan<double> Weights`, `static ReadOnlySpan<int> Opposite`
  (kolejność z tabeli w spec 3.1).

- [ ] **Step 1: Usuń stary kod i dodaj pliki konfiguracyjne**

`git rm -r "Lattice boltzmann" "Lattice boltzmann.sln"`. Utwórz `global.json`, `Directory.Build.props`, `Directory.Packages.props` z wartościami z Global Constraints. `.editorconfig`: `root = true`, wcięcia 4 spacje dla `*.cs`, `charset = utf-8`, `end_of_line = crlf` dla `*.cs`, a dla `tests/**/*.cs` `dotnet_diagnostic.CA1707.severity = none` (podkreślenia w nazwach testów) oraz `dotnet_diagnostic.CA1515.severity = none` (xUnit wymaga publicznych klas testów). Innych wyłączeń analizatorów nie dodajemy — ostrzeżenia w kodzie produkcyjnym się poprawia. Utwórz rozwiązanie `dotnet new sln -n LatticeBoltzmann` (SDK 10 tworzy `.slnx`), projekty `dotnet new classlib` / `dotnet new xunit` w podanych ścieżkach, usuń z szablonów `Class1.cs`/`UnitTest1.cs` i wersje pakietów (są centralne), dodaj oba projekty do `.slnx`, test → `ProjectReference` do Core, `<Using Include="Xunit" />`.

- [ ] **Step 2: Napisz test, który nie przejdzie**

```csharp
namespace LatticeBoltzmann.Core.Tests;

public class D2Q9Tests
{
    [Fact]
    public void Directions_follow_spec_order()
    {
        Assert.Equal(new[] { 0, 1, 0, -1, 0, 1, -1, -1, 1 }, D2Q9.Ex.ToArray());
        Assert.Equal(new[] { 0, 0, 1, 0, -1, 1, 1, -1, -1 }, D2Q9.Ey.ToArray());
        Assert.Equal(new[] { 0, 3, 4, 1, 2, 7, 8, 5, 6 }, D2Q9.Opposite.ToArray());
        Assert.Equal(9, D2Q9.Q);
    }

    [Fact]
    public void Weights_sum_to_one()
    {
        double sum = 0;
        foreach (var w in D2Q9.Weights) sum += w;
        Assert.Equal(1.0, sum, 15);
    }

    [Fact]
    public void Weighted_moments_match_isotropic_lattice()
    {
        double mx = 0, my = 0, mxx = 0, myy = 0, mxy = 0;
        for (var i = 0; i < D2Q9.Q; i++)
        {
            var w = D2Q9.Weights[i];
            mx += w * D2Q9.Ex[i]; my += w * D2Q9.Ey[i];
            mxx += w * D2Q9.Ex[i] * D2Q9.Ex[i];
            myy += w * D2Q9.Ey[i] * D2Q9.Ey[i];
            mxy += w * D2Q9.Ex[i] * D2Q9.Ey[i];
        }
        Assert.Equal(0, mx, 15); Assert.Equal(0, my, 15); Assert.Equal(0, mxy, 15);
        Assert.Equal(D2Q9.SoundSpeedSquared, mxx, 15);
        Assert.Equal(D2Q9.SoundSpeedSquared, myy, 15);
    }

    [Fact]
    public void Opposite_reverses_each_direction()
    {
        for (var i = 0; i < D2Q9.Q; i++)
        {
            var o = D2Q9.Opposite[i];
            Assert.Equal(i, D2Q9.Opposite[o]);
            Assert.Equal(-D2Q9.Ex[i], D2Q9.Ex[o]);
            Assert.Equal(-D2Q9.Ey[i], D2Q9.Ey[o]);
        }
    }
}
```

- [ ] **Step 3: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests`
Expected: błąd kompilacji `CS0103: The name 'D2Q9' does not exist`.

- [ ] **Step 4: Zaimplementuj `D2Q9` w `src/LatticeBoltzmann.Core/D2Q9.cs`**

Właściwości `ReadOnlySpan<T>` zwracają wyrażenia kolekcji ze stałymi (kompilator umieszcza je w danych statycznych). Wagi: `4.0/9`, cztery razy `1.0/9`, cztery razy `1.0/36`.

- [ ] **Step 5: Uruchom testy i build**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → 4 testy PASS.
Run: `dotnet build LatticeBoltzmann.slnx -c Release` → `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Replace legacy prototype with .NET 10 solution and D2Q9 lattice"
```

---

### Task 2: Stan symulacji — komórki, ściany, stężenie

**Files:**
- Create: `src/LatticeBoltzmann.Core/DiffusionSimulation.cs`
- Test: `tests/LatticeBoltzmann.Core.Tests/DiffusionSimulationStateTests.cs`

**Interfaces:**
- Consumes: `D2Q9` (Task 1).
- Produces: `public sealed class DiffusionSimulation` z API ze spec 3.2:
  `const double MinDiffusionCoefficient = 0.05`, `const double MaxDiffusionCoefficient = 0.5`,
  `DiffusionSimulation(int width, int height, double diffusionCoefficient)`,
  `int Width`, `int Height`, `long StepCount`, `double DiffusionCoefficient { get; set; }`,
  `double RelaxationTime`, `double TotalMass`, `bool IsWall(int x, int y)`,
  `void SetWall(int x, int y, bool isWall)`, `double GetConcentration(int x, int y)`,
  `void SetConcentration(int x, int y, double value)`,
  `void CopyConcentration(Span<double> destination)`.
  (`Advance` dochodzi w Task 3.)

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
namespace LatticeBoltzmann.Core.Tests;

public class DiffusionSimulationStateTests
{
    [Fact]
    public void New_simulation_is_all_fluid_and_empty()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        Assert.Equal(5, sim.Width); Assert.Equal(4, sim.Height); Assert.Equal(0, sim.StepCount);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 5; x++)
            {
                Assert.False(sim.IsWall(x, y));
                Assert.Equal(0.0, sim.GetConcentration(x, y));
            }
        Assert.Equal(0.0, sim.TotalMass);
    }

    [Fact]
    public void RelaxationTime_follows_diffusion_coefficient()
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        Assert.Equal(1.1, sim.RelaxationTime, 12);
        sim.DiffusionCoefficient = 0.5;
        Assert.Equal(2.0, sim.RelaxationTime, 12);
    }

    [Theory]
    [InlineData(2, 5, 0.2)]
    [InlineData(5, 2, 0.2)]
    [InlineData(5, 5, 0.049)]
    [InlineData(5, 5, 0.51)]
    [InlineData(5, 5, double.NaN)]
    public void Constructor_rejects_invalid_arguments(int w, int h, double d) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(w, h, d));

    [Theory]
    [InlineData(0.04)]
    [InlineData(0.6)]
    [InlineData(double.NaN)]
    public void DiffusionCoefficient_setter_rejects_out_of_range(double d)
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.DiffusionCoefficient = d);
        Assert.Equal(0.2, sim.DiffusionCoefficient);
    }

    [Fact]
    public void SetConcentration_stores_value_and_adds_to_mass()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(2, 1, 0.7);
        Assert.Equal(0.7, sim.GetConcentration(2, 1), 15);
        Assert.Equal(0.7, sim.TotalMass, 15);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SetConcentration_rejects_negative_or_non_finite(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(5, 4, 0.2).SetConcentration(1, 1, value));

    [Fact]
    public void SetConcentration_on_wall_throws()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetWall(1, 1, true);
        Assert.Throws<InvalidOperationException>(() => sim.SetConcentration(1, 1, 0.5));
    }

    [Fact]
    public void Coordinates_outside_grid_throw()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.IsWall(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.SetWall(5, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.SetConcentration(0, 4, 1));
    }

    [Fact]
    public void Painting_wall_on_fluid_removes_its_content()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(1, 1, 0.5);
        sim.SetConcentration(2, 2, 0.3);
        sim.SetWall(1, 1, true);
        Assert.True(sim.IsWall(1, 1));
        Assert.Equal(0.0, sim.GetConcentration(1, 1));
        Assert.Equal(0.3, sim.TotalMass, 15);
    }

    [Fact]
    public void Erased_wall_becomes_empty_fluid()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetWall(1, 1, true);
        sim.SetWall(1, 1, false);
        Assert.False(sim.IsWall(1, 1));
        Assert.Equal(0.0, sim.GetConcentration(1, 1));
        sim.SetConcentration(1, 1, 0.4);
        Assert.Equal(0.4, sim.GetConcentration(1, 1), 15);
    }

    [Fact]
    public void Erasing_fluid_cell_keeps_its_concentration()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(1, 1, 0.5);
        sim.SetWall(1, 1, false);
        Assert.Equal(0.5, sim.GetConcentration(1, 1), 15);
    }

    [Fact]
    public void CopyConcentration_is_row_major_with_zero_walls()
    {
        var sim = new DiffusionSimulation(3, 3, 0.2);
        sim.SetConcentration(2, 0, 0.4);
        sim.SetConcentration(0, 1, 0.9);
        sim.SetWall(1, 1, true);
        var buffer = new double[9];
        sim.CopyConcentration(buffer);
        Assert.Equal(new[] { 0, 0, 0.4, 0.9, 0, 0, 0, 0, 0 }, buffer.Select(v => Math.Round(v, 12)).ToArray());
    }

    [Fact]
    public void CopyConcentration_rejects_wrong_length() =>
        Assert.Throws<ArgumentException>(() => new DiffusionSimulation(3, 3, 0.2).CopyConcentration(new double[8]));
}
```

- [ ] **Step 2: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests --filter FullyQualifiedName~DiffusionSimulationStateTests`
Expected: błąd kompilacji — brak typu `DiffusionSimulation`.

- [ ] **Step 3: Zaimplementuj stan w `src/LatticeBoltzmann.Core/DiffusionSimulation.cs`**

Pola: `double[] _f` i `double[] _next` o długości `Width * Height * D2Q9.Q` (indeks `(y * Width + x) * Q + i`), `bool[] _wall`. `SetConcentration` zapisuje `fᵢ = wᵢ·value`. `SetWall` zeruje 9 populacji tylko przy zmianie stanu. `RelaxationTime = 3·D + 0.5`. Walidacja przez `ArgumentOutOfRangeException` z nazwą parametru, także dla NaN i nieskończoności (`double.IsFinite` sprawdzane przed zakresem — porównania z NaN są zawsze fałszywe); zła długość bufora w `CopyConcentration` → `ArgumentException`.

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → wszystkie PASS; `dotnet build LatticeBoltzmann.slnx -c Release` → 0 ostrzeżeń.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add DiffusionSimulation state: cells, walls and concentration"
```

---

### Task 3: Krok symulacji — kolizja BGK i streaming z odbiciem

**Files:**
- Modify: `src/LatticeBoltzmann.Core/DiffusionSimulation.cs`
- Test: `tests/LatticeBoltzmann.Core.Tests/DiffusionSimulationPhysicsTests.cs`

**Interfaces:**
- Consumes: `DiffusionSimulation` (Task 2), `D2Q9` (Task 1).
- Produces: `public void Advance(int steps = 1)` — `steps ≥ 1`, `StepCount += steps`.

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
namespace LatticeBoltzmann.Core.Tests;

public class DiffusionSimulationPhysicsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Advance_rejects_non_positive_steps(int steps) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(5, 5, 0.2).Advance(steps));

    [Fact]
    public void Advance_counts_steps()
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        sim.Advance(3);
        sim.Advance();
        Assert.Equal(4, sim.StepCount);
    }

    [Fact]
    public void Uniform_equilibrium_stays_uniform()
    {
        var sim = new DiffusionSimulation(20, 10, 0.2);
        Fill(sim, (_, _) => 0.7);
        sim.Advance(100);
        ForEachFluid(sim, (x, y) => Assert.Equal(0.7, sim.GetConcentration(x, y), 12));
    }

    [Fact]
    public void Mass_is_conserved_with_walls()
    {
        var sim = new DiffusionSimulation(60, 40, 0.3);
        for (var y = 0; y < 40; y++) if (y < 15 || y > 24) sim.SetWall(20, y, true);
        for (var x = 40; x <= 44; x++) for (var y = 5; y <= 9; y++) sim.SetWall(x, y, true);
        Fill(sim, (x, _) => x < 20 ? 1.0 : 0.0);
        var m0 = sim.TotalMass;
        sim.Advance(2000);
        Assert.True(Math.Abs(sim.TotalMass - m0) / m0 < 1e-12);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0 / 6.0)]
    [InlineData(0.3)]
    [InlineData(0.5)]
    public void Variance_grows_at_rate_two_D(double d)
    {
        var sim = new DiffusionSimulation(121, 121, d);
        sim.SetConcentration(60, 60, 1.0);
        sim.Advance(50);
        var v1 = VarianceX(sim);
        sim.Advance(100);
        var v2 = VarianceX(sim);
        var slope = (v2 - v1) / (2.0 * 100);
        Assert.InRange(slope, d * 0.99, d * 1.01);
    }

    [Theory]
    [InlineData(1.0 / 6.0)]
    [InlineData(0.3)]
    public void Concentration_stays_in_unit_range_when_tau_at_least_one(double d)
    {
        var sim = new DiffusionSimulation(60, 40, d);
        Fill(sim, (x, _) => x < 20 ? 1.0 : 0.0);
        sim.Advance(1000);
        ForEachFluid(sim, (x, y) => Assert.InRange(sim.GetConcentration(x, y), 0.0, 1.0 + 1e-12));
    }

    [Fact]
    public void Full_height_wall_is_impermeable()
    {
        var sim = new DiffusionSimulation(30, 10, 0.3);
        for (var y = 0; y < 10; y++) sim.SetWall(10, y, true);
        Fill(sim, (x, _) => x < 10 ? 1.0 : 0.0);
        sim.Advance(500);
        for (var x = 11; x < 30; x++) for (var y = 0; y < 10; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }

    [Fact]
    public void All_wall_grid_stays_empty()
    {
        var sim = new DiffusionSimulation(4, 4, 0.2);
        for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) sim.SetWall(x, y, true);
        sim.Advance(10);
        Assert.Equal(0.0, sim.TotalMass);
    }

    internal static void Fill(DiffusionSimulation sim, Func<int, int, double> c) =>
        ForEachFluid(sim, (x, y) => sim.SetConcentration(x, y, c(x, y)));

    internal static void ForEachFluid(DiffusionSimulation sim, Action<int, int> action)
    {
        for (var y = 0; y < sim.Height; y++)
            for (var x = 0; x < sim.Width; x++)
                if (!sim.IsWall(x, y)) action(x, y);
    }

    private static double VarianceX(DiffusionSimulation sim)
    {
        double m = 0, mx = 0, mxx = 0;
        ForEachFluid(sim, (x, y) => { var c = sim.GetConcentration(x, y); m += c; mx += c * x; mxx += c * x * x; });
        var mean = mx / m;
        return mxx / m - mean * mean;
    }
}
```

- [ ] **Step 2: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests --filter FullyQualifiedName~DiffusionSimulationPhysicsTests`
Expected: błąd kompilacji — brak metody `Advance`.

- [ ] **Step 3: Zaimplementuj `Advance` zgodnie ze spec 3.2**

Jeden krok (powtarzany `steps` razy), na buforach `_f` → `_next`:

```
_next.Clear()
for każdej komórki płynu (x, y):
    C = Σᵢ f[cell, i]
    for i in 0..8:
        post = f[cell, i] - (f[cell, i] - w[i]·C) / τ
        (tx, ty) = (x + Ex[i], y + Ey[i])
        if (tx, ty) w siatce i nie jest ścianą: _next[target, i] = post
        else:                                   _next[cell, Opposite[i]] = post
swap(_f, _next); StepCount++
```

Każdy slot komórki płynu dostaje dokładnie jeden wkład (od sąsiada albo z odbicia), więc wystarczy `=`. `Clear()` jest wymagane, bo sloty komórek ścian nie są zapisywane i muszą pozostać zerami w obu buforach.

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → wszystkie PASS; build Release → 0 ostrzeżeń.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add BGK collision and bounce-back streaming to DiffusionSimulation"
```

---

### Task 4: Scenariusz — pudełko ze ścianą działową

**Files:**
- Create: `src/LatticeBoltzmann.Core/PartitionedBox.cs`
- Test: `tests/LatticeBoltzmann.Core.Tests/PartitionedBoxTests.cs`

**Interfaces:**
- Consumes: `DiffusionSimulation` z `Advance` (Task 3).
- Produces: `public static class PartitionedBox`:
  `static int PartitionColumn(int width)`,
  `static DiffusionSimulation Create(int width, int height, int gapWidth, double diffusionCoefficient)`,
  `static void ApplyGap(DiffusionSimulation simulation, int gapWidth)`.

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
namespace LatticeBoltzmann.Core.Tests;

public class PartitionedBoxTests
{
    [Theory]
    [InlineData(200, 66)]
    [InlineData(90, 30)]
    public void PartitionColumn_is_one_third_of_width(int width, int expected) =>
        Assert.Equal(expected, PartitionedBox.PartitionColumn(width));

    [Fact]
    public void Create_builds_partition_with_centered_gap()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        for (var y = 0; y < 40; y++)
            Assert.Equal(y < 15 || y > 24, sim.IsWall(30, y));
        for (var x = 0; x < 90; x++) for (var y = 0; y < 40; y++)
            if (x != 30) Assert.False(sim.IsWall(x, y));
    }

    [Fact]
    public void Create_fills_left_chamber_only()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        for (var x = 0; x < 90; x++) for (var y = 0; y < 40; y++)
            if (!sim.IsWall(x, y)) Assert.Equal(x < 30 ? 1.0 : 0.0, sim.GetConcentration(x, y), 15);
        Assert.Equal(1200.0, sim.TotalMass, 9);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(41)]
    public void Gap_outside_range_is_rejected(int gap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedBox.Create(90, 40, gap, 0.2));
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedBox.ApplyGap(sim, gap));
    }

    [Fact]
    public void ApplyGap_rebuilds_only_partition_column()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        sim.SetWall(60, 5, true);
        PartitionedBox.ApplyGap(sim, 20);
        for (var y = 0; y < 40; y++)
            Assert.Equal(y < 10 || y > 29, sim.IsWall(30, y));
        Assert.True(sim.IsWall(60, 5));
    }

    [Fact]
    public void Closed_gap_keeps_right_chamber_empty()
    {
        var sim = PartitionedBox.Create(90, 40, 0, 0.3);
        sim.Advance(1000);
        for (var x = 31; x < 90; x++) for (var y = 0; y < 40; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }

    [Fact]
    public void Field_stays_symmetric_about_gap_axis()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        sim.Advance(500);
        for (var x = 0; x < 90; x++) for (var y = 0; y < 20; y++)
            Assert.Equal(sim.GetConcentration(x, y), sim.GetConcentration(x, 39 - y), 12);
    }
}
```

- [ ] **Step 2: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests --filter FullyQualifiedName~PartitionedBoxTests`
Expected: błąd kompilacji — brak typu `PartitionedBox`.

- [ ] **Step 3: Zaimplementuj `PartitionedBox` w `src/LatticeBoltzmann.Core/PartitionedBox.cs`**

`gapStart = (height - gapWidth) / 2`; `ApplyGap` wywołuje `SetWall(PartitionColumn(width), y, y poza otworem)` dla każdego `y` (reguła zmian geometrii robi resztę). `Create`: nowa symulacja → `ApplyGap` → `SetConcentration(x, y, 1)` dla płynu z `x < PartitionColumn`.

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → wszystkie PASS; build Release → 0 ostrzeżeń.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add partitioned box scenario with adjustable gap"
```

---

### Task 5: Paleta i renderowanie pola do bufora

**Files:**
- Create: `src/LatticeBoltzmann.Core/Visualization/Colormap.cs`, `src/LatticeBoltzmann.Core/Visualization/FieldRenderer.cs`
- Test: `tests/LatticeBoltzmann.Core.Tests/ColormapTests.cs`, `tests/LatticeBoltzmann.Core.Tests/FieldRendererTests.cs`

**Interfaces:**
- Consumes: `DiffusionSimulation` (Task 2).
- Produces (namespace `LatticeBoltzmann.Core.Visualization`):
  `public static class Colormap { const int WallArgb; static int ToArgb(double value); }`,
  `public static class FieldRenderer { static void Render(DiffusionSimulation simulation, Span<int> argb); }`.

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class ColormapTests
{
    [Fact]
    public void Endpoints_match_spec_colors()
    {
        Assert.Equal(unchecked((int)0xFFFCFCFB), Colormap.ToArgb(0.0));
        Assert.Equal(unchecked((int)0xFF0D366B), Colormap.ToArgb(1.0));
        Assert.Equal(unchecked((int)0xFF1A1A19), Colormap.WallArgb);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(double.NaN, 0.0)]
    [InlineData(2.0, 1.0)]
    public void Values_outside_unit_range_are_clamped(double value, double clamped) =>
        Assert.Equal(Colormap.ToArgb(clamped), Colormap.ToArgb(value));

    [Fact]
    public void Colors_are_opaque_and_get_darker()
    {
        var previous = double.MaxValue;
        for (var k = 0; k <= 255; k++)
        {
            var argb = Colormap.ToArgb(k / 255.0);
            Assert.Equal(255, (argb >> 24) & 0xFF);
            var luminance = RelativeLuminance(argb);
            Assert.True(luminance <= previous + 1e-12, $"luminance rose at {k}");
            previous = luminance;
        }
    }

    private static double RelativeLuminance(int argb)
    {
        static double Lin(int c) { var s = c / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * Lin((argb >> 16) & 0xFF) + 0.7152 * Lin((argb >> 8) & 0xFF) + 0.0722 * Lin(argb & 0xFF);
    }
}

public class FieldRendererTests
{
    [Fact]
    public void Walls_use_wall_color_and_fluid_uses_colormap()
    {
        var sim = new DiffusionSimulation(3, 3, 0.2);
        sim.SetConcentration(0, 0, 1.0);
        sim.SetWall(1, 0, true);
        var argb = new int[9];
        FieldRenderer.Render(sim, argb);
        Assert.Equal(Colormap.ToArgb(1.0), argb[0]);
        Assert.Equal(Colormap.WallArgb, argb[1]);
        Assert.All(argb[2..], v => Assert.Equal(Colormap.ToArgb(0.0), v));
    }

    [Fact]
    public void Wrong_buffer_length_is_rejected() =>
        Assert.Throws<ArgumentException>(() => FieldRenderer.Render(new DiffusionSimulation(3, 3, 0.2), new int[8]));
}
```

- [ ] **Step 2: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests --filter "FullyQualifiedName~ColormapTests|FullyQualifiedName~FieldRendererTests"`
Expected: błąd kompilacji — brak namespace `LatticeBoltzmann.Core.Visualization`.

- [ ] **Step 3: Zaimplementuj `Colormap` i `FieldRenderer`**

`Colormap`: 14 punktów kontrolnych ze spec 3.4 w tej kolejności, rozmieszczonych co 1/13; statyczna tablica 256 kolorów liczona raz (liniowa interpolacja kanałów sRGB, zaokrąglenie do najbliższej liczby całkowitej); `ToArgb` mapuje `value` → indeks `(int)Math.Round(Math.Clamp(value, 0, 1) * 255)`, NaN → 0. `FieldRenderer`: `CopyConcentration` do bufora z `ArrayPool<double>.Shared`, potem ściana → `WallArgb`, płyn → `ToArgb`.

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → wszystkie PASS; build Release → 0 ostrzeżeń.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add sequential blue colormap and field renderer"
```

---

### Task 6: Mapowanie widoku i pędzel 4-spójny

**Files:**
- Create: `src/LatticeBoltzmann.Core/Visualization/ViewMapping.cs`, `src/LatticeBoltzmann.Core/Visualization/BrushStroke.cs`
- Test: `tests/LatticeBoltzmann.Core.Tests/ViewMappingTests.cs`, `tests/LatticeBoltzmann.Core.Tests/BrushStrokeTests.cs`

**Interfaces:**
- Consumes: `DiffusionSimulation` (test szczelności).
- Produces (namespace `LatticeBoltzmann.Core.Visualization`):
  `public readonly record struct ViewMapping` z konstruktorem `ViewMapping(double viewWidth, double viewHeight, int gridWidth, int gridHeight)`, właściwościami `Scale`, `OffsetX`, `OffsetY`, `DrawWidth`, `DrawHeight` (`double`) i metodą `bool TryGetCell(double px, double py, out int x, out int y)`;
  `public static class BrushStroke { const int MinSize = 1; const int MaxSize = 8; static IReadOnlyList<(int X, int Y)> Cells(int x0, int y0, int x1, int y1, int size, int gridWidth, int gridHeight); }`.

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class ViewMappingTests
{
    [Fact]
    public void Wide_view_is_centered_horizontally()
    {
        var m = new ViewMapping(400, 100, 200, 100);
        Assert.Equal((1.0, 100.0, 0.0, 200.0, 100.0), (m.Scale, m.OffsetX, m.OffsetY, m.DrawWidth, m.DrawHeight));
    }

    [Fact]
    public void Tall_view_is_centered_vertically()
    {
        var m = new ViewMapping(100, 400, 50, 100);
        Assert.Equal((2.0, 0.0, 100.0, 100.0, 200.0), (m.Scale, m.OffsetX, m.OffsetY, m.DrawWidth, m.DrawHeight));
    }

    [Theory]
    [InlineData(100.0, 0.0, 0, 0)]
    [InlineData(299.9, 99.9, 199, 99)]
    [InlineData(150.5, 10.2, 50, 10)]
    public void Points_inside_image_map_to_cells(double px, double py, int ex, int ey)
    {
        Assert.True(new ViewMapping(400, 100, 200, 100).TryGetCell(px, py, out var x, out var y));
        Assert.Equal((ex, ey), (x, y));
    }

    [Theory]
    [InlineData(99.9, 50.0)]
    [InlineData(300.0, 50.0)]
    [InlineData(150.0, -0.1)]
    [InlineData(150.0, 100.0)]
    public void Points_outside_image_map_to_nothing(double px, double py) =>
        Assert.False(new ViewMapping(400, 100, 200, 100).TryGetCell(px, py, out _, out _));

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-5.0, 10.0)]
    public void Empty_view_maps_nothing(double vw, double vh)
    {
        var m = new ViewMapping(vw, vh, 200, 100);
        Assert.Equal(0.0, m.Scale);
        Assert.False(m.TryGetCell(0, 0, out _, out _));
    }

    [Fact]
    public void Non_positive_grid_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ViewMapping(100, 100, 0, 10));
}

public class BrushStrokeTests
{
    [Fact]
    public void Single_point_with_size_one_is_one_cell() =>
        Assert.Equal(new[] { (2, 2) }, BrushStroke.Cells(2, 2, 2, 2, 1, 10, 10));

    [Fact]
    public void Diagonal_line_steps_along_x_first() =>
        Assert.Equal(new[] { (0, 0), (1, 0), (1, 1), (2, 1), (2, 2) }, BrushStroke.Cells(0, 0, 2, 2, 1, 10, 10));

    [Fact]
    public void Line_is_four_connected()
    {
        var cells = BrushStroke.Cells(0, 0, 7, 3, 1, 10, 10);
        Assert.Equal((0, 0), cells[0]);
        Assert.Equal((7, 3), cells[^1]);
        for (var k = 1; k < cells.Count; k++)
            Assert.Equal(1, Math.Abs(cells[k].X - cells[k - 1].X) + Math.Abs(cells[k].Y - cells[k - 1].Y));
    }

    [Fact]
    public void Size_two_is_plus_shape() =>
        Assert.Equal(new HashSet<(int, int)> { (5, 5), (4, 5), (6, 5), (5, 4), (5, 6) },
                     BrushStroke.Cells(5, 5, 5, 5, 2, 10, 10).ToHashSet());

    [Fact]
    public void Cells_are_clipped_to_grid() =>
        Assert.Equal(new HashSet<(int, int)> { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (0, 2) },
                     BrushStroke.Cells(0, 0, 0, 0, 3, 10, 10).ToHashSet());

    [Fact]
    public void Cells_are_unique()
    {
        var cells = BrushStroke.Cells(1, 1, 6, 1, 3, 10, 10);
        Assert.Equal(cells.Count, cells.Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void Invalid_size_is_rejected(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BrushStroke.Cells(0, 0, 1, 1, size, 10, 10));

    [Fact]
    public void Drawn_diagonal_wall_is_impermeable()
    {
        var sim = new DiffusionSimulation(60, 60, 1.0 / 6.0);
        foreach (var (x, y) in BrushStroke.Cells(0, 0, 59, 59, 1, 60, 60)) sim.SetWall(x, y, true);
        for (var x = 0; x < 60; x++) for (var y = 0; y < x; y++)
            if (!sim.IsWall(x, y)) sim.SetConcentration(x, y, 1.0);
        sim.Advance(200);
        for (var x = 0; x < 60; x++) for (var y = x + 1; y < 60; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }
}
```

- [ ] **Step 2: Uruchom i potwierdź porażkę**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests --filter "FullyQualifiedName~ViewMappingTests|FullyQualifiedName~BrushStrokeTests"`
Expected: błąd kompilacji — brak `ViewMapping` i `BrushStroke`.

- [ ] **Step 3: Zaimplementuj `ViewMapping` i `BrushStroke`**

`ViewMapping`: ujemne rozmiary widoku traktowane jak 0; `Scale = min(vw/gw, vh/gh)`; offsety `(vw − DrawWidth)/2`, `(vh − DrawHeight)/2`; `TryGetCell` zwraca `false` przy `Scale == 0` lub komórce poza `[0, grid)`; komórka = `(int)Math.Floor((p − offset) / Scale)`.

`BrushStroke` — linia (punkty przed rozszerzeniem pędzlem):

```
dx = |x1−x0|, dy = |y1−y0|, sx = sign(x1−x0), sy = sign(y1−y0), err = dx − dy
emit(x, y)
while (x, y) != (x1, y1):
    e2 = 2·err
    stepX = e2 > −dy; stepY = e2 < dx
    if stepX: err −= dy; x += sx; emit(x, y)
    if stepY: err += dx; y += sy; emit(x, y)
```

Każdy wyemitowany punkt rozszerzany o przesunięcia z `dx² + dy² ≤ (size − 1)²`, przycięty do siatki, deduplikowany `HashSet` z zachowaniem kolejności pierwszego wystąpienia.

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → wszystkie PASS; build Release → 0 ostrzeżeń.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add view mapping and leak-proof 4-connected brush strokes"
```

---

### Task 7: Aplikacja — widok pola, pętla animacji, obsługa błędów

**Files:**
- Create: `src/LatticeBoltzmann.App/LatticeBoltzmann.App.csproj`, `Program.cs`, `MainForm.cs`, `SimulationView.cs`
- Create: `tests/LatticeBoltzmann.App.Tests/LatticeBoltzmann.App.Tests.csproj`, `StaThread.cs`, `SimulationViewTests.cs`, `MainFormTests.cs`
- Modify: `LatticeBoltzmann.slnx` (dodaj oba projekty)

**Interfaces:**
- Consumes: `PartitionedBox`, `DiffusionSimulation`, `FieldRenderer`, `ViewMapping`, `BrushStroke`, `Colormap`.
- Produces (namespace `LatticeBoltzmann.App`, wszystko `internal`):
  - `sealed class SimulationView : Control` — `DiffusionSimulation? Simulation { get; set; }`, `int BrushSize { get; set; }` (1–8, domyślnie 2), `void RefreshField()`, `void PaintField(Graphics graphics)` (wołane z `OnPaint`), `Bitmap? FieldBitmap { get; }`, `void BeginStroke(PointF location, MouseButtons button)`, `void ContinueStroke(PointF location)`, `void EndStroke()`.
  - `sealed partial class MainForm : Form` — `DiffusionSimulation Simulation { get; }`, `SimulationView View { get; }`, `bool IsRunning { get; }`, `string StatusText { get; }`, `int StepsPerFrame { get; set; }`, `void StartSimulation()`, `void StopSimulation()`, `void ToggleSimulation()`, `void StepOnce()`, `void ResetSimulation()`.
  - `static class Program` z `Main` i obsługą błędów ze spec 4.4.

- [ ] **Step 1: Utwórz projekty**

App: `dotnet new winforms`, usuń `Form1*`; csproj: `OutputType=WinExe`, `TargetFramework=net10.0-windows`, `UseWindowsForms=true`, `EnableWindowsTargeting=true`, `ApplicationHighDpiMode=PerMonitorV2`, `ApplicationVisualStyles=true`, `AssemblyTitle`/`Product` = „Lattice Boltzmann — dyfuzja”, `InternalsVisibleTo` `LatticeBoltzmann.App.Tests`, referencja do Core. App.Tests: `net10.0-windows`, `UseWindowsForms=true`, `EnableWindowsTargeting=true`, pakiety testowe, referencje do App i Core, `<Using Include="Xunit" />`. Oba w `.slnx`.

- [ ] **Step 2: Napisz testy, które nie przejdą**

`StaThread.Run(Action)` uruchamia akcję na nowym wątku `ApartmentState.STA`, czeka na koniec i ponownie rzuca złapany wyjątek (`ExceptionDispatchInfo`).

```csharp
using System.Drawing;
using System.Windows.Forms;
using LatticeBoltzmann.Core;
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.App.Tests;

public class SimulationViewTests
{
    private static SimulationView CreateView(int w = 200, int h = 120) => new()
    {
        Size = new Size(w, h),
        Simulation = PartitionedBox.Create(w, h, 20, 0.2),
    };

    [Fact]
    public void RefreshField_renders_simulation_colors() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.RefreshField();
        Assert.Equal(Colormap.ToArgb(1.0), view.FieldBitmap!.GetPixel(10, 60).ToArgb());
        Assert.Equal(Colormap.ToArgb(0.0), view.FieldBitmap.GetPixel(190, 60).ToArgb());
        Assert.Equal(Colormap.WallArgb, view.FieldBitmap.GetPixel(66, 5).ToArgb());
    });

    [Fact]
    public void View_paints_into_bitmap() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.RefreshField();
        using var bmp = new Bitmap(200, 120);
        view.DrawToBitmap(bmp, new Rectangle(0, 0, 200, 120));
        Assert.NotEqual(bmp.GetPixel(10, 60).ToArgb(), bmp.GetPixel(190, 60).ToArgb());
    });

    [Fact]
    public void Left_drag_draws_wall_and_right_drag_erases_it() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.BrushSize = 1;
        view.BeginStroke(new PointF(150.5f, 10.5f), MouseButtons.Left);
        view.ContinueStroke(new PointF(150.5f, 30.5f));
        view.EndStroke();
        for (var y = 10; y <= 30; y++) Assert.True(view.Simulation!.IsWall(150, y));
        view.BeginStroke(new PointF(150.5f, 20.5f), MouseButtons.Right);
        view.EndStroke();
        Assert.False(view.Simulation!.IsWall(150, 20));
    });

    [Fact]
    public void Stroke_restarts_after_leaving_the_image() => StaThread.Run(() =>
    {
        using var view = CreateView(200, 120);
        view.Size = new Size(400, 120);              // image at x 100..300
        view.BrushSize = 1;
        view.BeginStroke(new PointF(120.5f, 10.5f), MouseButtons.Left);   // cell (20, 10)
        view.ContinueStroke(new PointF(50f, 10.5f));                      // outside
        view.ContinueStroke(new PointF(180.5f, 10.5f));                   // cell (80, 10)
        view.EndStroke();
        Assert.True(view.Simulation!.IsWall(20, 10));
        Assert.True(view.Simulation.IsWall(80, 10));
        Assert.False(view.Simulation.IsWall(50, 10));
    });

    [Fact]
    public void Replacing_simulation_during_drag_is_safe() => StaThread.Run(() =>
    {
        using var view = CreateView(400, 240);
        view.BeginStroke(new PointF(390.5f, 230.5f), MouseButtons.Left);
        view.Simulation = PartitionedBox.Create(120, 72, 12, 0.2);
        view.ContinueStroke(new PointF(10.5f, 10.5f));
        view.EndStroke();
        view.RefreshField();
        Assert.Equal(new Size(120, 72), view.FieldBitmap!.Size);
    });

    [Fact]
    public void Zero_sized_view_refreshes_and_paints_without_error() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.Size = Size.Empty;
        view.RefreshField();
        view.BeginStroke(new PointF(0, 0), MouseButtons.Left);
        view.ContinueStroke(new PointF(0, 0));
        view.EndStroke();
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        view.PaintField(g);
    });
}

public class MainFormTests
{
    [Fact]
    public void Form_starts_paused_with_default_scenario() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.False(form.IsRunning);
        Assert.Equal((200, 120, 0L), (form.Simulation.Width, form.Simulation.Height, form.Simulation.StepCount));
        Assert.Equal(0.2, form.Simulation.DiffusionCoefficient);
        Assert.Equal(10, form.StepsPerFrame);
        Assert.Equal("Krok: 0 · Pauza", form.StatusText);
    });

    [Fact]
    public void StepOnce_advances_one_step_and_updates_status() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        Assert.Equal(1, form.Simulation.StepCount);
        Assert.Equal("Krok: 1 · Pauza", form.StatusText);
    });

    [Fact]
    public void Start_and_stop_toggle_running_state() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StartSimulation();
        Assert.True(form.IsRunning);
        Assert.Equal("Krok: 0 · Działa", form.StatusText);
        form.ToggleSimulation();
        Assert.False(form.IsRunning);
    });

    [Fact]
    public void Reset_restores_initial_state() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.View.BeginStroke(new PointF(1, 1), MouseButtons.Left);
        form.ResetSimulation();
        Assert.Equal(0, form.Simulation.StepCount);
        Assert.Equal(PartitionedBox.Create(200, 120, 20, 0.2).TotalMass, form.Simulation.TotalMass, 9);
    });
}
```

- [ ] **Step 3: Zbuduj i potwierdź porażkę**

Run: `dotnet build tests/LatticeBoltzmann.App.Tests`
Expected: błędy kompilacji — brak `SimulationView`, `MainForm`.

- [ ] **Step 4: Zaimplementuj `SimulationView`**

Wg spec 4.2. `DoubleBuffered = true`, `ResizeRedraw = true`, tło `SystemColors.Control`. Właściwości publiczne kontrolki oznacz `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` (analizator WinForms WFO1000). Ustawienie `Simulation` zwalnia starą bitmapę, tworzy nową (`Format32bppArgb`, rozmiar siatki), zeruje stan pociągnięcia i woła `RefreshField()`. Pociągnięcie: `BeginStroke` zapamiętuje przycisk (tylko `Left` = ściana, `Right` = gumka; inne ignoruje) i maluje komórkę pod kursorem; `ContinueStroke` przy `TryGetCell == false` zeruje ostatnią komórkę, w przeciwnym razie maluje `BrushStroke.Cells(ostatnia ?? bieżąca, bieżąca, …)`; `EndStroke` kończy. Po malowaniu `RefreshField()`. `OnMouseDown/Move/Up` delegują do tych metod, `OnPaint` do `PaintField` (przy `Scale == 0` rysuje tylko tło). `Dispose(bool)` zwalnia bitmapę.

- [ ] **Step 5: Zaimplementuj `MainForm` (część z widokiem i pętlą) i `Program`**

`MainForm.cs`: stałe domyślne z Global Constraints, `System.Windows.Forms.Timer` (16 ms), `StatusStrip` z etykietą; `StatusText` = `$"Krok: {StepCount.ToString("N0", pl)} · {(IsRunning ? "Działa" : "Pauza")}"`; tick → `Advance(StepsPerFrame)` → `View.RefreshField()` → status. `ResetSimulation` tworzy `PartitionedBox.Create(gridW, gridH, gap, D)` z bieżących pól i przypisuje do `View.Simulation`. `StepsPerFrame` waliduje 1–50. Tytuł „Lattice Boltzmann — dyfuzja”, `MinimumSize` 960×600, `AutoScaleMode.Dpi`. Na tym etapie `View` wypełnia formularz (`Dock.Fill`); panel sterowania dochodzi w Task 8. Zamknięcie formularza zatrzymuje i zwalnia timer.

`Program.cs` wg spec 4.4; `ReportError` zatrzymuje każdy otwarty `MainForm` i pokazuje `MessageBox.Show($"Wystąpił nieoczekiwany błąd: {ex.Message}", "Lattice Boltzmann", MessageBoxButtons.OK, MessageBoxIcon.Error)`.

- [ ] **Step 6: Zbuduj**

Run: `dotnet build LatticeBoltzmann.slnx -c Release` → 0 ostrzeżeń, 0 błędów.
Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → PASS (bez regresji).
(Testy App uruchamia CI na Windows w Task 9.)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add WinForms app with simulation view, animation loop and error handling"
```

---

### Task 8: Panel sterowania, legenda i skróty klawiszowe

**Files:**
- Create: `src/LatticeBoltzmann.App/MainForm.Layout.cs`, `src/LatticeBoltzmann.App/ColorLegend.cs`
- Modify: `src/LatticeBoltzmann.App/MainForm.cs`
- Test: `tests/LatticeBoltzmann.App.Tests/MainFormControlsTests.cs`

**Interfaces:**
- Consumes: `MainForm` i `SimulationView` (Task 7), `Colormap` (Task 5), `PartitionedBox.ApplyGap` (Task 4).
- Produces (`internal` w `MainForm`): `Button StartPauseButton`, `Button StepButton`, `Button ResetButton`, `TrackBar DiffusionTrackBar` (5–50, wartość/100 = D), `Label DiffusionLabel`, `NumericUpDown StepsPerFrameInput` (1–50), `TrackBar GapTrackBar` (0–Height/2), `Label GapLabel`, `ComboBox ResolutionComboBox`, `TrackBar BrushTrackBar` (1–8), `Label BrushLabel`, `bool HandleShortcut(Keys keyData)`; klasa `internal sealed class ColorLegend : Control`.

- [ ] **Step 1: Napisz testy, które nie przejdą**

```csharp
using System.Windows.Forms;
using LatticeBoltzmann.Core;

namespace LatticeBoltzmann.App.Tests;

public class MainFormControlsTests
{
    [Fact]
    public void Controls_show_default_values() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.Equal("Start", form.StartPauseButton.Text);
        Assert.Equal(20, form.DiffusionTrackBar.Value);
        Assert.Equal("D = 0,20 (τ = 1,10)", form.DiffusionLabel.Text);
        Assert.Equal(10, (int)form.StepsPerFrameInput.Value);
        Assert.Equal((20, 60), (form.GapTrackBar.Value, form.GapTrackBar.Maximum));
        Assert.Equal("Szerokość otworu: 20", form.GapLabel.Text);
        Assert.Equal(new[] { "120 × 72", "200 × 120", "300 × 180", "400 × 240" }, form.ResolutionComboBox.Items.Cast<string>().ToArray());
        Assert.Equal(1, form.ResolutionComboBox.SelectedIndex);
        Assert.Equal(2, form.BrushTrackBar.Value);
        Assert.Equal("Rozmiar pędzla: 2", form.BrushLabel.Text);
    });

    [Fact]
    public void Diffusion_slider_updates_simulation_live() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.DiffusionTrackBar.Value = 50;
        Assert.Equal(0.5, form.Simulation.DiffusionCoefficient, 12);
        Assert.Equal("D = 0,50 (τ = 2,00)", form.DiffusionLabel.Text);
        Assert.Equal(1, form.Simulation.StepCount);
    });

    [Fact]
    public void Gap_slider_rebuilds_partition_live() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.GapTrackBar.Value = 0;
        var column = PartitionedBox.PartitionColumn(form.Simulation.Width);
        for (var y = 0; y < form.Simulation.Height; y++) Assert.True(form.Simulation.IsWall(column, y));
    });

    [Fact]
    public void Steps_and_brush_inputs_apply() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepsPerFrameInput.Value = 50;
        form.BrushTrackBar.Value = 5;
        Assert.Equal(50, form.StepsPerFrame);
        Assert.Equal(5, form.View.BrushSize);
    });

    [Fact]
    public void Resolution_change_resets_and_scales_gap() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.ResolutionComboBox.SelectedIndex = 0;
        Assert.Equal((120, 72, 0L), (form.Simulation.Width, form.Simulation.Height, form.Simulation.StepCount));
        Assert.Equal((12, 36), (form.GapTrackBar.Value, form.GapTrackBar.Maximum));
    });

    [Fact]
    public void Gap_at_maximum_survives_every_resolution() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        foreach (var index in new[] { 3, 0, 2, 1, 0, 3 })
        {
            form.GapTrackBar.Value = form.GapTrackBar.Maximum;
            form.ResolutionComboBox.SelectedIndex = index;
            Assert.InRange(form.GapTrackBar.Value, 0, form.GapTrackBar.Maximum);
            Assert.Equal(form.Simulation.Height / 2, form.GapTrackBar.Maximum);
        }
    });

    [Fact]
    public void Shortcuts_control_the_simulation() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.True(form.HandleShortcut(Keys.Space));
        Assert.True(form.IsRunning);
        Assert.Equal("Pauza", form.StartPauseButton.Text);
        Assert.True(form.HandleShortcut(Keys.Space));
        Assert.False(form.IsRunning);
        Assert.True(form.HandleShortcut(Keys.N));
        Assert.Equal(1, form.Simulation.StepCount);
        Assert.True(form.HandleShortcut(Keys.R));
        Assert.Equal(0, form.Simulation.StepCount);
        Assert.False(form.HandleShortcut(Keys.A));
    });
}
```

- [ ] **Step 2: Zbuduj i potwierdź porażkę**

Run: `dotnet build tests/LatticeBoltzmann.App.Tests`
Expected: błędy kompilacji — brak `StartPauseButton`, `HandleShortcut` itd.

- [ ] **Step 3: Zbuduj układ w `MainForm.Layout.cs`**

Wg spec 4.1: `TableLayoutPanel` (2 kolumny: 100% i stała szerokość `LogicalToDeviceUnits(280)`), w prawej `FlowLayoutPanel` (`TopDown`, `WrapContents = false`, `AutoScroll = true`) z `GroupBox`-ami „Symulacja”, „Parametry”, „Rysowanie”, „Legenda”. Napisy: przyciski „Start”/„Pauza”, „Krok”, „Reset”; etykiety „Kroki na klatkę”, „Rozdzielczość”; w grupie Rysowanie opis „Lewy przycisk — ściana, prawy — gumka”. `ToolTip`: „Start / pauza (Spacja)”, „Jeden krok (N)”, „Przywróć stan początkowy (R)”. `StatusStrip` zadokowany na dole.

- [ ] **Step 4: Podłącz kontrolki i skróty w `MainForm.cs`**

- `DiffusionTrackBar.ValueChanged` → `Simulation.DiffusionCoefficient = Value / 100.0`, etykieta `$"D = {d.ToString("0.00", pl)} (τ = {tau.ToString("0.00", pl)})"`.
- `GapTrackBar.ValueChanged` → `PartitionedBox.ApplyGap`, `View.RefreshField()`, etykieta `$"Szerokość otworu: {gap}"`.
- `ResolutionComboBox.SelectedIndexChanged` → `newGap = Math.Clamp((int)Math.Round(gap * newHeight / (double)oldHeight), 0, newHeight / 2)`; zapisz nowe wymiary i `newGap` w polach; `ResetSimulation()` (nowa siatka już z `newGap`); dopiero potem, z flagą `_updatingControls = true` (handler `GapTrackBar.ValueChanged` ją sprawdza i nic nie robi), ustaw `GapTrackBar.Maximum = newHeight / 2` i `GapTrackBar.Value = newGap`, zaktualizuj etykietę. Dzięki temu `ApplyGap` nigdy nie dostaje otworu szerszego niż bieżąca siatka (np. 120 przy wysokości 72), a `TrackBar` nie dostaje wartości spoza zakresu.
- `StepsPerFrameInput.ValueChanged` → `StepsPerFrame`; `BrushTrackBar.ValueChanged` → `View.BrushSize`, etykieta `$"Rozmiar pędzla: {size}"`.
- `StartSimulation`/`StopSimulation` ustawiają tekst `StartPauseButton` („Pauza”/„Start”).
- `HandleShortcut`: `Keys.Space` → `ToggleSimulation`, `Keys.N` → `StepOnce`, `Keys.R` → `ResetSimulation`; zwraca `true` dla obsłużonych. `ProcessCmdKey` woła `HandleShortcut` i zwraca `true`, gdy obsłużone.

- [ ] **Step 5: Zaimplementuj `ColorLegend`**

Wysokość `LogicalToDeviceUnits(64)`; tytuł „Stężenie”, pasek gradientu z `Colormap.ToArgb(x / (szerokość − 1))` (rysowany raz do bitmapy przy zmianie rozmiaru), podpisy „0” i „1” pod końcami, obok próbka `Colormap.WallArgb` z podpisem „Ściana”. Tekst w `SystemColors.ControlText`. `DoubleBuffered`, `ResizeRedraw`; zwalnia bitmapę w `Dispose`.

- [ ] **Step 6: Zbuduj**

Run: `dotnet build LatticeBoltzmann.slnx -c Release` → 0 ostrzeżeń, 0 błędów.
Run: `dotnet test tests/LatticeBoltzmann.Core.Tests` → PASS.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add control panel, color legend and keyboard shortcuts"
```

---

### Task 9: CI, README i weryfikacja wydania

**Files:**
- Create: `.github/workflows/ci.yml`, `README.md`
- Modify: `.gitignore` (dopisz `artifacts/`, jeśli brak)

**Interfaces:**
- Consumes: całe rozwiązanie.

- [ ] **Step 1: Napisz workflow `.github/workflows/ci.yml`**

`name: CI`; `on: push` (wszystkie gałęzie) i `pull_request`; job `build` na `windows-latest`: checkout → `actions/setup-dotnet` z `global-json-file: global.json` → `dotnet restore LatticeBoltzmann.slnx` → `dotnet build LatticeBoltzmann.slnx -c Release --no-restore` → `dotnet test LatticeBoltzmann.slnx -c Release --no-build` → `dotnet publish src/LatticeBoltzmann.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64` → `actions/upload-artifact` z nazwą `LatticeBoltzmann-win-x64`. Wersje akcji: najnowsze główne wydania potwierdzone `git ls-remote --tags` (checkout, setup-dotnet, upload-artifact).

- [ ] **Step 2: Napisz `README.md`** (po polsku, zawartość wg spec 7)

Sekcje: opis i zrzut zachowania (słownie), model (wzory D2Q9/BGK/odbicie, D = (τ − ½)/3), sterowanie i skróty, wymagania, budowanie (`dotnet build`), testy (`dotnet test tests/LatticeBoltzmann.Core.Tests`; testy UI tylko na Windows), wydanie (polecenie `dotnet publish` z kroku 1), znane ograniczenia (przestrzały dla τ < 1; rysowanie ściany usuwa zawartość; ukośne styki narożnikami przepuszczają populacje ukośne, dlatego pędzel rysuje linie 4-spójne).

- [ ] **Step 3: Weryfikacja lokalna**

Run: `dotnet build LatticeBoltzmann.slnx -c Release` → `0 Warning(s)`, `0 Error(s)`.
Run: `dotnet test tests/LatticeBoltzmann.Core.Tests -c Release` → wszystkie PASS.
Run: polecenie `dotnet publish` z kroku 1 → plik `artifacts/win-x64/LatticeBoltzmann.App.exe` istnieje.

- [ ] **Step 4: Commit i push**

```bash
git add -A
git commit -m "Add CI workflow and Polish README"
git push -u origin claude/project-explanation-bugs-yoornn
```

- [ ] **Step 5: Sprawdź CI**

Odczytaj wynik workflow `CI` dla ostatniego commita (GitHub MCP: `actions_list` / `get_job_logs`). Expected: job `build` zielony — w tym testy `LatticeBoltzmann.App.Tests`. Czerwony wynik naprawiasz u źródła (superpowers:systematic-debugging) i pushujesz ponownie.
