# Cyber Typing Defender

**Cyber Typing Defender** to zręcznościowa gra okienkowa WPF napisana w języku C# (.NET 8.0) i połączona z bazą danych MongoDB. Zadaniem gracza jest obrona systemu przed spadającymi słowami-zagrożeniami poprzez ich szybkie i bezbłędne wpisywanie na klawiaturze.

Projekt charakteryzuje się unikalną **stylistyką terminala cyberpunk** (ciemny motyw, monospacowane czcionki, neonowe akcenty kolorystyczne) i oferuje zaawansowany system statystyk, rankingów oraz historii rozgrywek zintegrowany z MongoDB.

---

## Główne Funkcje Aplikacji

1. **Dynamiczna Rozgrywka (Arcade Typing):**
   * Słowa o różnej długości spadają z góry ekranu na planszę (Canvas).
   * Poprawne wpisanie słowa i naciśnięcie `Enter` lub spacji niszczy zagrożenie i dodaje punkty.
   * Gracz ma 3 życia – utrata życia następuje, gdy słowo dotrze do dolnej krawędzi ekranu.
   * Wzrost poziomu (Level) następuje wraz ze zdobywaniem punktów, co zwiększa prędkość spadania słów oraz częstotliwość ich spawnu.
   * **Wskaźniki na żywo:** Wynik (Score), Poziom (Level), Życia, Celność (%) oraz tempo pisania (KPM – Klawisze na Minutę).

2. **Dedykowane Poziomy Trudności:**
   * **ŁATWY (Easy):** Słowa 3–5 literowe, wolniejszy start, zielony neon (`#00FF41`).
   * **ŚREDNI (Medium):** Słowa 6–8 literowe, umiarkowane tempo, pomarańczowy neon (`#FFB000`).
   * **TRUDNY (Hard):** Słowa 9+ literowe (często specjalistyczne terminy techniczne), bardzo szybkie tempo, czerwony neon (`#FF3333`).

3. **Autoseeding Bazy Słów:**
   * Przy pierwszym połączeniu z bazą danych program automatycznie wykrywa brak danych i zasila kolekcję MongoDB zestawem startowym słówek z podziałem na trudność (Easy, Medium, Hard).

4. **Tabele Rankingowe (Top 100):**
   * Oddzielne, przejrzyste tabele rankingowe dla każdego z trzech poziomów trudności.
   * Prawidłowa numeracja miejsc (od 1, a nie od 0).
   * Prezentacja nazwy gracza, uzyskanego wyniku punktowego oraz tempa KPM.

5. **Interaktywne Rekordy Gracza (Records Overlay):**
   * Kliknięcie na login (nazwę) dowolnego gracza w tabeli rankingowej otwiera stylowe okienko nakładkowe prezentujące jego **rekord życiowy na każdym z trzech poziomów**.
   * Okienko wyświetla najwyższy uzyskany wynik punktowy, KPM, celność oraz dokładną datę i godzinę ustanowienia rekordu (`Ustanowiono: dd.MM.yyyy HH:mm`).
   * Wyszukiwanie rekordów odbywa się w bazie danych **case-insensitively** (bez względu na wielkość liter). Dzięki temu wyniki gracza, który rejestrował się np. jako `Gabriel` oraz `gabriel`, są poprawnie agregowane i prezentowane razem.

6. **Globalna Historia Gier:**
   * Dostępny z poziomu paska stanu panel `[HISTORIA]` wyświetlający 20 ostatnich rozegranych gier wszystkich użytkowników (data, gracz, poziom, wynik, KPM, celność).

7. **Zabezpieczenie przed Utratą Danych (Robust Saving):**
   * Zamknięcie okna podczas rozgrywki (zdarzenie `Closing`) lub powrót do menu głównego za pomocą przycisku pauzy automatycznie kończy grę i zapisuje aktualnie uzyskany wynik w MongoDB.
   * Zamknięcie aplikacji w trakcie trwania asynchronicznego zapisu blokuje wyłączenie procesu do czasu bezpiecznego zakończenia zapisu danych do bazy.

