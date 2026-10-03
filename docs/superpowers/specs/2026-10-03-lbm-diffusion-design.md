# Lattice Boltzmann — dyfuzja stężenia (projekt wersji produkcyjnej)

Data: 2026-10-03 · Status: do przeglądu

## 1. Cel

Zastąpić obecną, błędną symulację cząsteczek (WinForms, .NET Framework 4.7.2)
prawdziwą symulacją dyfuzji metodą Lattice Boltzmann, z porządnym UI,
automatycznymi testami fizyki i CI.

Kryteria sukcesu:

- Symulacja pokazuje, jak stężenie z lewej komory dyfunduje przez otwór
  w ścianie działowej do pustej prawej komory; kolor oznacza stężenie.
- Fizyka jest zweryfikowana testami: zachowanie masy, zgodność współczynnika
  dyfuzji z teorią, brak przepływu przez zamkniętą ścianę.
- UI działa płynnie (ciągła animacja), nie traci obrazu po zasłonięciu okna,
  pozwala zmieniać parametry i rysować ściany myszą.
- Build bez ostrzeżeń, testy zielone, CI na Windows buduje, testuje
  i publikuje gotowy plik `.exe`.

### Decyzje użytkownika

- Model: LBM dyfuzji stężenia (D2Q9, BGK, odbicie od ścian).
- Platforma: .NET 10 + WinForms.
- UI: ciągła animacja, panel parametrów, rysowanie ścian myszą.
  Bez statystyk na żywo.
- Architektura: osobny rdzeń + aplikacja + testy (podejście A).

### Założenia zaakceptowane przez użytkownika

1. Scenariusz startowy jak w oryginale: ściana działowa na 1/3 szerokości
   z otworem; lewa komora C = 1, prawa C = 0.
2. Stary kod (`Lattice boltzmann/`, `Lattice boltzmann.sln`) zostaje usunięty;
   pozostaje w historii gita.
3. Identyfikatory w kodzie po angielsku; napisy UI, komunikaty i README
   po polsku.
4. „Produkcyjnie” = testy fizyki, nullable, ostrzeżenia jako błędy,
   analizatory, CI na GitHub Actions (Windows), README z instrukcją
   budowania i wydania.
5. Bez statystyk na żywo; pasek stanu pokazuje tylko numer kroku i stan.
6. Okna nie da się uruchomić w środowisku deweloperskim (Linux); UI
   weryfikuje test dymny w CI na Windows oraz użytkownik lokalnie.

## 2. Architektura

```
LatticeBoltzmann.slnx
src/LatticeBoltzmann.Core/          net10.0, bez zależności od UI
src/LatticeBoltzmann.App/           net10.0-windows, WinForms
tests/LatticeBoltzmann.Core.Tests/  net10.0, xUnit (Linux + Windows)
tests/LatticeBoltzmann.App.Tests/   net10.0-windows, xUnit (tylko Windows)
```

Zależności: `App → Core`, `Core.Tests → Core`, `App.Tests → App, Core`.
Rdzeń nie zna WinForms ani System.Drawing — kolory to liczby `int` w formacie
ARGB. Cała logika, którą da się sprawdzić bez okna (fizyka, paleta, mapowanie
ekran↔komórka, pędzel, renderowanie do bufora), jest w rdzeniu.

## 3. Rdzeń (`LatticeBoltzmann.Core`)

### 3.1 Sieć D2Q9 (`D2Q9`)

Kierunki w ustalonej kolejności:

| i | eᵢ | wᵢ | przeciwny |
|---|----|----|-----------|
| 0 | (0, 0) | 4/9 | 0 |
| 1 | (1, 0) | 1/9 | 3 |
| 2 | (0, 1) | 1/9 | 4 |
| 3 | (−1, 0) | 1/9 | 1 |
| 4 | (0, −1) | 1/9 | 2 |
| 5 | (1, 1) | 1/36 | 7 |
| 6 | (−1, 1) | 1/36 | 8 |
| 7 | (−1, −1) | 1/36 | 5 |
| 8 | (1, −1) | 1/36 | 6 |

c_s² = 1/3. Oś y rośnie w dół (konwencja ekranu); model jest symetryczny,
więc nie wpływa to na fizykę.

