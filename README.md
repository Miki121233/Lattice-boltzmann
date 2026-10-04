# Lattice Boltzmann — dyfuzja

Aplikacja okienkowa (Windows, WinForms, .NET 10) pokazująca na żywo dyfuzję
w pudełku podzielonym ścianą z regulowanym otworem. Obliczenia wykonuje
metoda siatkowa Boltzmanna (LBM); program jest przepisaniem od zera
wcześniejszego prototypu opartego na cząstkach (jego historia jest w gitcie).

## Co widać na ekranie

Po starcie pudełko ma ścianę działową mniej więcej w jednej trzeciej
szerokości, z otworem pośrodku. Lewa część jest wypełniona substancją
o stężeniu 1, prawa jest pusta (stężenie 0). Substancja przenika przez otwór
i stopniowo wyrównuje się po obu stronach. Stężenie jest pokazane skalą barw
od 0 do 1, a ściany są rysowane osobnym kolorem. Symulacja startuje
w pauzie.

## Model

Czysta dyfuzja (bez adwekcji) na siatce kwadratowej D2Q9, z operatorem
zderzeń BGK. W każdej komórce płynu przechowywanych jest 9 populacji fᵢ.

- stężenie: C = Σᵢ fᵢ (w ścianie 0),
- rozkład równowagowy: fᵢᵉq = wᵢ·C,
- czas relaksacji: τ = 3·D + ½, czyli D = (τ − ½)/3.

Jeden krok składa się z dwóch etapów:

1. kolizja BGK: fᵢ* = fᵢ − (fᵢ − wᵢ·C)/τ,
2. przesunięcie populacji do sąsiadów; jeśli sąsiad leży poza siatką lub jest
   ścianą, populacja wraca do tej samej komórki w kierunku przeciwnym
   (odbicie w połowie drogi).

Odbicie zachowuje masę (do błędów zaokrągleń) i daje zerowy strumień przez
ściany. Brzeg siatki zachowuje się jak ściana.

## Sterowanie

Przyciski: **Start/Pauza**, **Krok**, **Reset**.

Skróty klawiszowe: **Spacja** — Start/Pauza, **N** — jeden krok,
**R** — reset.

Suwaki i listy:

- współczynnik dyfuzji D: 0,05–0,50 (τ = 3D + 0,5),
- kroki na klatkę: 1–50,
- szerokość otworu: od 0 do połowy wysokości siatki (zmiana na żywo),
- rozdzielczość: 120×72, 200×120, 300×180, 400×240 (zmiana resetuje symulację),
- rozmiar pędzla: 1–8.

Rysowanie myszą na obrazie: **lewy przycisk** — ściana, **prawy przycisk** —
gumka. Legenda po prawej stronie pokazuje skalę „Stężenie” od 0 do 1
oraz próbkę koloru „Ściana”.

## Wymagania

- Windows,
- .NET 10 Desktop Runtime albo wersja self-contained (patrz „Wydanie”).

Do budowania potrzebny jest .NET SDK 10.0.100 lub nowszy
(wersja przypięta w `global.json`).

## Budowanie

```
dotnet build LatticeBoltzmann.slnx -c Release
```

Projekt aplikacji ma włączone `EnableWindowsTargeting`, więc kompiluje się
także na Linuksie, ale uruchomić ją można tylko na Windows.

## Testy

```
dotnet test tests/LatticeBoltzmann.Core.Tests
```

Testy rdzenia działają na każdym systemie. Testy interfejsu
(`tests/LatticeBoltzmann.App.Tests`) uruchamiają się tylko na Windows;
na Linuksie są jedynie kompilowane. Pełny zestaw (`dotnet test
LatticeBoltzmann.slnx -c Release`) uruchamia CI na `windows-latest`.

## Wydanie

```
dotnet publish src/LatticeBoltzmann.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
```

Powstaje pojedynczy plik `artifacts/win-x64/LatticeBoltzmann.App.exe`.
Workflow CI wykonuje to samo polecenie i udostępnia wynik jako artefakt
`LatticeBoltzmann-win-x64`.

## Znane ograniczenia

- Dla τ < 1 (czyli D < 1/6) na ostrych frontach mogą wystąpić drobne
  przestrzały poza zakresem [0, 1]. Masa nadal jest zachowana, a paleta
  barw przycina wartości. Jest to cecha metody; dla τ ≥ 1 stężenie
  pozostaje w przedziale wartości początkowych.
- Narysowanie ściany na komórce płynu usuwa jej zawartość (masa tej komórki
  znika); usunięcie ściany gumką tworzy komórkę o stężeniu 0.
- Ściany stykające się tylko narożnikami przepuszczałyby populacje ukośne,
  dlatego pędzel rysuje linie 4-spójne.
- Poza zakresem: adwekcja i przepływ płynu, statystyki i wykresy na żywo,
  zapis stanu, eksport obrazów.
