using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using projektaplikacjamongo.Models;
using projektaplikacjamongo.Services;

namespace projektaplikacjamongo.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private readonly MongoService _mongoService;
        private AppSettings _settings;

        // ─── Game state ───
        private GameState _currentGameState = GameState.Menu;
        private string _playerName = "Gracz";
        private string _selectedDifficulty = "easy";

        // ─── Gameplay stats ───
        private int _score;
        private int _lives = 3;
        private int _maxLives = 3;
        private double _kpm;
        private int _wordsDestroyed;
        private int _wordsMissed;
        private int _totalKeystrokes;
        private double _gameDurationSeconds;
        private string _currentInput = string.Empty;
        private int _currentLevel = 1;

        // ─── UI State ───
        private bool? _isConnected;
        private string _statusText = "Łączenie...";
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        // ─── Settings form ───
        private string _settingConnectionString = string.Empty;
        private string _settingDatabaseName = string.Empty;
        private string _settingPlayerName = string.Empty;

        // ─── Word pool ───
        private List<string> _wordPool = new();

        // ─── Rankings ───
        private string _selectedRankingTab = "easy";

        // ─── Player records ───
        private string _selectedRankingPlayer = string.Empty;
        private GameSession? _easyRecord;
        private GameSession? _mediumRecord;
        private GameSession? _hardRecord;
        private Task? _saveTask;

        public Task? SaveTask => _saveTask;

        public GameViewModel()
        {
            _settings = AppSettings.Load();
            _mongoService = new MongoService(_settings);

            PlayerName = _settings.PlayerName;
            ResetSettingsForm();

            TopScoresEasy = new ObservableCollection<GameSession>();
            TopScoresMedium = new ObservableCollection<GameSession>();
            TopScoresHard = new ObservableCollection<GameSession>();
            RecentGames = new ObservableCollection<GameSession>();

            // Commands
            StartGameCommand = new RelayCommand(async (param) => await PrepareAndStartGame(param as string ?? "easy"));
            BackToMenuCommand = new RelayCommand(async () => 
            { 
                CurrentGameState = GameState.Menu; 
                if (SaveTask != null)
                {
                    try { await SaveTask; } catch { }
                }
                await LoadMenuDataAsync(); 
            });
            OpenSettingsCommand = new RelayCommand(() => CurrentGameState = GameState.Settings);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
            CloseErrorCommand = new RelayCommand(() => ErrorMessage = string.Empty);
            BackFromSettingsCommand = new RelayCommand(() => { ResetSettingsForm(); CurrentGameState = GameState.Menu; });
            OpenHistoryCommand = new RelayCommand(async () => { await LoadHistoryDataAsync(); CurrentGameState = GameState.History; });
            BackFromHistoryCommand = new RelayCommand(() => CurrentGameState = GameState.Menu);
            SwitchRankingTabCommand = new RelayCommand((param) => SwitchRankingTab(param as string ?? "easy"));
            ShowPlayerRecordsCommand = new RelayCommand(async (param) => await LoadPlayerRecordsAsync(param as string));
            BackFromStatsCommand = new RelayCommand(() => CurrentGameState = GameState.Menu);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await TestConnectionAsync();
            if (IsConnected == true)
            {
                await _mongoService.SeedWordsAsync();
                await LoadMenuDataAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  PROPERTIES
        // ═══════════════════════════════════════════════════════════════

        public GameState CurrentGameState
        {
            get => _currentGameState;
            set => SetProperty(ref _currentGameState, value);
        }

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public string SelectedDifficulty
        {
            get => _selectedDifficulty;
            set => SetProperty(ref _selectedDifficulty, value);
        }

        public int Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }

        public int Lives
        {
            get => _lives;
            set => SetProperty(ref _lives, value);
        }

        public int MaxLives
        {
            get => _maxLives;
            set => SetProperty(ref _maxLives, value);
        }

        public double Kpm
        {
            get => _kpm;
            set => SetProperty(ref _kpm, Math.Round(value, 1));
        }

        public int WordsDestroyed
        {
            get => _wordsDestroyed;
            set => SetProperty(ref _wordsDestroyed, value);
        }

        public int WordsMissed
        {
            get => _wordsMissed;
            set => SetProperty(ref _wordsMissed, value);
        }

        public int TotalKeystrokes
        {
            get => _totalKeystrokes;
            set
            {
                if (SetProperty(ref _totalKeystrokes, value))
                    UpdateKpm();
            }
        }

        public double GameDurationSeconds
        {
            get => _gameDurationSeconds;
            set
            {
                if (SetProperty(ref _gameDurationSeconds, value))
                {
                    UpdateKpm();
                    OnPropertyChanged(nameof(GameDurationFormatted));
                }
            }
        }

        public string GameDurationFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(_gameDurationSeconds);
                return ts.ToString(@"mm\:ss");
            }
        }

        public string CurrentInput
        {
            get => _currentInput;
            set => SetProperty(ref _currentInput, value);
        }

        public int CurrentLevel
        {
            get => _currentLevel;
            set => SetProperty(ref _currentLevel, value);
        }

        public double AccuracyPercent
        {
            get
            {
                int total = WordsDestroyed + WordsMissed;
                if (total == 0) return 100.0;
                return Math.Round((double)WordsDestroyed / total * 100.0, 1);
            }
        }

        // ─── UI State ───

        public bool? IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ─── Settings form ───

        public string SettingConnectionString
        {
            get => _settingConnectionString;
            set => SetProperty(ref _settingConnectionString, value);
        }

        public string SettingDatabaseName
        {
            get => _settingDatabaseName;
            set => SetProperty(ref _settingDatabaseName, value);
        }

        public string SettingPlayerName
        {
            get => _settingPlayerName;
            set => SetProperty(ref _settingPlayerName, value);
        }

        // ─── Ranking tab ───

        public string SelectedRankingTab
        {
            get => _selectedRankingTab;
            set => SetProperty(ref _selectedRankingTab, value);
        }

        // ─── Player records ───

        public string SelectedRankingPlayer
        {
            get => _selectedRankingPlayer;
            set => SetProperty(ref _selectedRankingPlayer, value);
        }

        public GameSession? EasyRecord
        {
            get => _easyRecord;
            set => SetProperty(ref _easyRecord, value);
        }

        public GameSession? MediumRecord
        {
            get => _mediumRecord;
            set => SetProperty(ref _mediumRecord, value);
        }

        public GameSession? HardRecord
        {
            get => _hardRecord;
            set => SetProperty(ref _hardRecord, value);
        }

        // ─── Collections ───

        public ObservableCollection<GameSession> TopScoresEasy { get; }
        public ObservableCollection<GameSession> TopScoresMedium { get; }
        public ObservableCollection<GameSession> TopScoresHard { get; }
        public ObservableCollection<GameSession> RecentGames { get; }

        // ─── Commands ───

        public RelayCommand StartGameCommand { get; }
        public RelayCommand BackToMenuCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand TestConnectionCommand { get; }
        public RelayCommand CloseErrorCommand { get; }
        public RelayCommand BackFromSettingsCommand { get; }
        public RelayCommand OpenHistoryCommand { get; }
        public RelayCommand BackFromHistoryCommand { get; }
        public RelayCommand SwitchRankingTabCommand { get; }
        public RelayCommand ShowPlayerRecordsCommand { get; }
        public RelayCommand BackFromStatsCommand { get; }

        // ─── Events for code-behind ───

        public event Action? GameStarted;
        public event Action? GameEnded;

        // ═══════════════════════════════════════════════════════════════
        //  WORD POOL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the word pool prepared for the current game.
        /// </summary>
        public List<string> GetWordPool() => _wordPool;

        // ═══════════════════════════════════════════════════════════════
        //  GAME FLOW
        // ═══════════════════════════════════════════════════════════════

        private async Task PrepareAndStartGame(string difficulty)
        {
            if (IsConnected != true)
            {
                ErrorMessage = "Brak połączenia z bazą danych!";
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                ErrorMessage = "Wpisz swoją nazwę gracza!";
                return;
            }

            // Save player name
            _settings.PlayerName = PlayerName.Trim();
            _settings.Save();

            // Await save task of previous game if still running
            if (SaveTask != null)
            {
                try { await SaveTask; } catch { }
            }

            SelectedDifficulty = difficulty;
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Load words for chosen difficulty
                var words = await _mongoService.GetWordsByDifficultyAsync(difficulty);

                _wordPool = words.Select(w => w.Text.ToLowerInvariant()).ToList();

                if (_wordPool.Count == 0)
                {
                    ErrorMessage = "Brak słów w bazie dla tego poziomu trudności. Sprawdź połączenie z bazą.";
                    return;
                }

                // Reset stats
                Score = 0;
                MaxLives = 3;
                Lives = MaxLives;
                WordsDestroyed = 0;
                WordsMissed = 0;
                TotalKeystrokes = 0;
                GameDurationSeconds = 0;
                Kpm = 0;
                CurrentInput = string.Empty;
                CurrentLevel = 1;

                CurrentGameState = GameState.Playing;
                GameStarted?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Błąd ładowania gry: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Called by code-behind when a word is successfully destroyed.
        /// </summary>
        public void OnWordDestroyed(string word)
        {
            WordsDestroyed++;
            Score += word.Length;
            OnPropertyChanged(nameof(AccuracyPercent));
        }

        /// <summary>
        /// Called by code-behind when a word reaches the bottom.
        /// </summary>
        public void OnWordMissed(string word)
        {
            WordsMissed++;
            Lives--;
            OnPropertyChanged(nameof(AccuracyPercent));

            if (Lives <= 0)
            {
                Lives = 0;
                _ = EndGameAsync();
            }
        }

        public Task EndGameAsync()
        {
            GameEnded?.Invoke();
            ErrorMessage = string.Empty;
            CurrentGameState = GameState.GameOver;

            var session = new GameSession
            {
                PlayerName = PlayerName,
                Date = DateTime.UtcNow,
                Difficulty = SelectedDifficulty,
                Score = Score,
                Kpm = Kpm,
                WordsDestroyed = WordsDestroyed,
                WordsMissed = WordsMissed,
                AccuracyPercent = AccuracyPercent,
                DurationSeconds = (int)GameDurationSeconds
            };

            _saveTask = Task.Run(async () =>
            {
                try
                {
                    await _mongoService.SaveGameSessionAsync(session);
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ErrorMessage = "Wynik zapisany pomyślnie!";
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ErrorMessage = $"Błąd zapisu w bazie danych: {ex.Message}";
                    });
                }
            });

            return _saveTask;
        }

        // ═══════════════════════════════════════════════════════════════
        //  MENU DATA
        // ═══════════════════════════════════════════════════════════════

        public async Task LoadMenuDataAsync()
        {
            try
            {
                var easyScores = await _mongoService.GetTopScoresByDifficultyAsync("easy", 10);
                var mediumScores = await _mongoService.GetTopScoresByDifficultyAsync("medium", 10);
                var hardScores = await _mongoService.GetTopScoresByDifficultyAsync("hard", 10);

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    TopScoresEasy.Clear();
                    foreach (var s in easyScores) TopScoresEasy.Add(s);

                    TopScoresMedium.Clear();
                    foreach (var s in mediumScores) TopScoresMedium.Add(s);

                    TopScoresHard.Clear();
                    foreach (var s in hardScores) TopScoresHard.Add(s);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMenuData error: {ex.Message}");
            }
        }

        private void SwitchRankingTab(string tab)
        {
            SelectedRankingTab = tab;
        }

        public async Task LoadHistoryDataAsync()
        {
            try
            {
                var recent = await _mongoService.GetRecentGamesAsync(20);
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentGames.Clear();
                    foreach (var g in recent) RecentGames.Add(g);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }

        public async Task LoadPlayerRecordsAsync(string? playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return;

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedRankingPlayer = playerName;
                EasyRecord = null;
                MediumRecord = null;
                HardRecord = null;
                CurrentGameState = GameState.Stats; // Switch to records screen
            });

            try
            {
                var easyTask = _mongoService.GetPlayerRecordAsync(playerName, "easy");
                var mediumTask = _mongoService.GetPlayerRecordAsync(playerName, "medium");
                var hardTask = _mongoService.GetPlayerRecordAsync(playerName, "hard");

                await Task.WhenAll(easyTask, mediumTask, hardTask);

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    EasyRecord = easyTask.Result;
                    MediumRecord = mediumTask.Result;
                    HardRecord = hardTask.Result;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPlayerRecords error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  KPM
        // ═══════════════════════════════════════════════════════════════

        private void UpdateKpm()
        {
            if (_gameDurationSeconds > 0)
            {
                Kpm = (_totalKeystrokes / _gameDurationSeconds) * 60.0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CONNECTION & SETTINGS
        // ═══════════════════════════════════════════════════════════════

        public async Task TestConnectionAsync()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsConnected = null;
                StatusText = "Łączenie...";
                ErrorMessage = string.Empty;
            });

            bool active = await _mongoService.TestConnectionAsync();

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsConnected = active;
                StatusText = active ? "Połączono" : "Brak połączenia";

                if (!active)
                {
                    ErrorMessage = "Nie można połączyć się z MongoDB. Sprawdź ustawienia.";
                }
            });
        }

        private void ResetSettingsForm()
        {
            SettingConnectionString = _settings.ConnectionString;
            SettingDatabaseName = _settings.DatabaseName;
            SettingPlayerName = _settings.PlayerName;
        }

        public async Task SaveSettingsAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SettingConnectionString))
            {
                ErrorMessage = "Adres URI bazy danych nie może być pusty.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SettingDatabaseName))
            {
                ErrorMessage = "Nazwa bazy danych nie może być pusta.";
                return;
            }

            IsLoading = true;
            try
            {
                _settings.ConnectionString = SettingConnectionString.Trim();
                _settings.DatabaseName = SettingDatabaseName.Trim();
                _settings.PlayerName = (SettingPlayerName ?? "Gracz").Trim();
                _settings.Save();

                PlayerName = _settings.PlayerName;
                _mongoService.UpdateSettings(_settings);

                await TestConnectionAsync();
                if (IsConnected == true)
                {
                    await _mongoService.SeedWordsAsync();
                    await LoadMenuDataAsync();
                }

                CurrentGameState = GameState.Menu;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Błąd zapisu ustawień: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