### 3.2 Symulacja (`DiffusionSimulation`)

Stan: siatka `Width × Height` komórek; każda komórka jest płynem albo ścianą;
w komórce płynu 9 populacji fᵢ (`double`). Komórki ścian mają fᵢ = 0.
Brzeg siatki zachowuje się jak ściana.

Wielkości:

- stężenie C(x, y) = Σᵢ fᵢ (dla ściany 0),
- równowaga fᵢᵉq = wᵢ·C (czysta dyfuzja, brak adwekcji),
- czas relaksacji τ = 3·D + ½, czyli D = c_s²·(τ − ½).

Jeden krok (`Advance`):

1. **Kolizja BGK** w każdej komórce płynu: fᵢ* = fᵢ − (fᵢ − wᵢ·C)/τ.
2. **Streaming z odbiciem w połowie drogi:** dla każdej komórki płynu x
   i kierunku i: jeśli x + eᵢ leży w siatce i jest płynem, to
   fᵢ(x + eᵢ) ← fᵢ*(x); w przeciwnym razie f_opp(i)(x) ← fᵢ*(x).
3. Podmiana buforów, `StepCount++`.

Odbicie gwarantuje zachowanie masy (do zaokrągleń) i zerowy strumień przez
ściany.

API:

- `DiffusionSimulation(int width, int height, double diffusionCoefficient)` —
  wszystko płyn, C = 0. Wymaga `width ≥ 3`, `height ≥ 3`.
- `int Width`, `int Height`, `long StepCount`.
- `double DiffusionCoefficient { get; set; }` — zakres
  [`MinDiffusionCoefficient` = 0,05; `MaxDiffusionCoefficient` = 0,5],
  w przeciwnym razie `ArgumentOutOfRangeException`. Zmiana działa od
  następnego kroku.
- `double RelaxationTime` — τ wyliczane z D.
- `void Advance(int steps = 1)` — `steps ≥ 1`.
- `bool IsWall(int x, int y)`, `void SetWall(int x, int y, bool isWall)`.
- `double GetConcentration(int x, int y)`.
- `void SetConcentration(int x, int y, double value)` — tylko dla płynu,
  `value ≥ 0` i skończone; ustawia fᵢ = wᵢ·value (równowaga). Dla ściany
  `InvalidOperationException`.
- `double TotalMass` — suma C po komórkach płynu.
- `void CopyConcentration(Span<double> destination)` — długość
  `Width·Height`, kolejność wierszowa (`y·Width + x`), ściany = 0.

Współrzędne spoza siatki → `ArgumentOutOfRangeException`.

**Reguła zmian geometrii** (mysz, suwak otworu):

- `SetWall(x, y, true)` na płynie: komórka staje się ścianą, jej zawartość
  znika (fᵢ = 0).
- `SetWall(x, y, false)` na ścianie: komórka staje się płynem z C = 0.
- Wywołanie bez zmiany stanu nic nie robi (gumka przeciągnięta po płynie
  nie zeruje stężenia).

**Dokładność i stabilność:** dla τ ≥ 1 obowiązuje zasada maksimum
(C pozostaje w przedziale wartości początkowych). Dla τ < 1 na ostrych
frontach mogą wystąpić drobne przestrzały poza [0, 1]; masa nadal jest
zachowana, a paleta przycina wartości. To cecha metody, opisana w README.

### 3.3 Scenariusz (`PartitionedBox`)

- `static DiffusionSimulation Create(int width, int height, int gapWidth,
  double diffusionCoefficient)`.
- Kolumna ściany działowej: `PartitionX = width / 3`.
- Otwór wyśrodkowany: `gapStart = (height − gapWidth) / 2`, komórki
  `gapStart … gapStart + gapWidth − 1` są płynem, reszta kolumny — ścianą.
- Stan początkowy: płyn z `x < PartitionX` ma C = 1, pozostały płyn C = 0.
- `static void ApplyGap(DiffusionSimulation sim, int gapWidth)` — przebudowuje
  tylko kolumnę działową według reguły zmian geometrii (inne ściany
  użytkownika zostają). `0 ≤ gapWidth ≤ height`, inaczej
  `ArgumentOutOfRangeException`.
