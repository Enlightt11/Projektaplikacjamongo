# Dziennik Progresu Siłowego 1RM (Strength Goal Tracker)

Dedykowana aplikacja okienkowa WPF napisana w języku C# (WPF, .NET 8.0) i połączona z bazą danych MongoDB. Aplikacja służy do precyzyjnego śledzenia progresu siłowego w jednym, konkretnym boju (np. wyciskaniu leżąc) z celem osiągnięcia założonej wagi maksymalnej (np. 100 kg).

Aplikacja opiera się na wyliczaniu szacowanego ciężaru maksymalnego (1RM) na podstawie każdego treningu, co pozwala na bieżąco monitorować realny wzrost siły i postęp w drodze do celu.

---

## Główne funkcje

1. **Zapis nowych treningów**: Prosty formularz z polami: data, ciężar (suwak/pole tekstowe) i liczba powtórzeń (suwak/pole tekstowe).
2. **Podgląd 1RM na żywo (Live Preview)**: Formularz automatycznie wylicza i prezentuje szacowany maks (1RM) w locie, zanim jeszcze zapiszesz trening.
3. **Pasek postępu 1RM**: Wizualny pasek postępu (od 0% do 100%) pokazujący, jak blisko jesteś swojego celu siłowego (np. 100 kg) w oparciu o Twój najlepszy historyczny wynik 1RM.
4. **Dashboard statystyk**:
   - **Rekord 1RM**: Najwyższy wyliczony maks w historii.
   - **Ostatni trening**: Wynik 1RM z ostatniej sesji.
   - **Wszystkich serii**: Łączna liczba zarejestrowanych wpisów.
5. **Historia treningów**: Lista z historią w postaci czytelnych kart zawierających datę, podniesiony ciężar, liczbę powtórzeń, wyliczone 1RM oraz przycisk do bezpiecznego usuwania wpisów.
6. **Panel ustawień (Drawer)**: Możliwość konfiguracji nazwy ćwiczenia, wagi docelowej oraz parametrów połączenia MongoDB z poziomu interfejsu aplikacji.

---

## Architektura i technologie

Aplikacja została zaprojektowana zgodnie z klasycznym wzorcem MVVM (Model-View-ViewModel):
* **Język**: C# (.NET 8.0 Windows SDK)
* **Interfejs**: WPF (XAML) ze spersonalizowanym, ciemnym motywem graficznym (Dark Mode) i efektami cieniowania (glow/drop shadows).
* **Baza danych**: MongoDB (z użyciem oficjalnego sterownika NuGet `MongoDB.Driver`).
* **Struktura katalogów**:
  - `Models/`: Modele danych ([WorkoutSession.cs](file:///d:/projektaplikacjamongo/Models/WorkoutSession.cs), [AppSettings.cs](file:///d:/projektaplikacjamongo/Models/AppSettings.cs)).
  - `ViewModels/`: Klasy logiki prezentacji ([MainViewModel.cs](file:///d:/projektaplikacjamongo/ViewModels/MainViewModel.cs), pomocnicze [ViewModelBase.cs](file:///d:/projektaplikacjamongo/ViewModels/ViewModelBase.cs) i [RelayCommand.cs](file:///d:/projektaplikacjamongo/ViewModels/RelayCommand.cs)).
  - `Services/`: Obsługa bazy danych ([MongoService.cs](file:///d:/projektaplikacjamongo/Services/MongoService.cs)).
  - `Views/`: Warstwa graficzna interfejsu ([MainWindow.xaml](file:///d:/projektaplikacjamongo/MainWindow.xaml)).

---

## Metoda wyliczania 1RM

Szacowany ciężar maksymalny (1RM - One-Rep Max) jest wyliczany przy użyciu popularnego w sporcie wzoru Epleya:

* Dla liczby powtórzeń ($r > 1$):
  $$1\text{RM} = w \times \left(1 + \frac{r}{30}\right)$$
  *Gdzie $w$ to podniesiony ciężar (kg), a $r$ to wykonana liczba powtórzeń.*
* Dla 1 powtórzenia ($r = 1$):
  $$1\text{RM} = w$$

Aplikacja zaokrągla wyniki do jednego miejsca po przecinku (np. `93.3 kg`).

---

## Konfiguracja połączenia z MongoDB

Aplikacja domyślnie próbuje łączyć się z lokalną bazą danych pod adresem:
`mongodb://127.0.0.1:27017`

### Tolerancja i walidacja wprowadzanych danych:
* Program automatycznie zabezpiecza użytkownika przed błędami wpisu w panelu konfiguracji. Jeżeli podasz sam adres bez protokołu (np. `127.0.0.1:27017` lub `localhost:27017`), aplikacja samodzielnie dopisze na początku wymagany przedrostek `mongodb://`, zapobiegając awarii sterownika.
* Limit czasu na połączenie z bazą (timeout) wynosi 3 sekundy. Jeśli baza jest wyłączona, aplikacja nie zawiesi się – wyświetli u góry czytelny, czerwony baner z ostrzeżeniem, zablokuje przyciski zapisu i pozwoli na swobodne wejście do ustawień w celu zmiany konfiguracji.

### Plik konfiguracyjny (AppSettings):
Wszelkie parametry ustawień aplikacji (waga docelowa, nazwa ćwiczenia, URI połączenia do MongoDB) są trwale zapisywane na komputerze w pliku:
`%localappdata%\StrengthTracker\config.json`

---

## Jak uruchomić projekt?

### Wymagania:
1. .NET 8.0 SDK (zainstalowane na komputerze).
2. Działająca baza MongoDB (domyślnie na porcie `27017`).

### Uruchomienie lokalnej bazy danych (Windows):
Jeśli baza MongoDB działa jako usługa systemowa, upewnij się, że jest włączona. Możesz ją uruchomić, otwierając PowerShell jako Administrator i wpisując:
```powershell
Start-Service MongoDB
```

### Kompilacja i uruchomienie programu:
Otwórz terminal w katalogu głównym projektu (`D:\projektaplikacjamongo`) i wykonaj poniższe polecenia:

1. **Przywrócenie pakietów i budowanie projektu**:
   ```powershell
   dotnet build
   ```

2. **Uruchomienie aplikacji**:
   ```powershell
   dotnet run
   ```