---

## Architektura i Technologie

Projekt został zbudowany zgodnie ze wzorcem **MVVM (Model-View-ViewModel)**:

* **Platforma**: .NET 8.0-windows (WPF)
* **Baza danych**: MongoDB (sterownik `MongoDB.Driver` 3.9.0)
* **Logowanie/Konfiguracja**: Plik `config.json` w katalogu `%localappdata%\TypingDefender\`.
* **Struktura katalogów**:
  * [Models/](file:///d:/projektaplikacjamongo/Models/) — Definicje encji MongoDB i struktur pomocniczych (`Word.cs`, `GameSession.cs`, `AppSettings.cs`, `FallingWord.cs`).
  * [ViewModels/](file:///d:/projektaplikacjamongo/ViewModels/) — Logika gry i bindowanie widoku (`GameViewModel.cs`, `GameState.cs`, `IndexPlusOneConverter.cs`, `ViewModelBase.cs`, `RelayCommand.cs`).
  * [Services/](file:///d:/projektaplikacjamongo/Services/) — Zarządzanie bazą danych (`MongoService.cs`).
  * [MainWindow.xaml](file:///d:/projektaplikacjamongo/MainWindow.xaml) / [MainWindow.xaml.cs](file:///d:/projektaplikacjamongo/MainWindow.xaml.cs) — Widok interfejsu w stylu retro-cyberpunk terminala z obsługą animacji renderowania tekstu.

---

## Kolekcje w bazie MongoDB (`typing_defender_db`)

1. **`words`**:
   * `_id` (`ObjectId`)
   * `word` (`string`) – Tekst słowa do wpisywania.
   * `difficulty` (`string`) – Poziom trudności (`easy`, `medium`, `hard`).
   * `category` (`string`) – Kategoria słowa (np. `ogólne`, `techniczne`).

2. **`game_sessions`**:
   * `_id` (`ObjectId`)
   * `player_name` (`string`) – Login gracza.
   * `date` (`DateTime`) – Czas ukończenia rozgrywki (zapisywany w UTC, prezentowany w czasie lokalnym).
   * `difficulty` (`string`) – Poziom trudności sesji.
   * `score` (`int`) – Uzyskany wynik.
   * `kpm` (`double`) – Klawisze na minutę.
   * `words_destroyed` (`int`) – Słowa poprawnie wpisane.
   * `words_missed` (`int`) – Słowa, które dotarły na dół.
   * `accuracy_percent` (`double`) – Procentowa celność wpisywania.
   * `duration_seconds` (`int`) – Czas trwania gry w sekundach.

---

## Konfiguracja i Uruchomienie

### Wymagania wstępne:
1. Pakiet SDK dla .NET 8.0.
2. Działająca baza MongoDB na porcie `27017` (lokalnie lub w chmurze).

### Uruchomienie bazy MongoDB:
Jeśli masz zainstalowaną bazę MongoDB na systemie Windows, upewnij się, że usługa działa:
```powershell
Start-Service MongoDB
```

### Budowanie i uruchomienie gry:
1. Otwórz konsolę PowerShell w folderze głównym projektu (`D:\projektaplikacjamongo`).
2. Przywróć pakiety i zbuduj aplikację:
   ```powershell
   dotnet build
   ```
3. Uruchom grę:
   ```powershell
   dotnet run
   ```

### Konfiguracja Połączenia w Grze:
Domyślnie gra łączy się z adresem `mongodb://localhost:27017`. Jeżeli Twoja baza działa pod innym adresem lub w chmurze MongoDB Atlas:
1. Kliknij ikonę ustawień w prawym górnym rogu ekranu powitalnego gry.
2. Wprowadź poprawny Connection String (program automatycznie doda przedrostek `mongodb://` w przypadku jego braku) oraz nazwę bazy.
3. Kliknij **ZAPISZ**, aby zapisać konfigurację do pliku `config.json`. Stan połączenia (czerwony/zielony baner) zaktualizuje się automatycznie.