- `static int PartitionColumn(int width)` — zwraca `width / 3`.

### 3.4 Wizualizacja (`LatticeBoltzmann.Core.Visualization`)

**`Colormap`** — sekwencyjna, jedna barwa (niebieska), jasna → ciemna:

- Punkty kontrolne rozłożone równomiernie na [0, 1]:
  `#fcfcfb, #cde2fb, #b7d3f6, #9ec5f4, #86b6ef, #6da7ec, #5598e7, #3987e5,
  #2a78d6, #256abf, #1c5cab, #184f95, #104281, #0d366b`
  (tło + rampa 100–700 z palety referencyjnej skilla `dataviz`).
- Interpolacja liniowa w sRGB między sąsiednimi punktami; tablica 256
  wpisów liczona raz.
- `int ToArgb(double value)` — wartości < 0 i NaN → 0, > 1 → 1.
- `const int WallArgb` = `#1a1a19` (nieprzezroczysty). Separacja od każdego
  stopnia rampy zmierzona walidatorem palety: najgorzej ΔE 15,5 (deutan),
  14,5 (tritan) — powyżej celu 8.

**`FieldRenderer`** — `static void Render(DiffusionSimulation sim,
Span<int> argb)`: bufor `Width·Height`, kolejność wierszowa; ściana →
`WallArgb`, płyn → `Colormap.ToArgb(C)`.

**`ViewMapping`** — `readonly record struct` z rozmiaru widoku (px)
i siatki:

- `Scale = min(viewWidth / gridWidth, viewHeight / gridHeight)` (`double`),
- obraz wyśrodkowany: `OffsetX`, `OffsetY`, `DrawWidth`, `DrawHeight`,
- `bool TryGetCell(double px, double py, out int x, out int y)` —
  `x = floor((px − OffsetX) / Scale)`; `false` poza obrazem.
- Widok o zerowym rozmiarze: `Scale = 0`, `TryGetCell` zawsze `false`.

**`BrushStroke`** — `static IReadOnlyList<(int X, int Y)> Cells(int x0,
int y0, int x1, int y1, int size, int gridWidth, int gridHeight)`:

- linia **4-spójna** z (x0, y0) do (x1, y1): Bresenham, ale przy kroku
  po skosie dodawana jest komórka pośrednia (najpierw ruch w osi x), więc
  kolejne komórki linii różnią się o 1 w dokładnie jednej osi,
- wokół każdego punktu koło: komórki z dx² + dy² ≤ (size − 1)²
  (`size = 1` → jedna komórka),
- przycięte do siatki, bez duplikatów; `1 ≤ size ≤ 8`.

Uzasadnienie 4-spójności: w D2Q9 populacje ukośne przechodzą między dwiema
komórkami ściany, które stykają się tylko narożnikiem. Prototyp potwierdził,
że ukośna ściana 8-spójna o grubości 1 przepuszcza masę; linia 4-spójna jest
szczelna.

## 4. Aplikacja (`LatticeBoltzmann.App`)

### 4.1 Układ

- `MainForm` (tytuł „Lattice Boltzmann — dyfuzja”, minimalny rozmiar
  960×600, `AutoScaleMode.Dpi`, `ApplicationHighDpiMode.PerMonitorV2`).
- `TableLayoutPanel`: lewa kolumna `SimulationView` (wypełnia), prawa ~280 px
  (skalowane DPI) z panelem sterowania (`FlowLayoutPanel` z grupami).
- UI budowane w kodzie (`MainForm.Layout.cs`), bez plików Designera.

Grupy panelu:

1. **Symulacja:** [Start]/[Pauza] (jeden przycisk), [Krok], [Reset].
2. **Parametry:**
   - „Współczynnik dyfuzji”: `TrackBar` 0,05–0,50 co 0,01, etykieta
     „D = 0,20 (τ = 1,10)”; na żywo.
   - „Kroki na klatkę”: 1–50; na żywo.
   - „Szerokość otworu”: 0 … `Height / 2` komórek; na żywo przez
     `PartitionedBox.ApplyGap`.
   - „Rozdzielczość”: `ComboBox` (DropDownList) 120×72, 200×120, 300×180,
     400×240; zmiana = reset z nową siatką; szerokość otworu przeskalowana
     proporcjonalnie: `round(gap · newHeight / oldHeight)`, przycięta do
     zakresu.
3. **Rysowanie:** opis „Lewy przycisk — ściana, prawy — gumka”, suwak
   „Rozmiar pędzla” 1–8.
4. **Legenda:** `ColorLegend` — gradient z `Colormap`, podpisy „0” i „1”,
   tytuł „Stężenie”, próbka „Ściana”.

Pasek stanu (`StatusStrip`): „Krok: 12 345 · Działa” / „· Pauza”.

Wartości domyślne: 200×120, otwór 20 komórek, D = 0,20, 10 kroków na
klatkę, pędzel 2, start w pauzie.

Liczby formatowane kulturą `pl-PL` (przecinek dziesiętny, spacja tysięcy).

Skróty klawiszowe (`ProcessCmdKey`): **Spacja** — Start/Pauza, **N** —
krok, **R** — reset; opisane w podpowiedziach (`ToolTip`) przycisków.
(Strzałki odpadają, bo obsługują je suwaki.)

### 4.2 Widok (`SimulationView : Control`)

- `DoubleBuffered`, `ResizeRedraw`; bitmapa `Format32bppArgb` o rozmiarze
  siatki, przebudowywana przy zmianie symulacji, zwalniana w `Dispose`.
- `Simulation` (właściwość) i `BrushSize`.
- `RefreshField()`: `FieldRenderer.Render` → bufor `int[]` → bitmapa przez
  `LockBits` (kopiowanie wierszami z uwzględnieniem `Stride`) →
  `Invalidate()`.
- `OnPaint`: tło, bitmapa w prostokącie z `ViewMapping`,
  `InterpolationMode.NearestNeighbor`, `PixelOffsetMode.Half`.
- Mysz: `MouseDown` zapamiętuje komórkę i przycisk; `MouseMove` z wciśniętym
  przyciskiem stosuje `BrushStroke` od poprzedniej komórki do bieżącej;
  lewy = ściana, prawy = gumka; po zmianie `RefreshField()`. Działa w pauzie
  i w trakcie animacji. Gdy kursor wyjdzie poza obraz siatki, pociągnięcie
  się przerywa i zaczyna od nowa od pierwszej komórki po powrocie.

### 4.3 Pętla

- `System.Windows.Forms.Timer`, interwał 16 ms; tick: `Advance(kroki)`,
  `RefreshField()`, aktualizacja paska stanu.
- „Krok” wykonuje jeden krok (także w trakcie animacji).
- Timer zatrzymywany i zwalniany przy zamykaniu formularza.

### 4.4 Obsługa błędów

- `Program.Main`: `ApplicationConfiguration.Initialize()`,
  `Application.SetUnhandledExceptionMode(CatchException)`,
  `Application.ThreadException` i `AppDomain.CurrentDomain.UnhandledException`
  → zatrzymanie animacji (`MainForm.StopSimulation()`) i komunikat
  „Wystąpił nieoczekiwany błąd: …” (`MessageBox`, ikona błędu).
- Kontrolki UI ograniczają wartości do poprawnych zakresów; rdzeń mimo to
  waliduje wszystko.
- Bez `GC.Collect`, `goto`, czytania pikseli z ekranu i `CreateGraphics`.

## 5. Testy

TDD: każdy test powstaje przed kodem i najpierw musi nie przejść.

### 5.1 `LatticeBoltzmann.Core.Tests`

- **D2Q9:** Σwᵢ = 1; Σwᵢeᵢ = 0; Σwᵢeᵢₓ² = Σwᵢeᵢᵧ² = ⅓, Σwᵢeᵢₓeᵢᵧ = 0;
  `Opposite[Opposite[i]] = i` i eₒₚₚ = −eᵢ.
- **Równowaga:** jednorodne C = 0,7 bez ścian zostaje 0,7 (±1e-12)
  po 100 krokach.
- **Zachowanie masy:** scenariusz z otworem + dodatkowe ściany, 2000 kroków,
  |ΔM|/M < 1e-12.
- **Współczynnik dyfuzji:** siatka 121×121 bez ścian, impuls C = 1
  w komórce (60, 60); wariancja w osi x σ²(t) = Σ C·(x − x̄)² / Σ C;
  nachylenie (σ²(t₂) − σ²(t₁)) / (2·(t₂ − t₁)) dla t₁ = 50, t₂ = 150 równe D
  z tolerancją 1% dla D ∈ {0,1; 1/6; 0,3; 0,5}. (Prototyp: błąd ≤ 1e-5;
  impuls nie dociera do brzegów przed t₂.)
- **Zamknięty otwór:** `gapWidth = 0`, 1000 kroków → cała prawa komora
  ma C = 0 dokładnie.
- **Symetria:** 90×40, otwór 10 → C(x, y) = C(x, 39 − y) (±1e-12)
  po 500 krokach.
- **Zasada maksimum:** D = 0,3 (τ = 1,4) i D = 1/6 (τ = 1), 1000 kroków →
  wszystkie C w [0, 1].
- **Reguła geometrii:** ściana na płynie usuwa zawartość (spadek
  `TotalMass` o C tej komórki); wymazana ściana ma C = 0; gumka na płynie
  nic nie zmienia.
- **Walidacja:** wymiary < 3, D poza zakresem, `steps < 1`, współrzędne
  poza siatką, `SetConcentration` na ścianie / ujemne / NaN.
- **PartitionedBox:** kolumna `width/3`, położenie i szerokość otworu,
  stan początkowy, `ApplyGap` zachowuje ściany użytkownika.
- **Colormap:** 0 → `#fcfcfb`, 1 → `#0d366b`, jasność względna
  monotonicznie nierosnąca, przycinanie < 0, > 1 i NaN, alfa = 255.
- **FieldRenderer:** ściany → `WallArgb`, płyn → `Colormap`, zła długość
  bufora → wyjątek.
- **ViewMapping:** proporcje i marginesy, narożniki obrazu, punkty poza
  obrazem, widok o zerowym rozmiarze.
- **BrushStroke:** linia 4-spójna (kolejne komórki różnią się o 1 w jednej
  osi), liczba komórek dla `size` 1 i 2, przycięcie do siatki, brak
  duplikatów, walidacja `size`.
- **Szczelność narysowanej ściany:** ukośna ściana z `BrushStroke` (size 1)
  przez całą siatkę 60×60, masa po jednej stronie, 200 kroków → po drugiej
  stronie C = 0 dokładnie.

### 5.2 `LatticeBoltzmann.App.Tests` (tylko Windows)

Uruchamiane na wątku STA. Tworzą `MainForm` bez pokazywania, wykonują kilka
kroków, renderują `SimulationView` do bitmapy i sprawdzają: obraz nie jest
jednolity, pasek stanu pokazuje numer kroku, Reset zeruje licznik.

## 6. Jakość budowania i CI

- `global.json`: SDK 10.0.100, `rollForward: latestFeature`.
- `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`,
  `TreatWarningsAsErrors=true`, `AnalysisMode=Recommended`,
  `EnforceCodeStyleInBuild=true`, `LangVersion=latest`.
- `Directory.Packages.props`: centralne wersje pakietów testowych.
- `.editorconfig`: styl C#; w testach dozwolone podkreślenia w nazwach.
- `.github/workflows/ci.yml` (windows-latest, push i pull request):
  `setup-dotnet` z `global.json` → `restore` → `build -c Release` →
  `test -c Release` → `publish` App (`win-x64`, self-contained,
  single-file) → artefakt `LatticeBoltzmann-win-x64`.
- Lokalnie na Linuksie: `dotnet build -p:EnableWindowsTargeting=true`
  oraz `dotnet test tests/LatticeBoltzmann.Core.Tests`.

## 7. Dokumentacja

`README.md` po polsku: co robi program, krótki opis metody (wzory z 3.2),
sterowanie i skróty, wymagania (.NET 10 Desktop Runtime lub wersja
self-contained), budowanie, testy, wydanie, znane ograniczenia
(przestrzały dla τ < 1, rysowanie ściany usuwa zawartość komórki).

## 8. Poza zakresem

Statystyki i wykresy na żywo, adwekcja / przepływ płynu, obliczenia
równoległe, zapis i odczyt stanu, eksport obrazów, lokalizacja inna niż
polska.
