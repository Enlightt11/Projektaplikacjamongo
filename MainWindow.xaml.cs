using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using projektaplikacjamongo.Models;
using projektaplikacjamongo.ViewModels;

namespace projektaplikacjamongo
{
    public partial class MainWindow : Window
    {
        private GameViewModel _viewModel = null!;

        // ─── Game timers ───
        private DispatcherTimer _gameTimer = null!;
        private DispatcherTimer _spawnTimer = null!;
        private DispatcherTimer _durationTimer = null!;

        // ─── Active words on canvas ───
        private readonly List<FallingWord> _activeWords = new();
        private readonly Random _random = new();

        // ─── Game config ───
        private double _baseSpeed;
        private double _spawnIntervalMs;
        private int _wordsSpawned;
        private DateTime _gameStartTime;
        private int _currentLevel = 1;

        // ─── Pause tracking ───
        private DateTime _pauseStartTime;
        private double _totalPausedSeconds;

        public MainWindow()
        {
            InitializeComponent();
            Closing += Window_Closing;

            Loaded += (s, e) =>
            {
                _viewModel = (GameViewModel)DataContext;
                _viewModel.GameStarted += OnGameStarted;
                _viewModel.GameEnded += OnGameEnded;

                _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
                _gameTimer.Tick += GameTick;

                _spawnTimer = new DispatcherTimer();
                _spawnTimer.Tick += SpawnTick;

                _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _durationTimer.Tick += DurationTick;

                UpdateLivesDisplay(_viewModel.Lives);
            };
        }

        // ═══════════════════════════════════════════════════════════════
        //  GAME START / STOP / PAUSE
        // ═══════════════════════════════════════════════════════════════

        private void OnGameStarted()
        {
            GameCanvas.Children.Clear();
            _activeWords.Clear();
            _wordsSpawned = 0;
            _totalPausedSeconds = 0;
            _currentLevel = 1;

            switch (_viewModel.SelectedDifficulty)
            {
                case "easy":
                    _baseSpeed = 1.2;
                    _spawnIntervalMs = 2800;
                    break;
                case "medium":
                    _baseSpeed = 1.8;
                    _spawnIntervalMs = 2200;
                    break;
                case "hard":
                    _baseSpeed = 2.5;
                    _spawnIntervalMs = 1600;
                    break;
                default:
                    _baseSpeed = 1.2;
                    _spawnIntervalMs = 2800;
                    break;
            }

            _gameStartTime = DateTime.Now;
            UpdateLivesDisplay(_viewModel.Lives);

            _spawnTimer.Interval = TimeSpan.FromMilliseconds(_spawnIntervalMs * 0.80);
            _gameTimer.Start();
            _spawnTimer.Start();
            _durationTimer.Start();

            InputTextBox.Text = "";
            InputTextBox.Focus();

            SpawnWord();
        }

        private void OnGameEnded()
        {
            _gameTimer.Stop();
            _spawnTimer.Stop();
            _durationTimer.Stop();
        }

        private void PauseGame()
        {
            if (_viewModel.CurrentGameState != GameState.Playing) return;
            _pauseStartTime = DateTime.Now;
            _gameTimer.Stop();
            _spawnTimer.Stop();
            _durationTimer.Stop();
            _viewModel.CurrentGameState = GameState.Paused;
        }

        private void ResumeGame()
        {
            if (_viewModel.CurrentGameState != GameState.Paused) return;
            _totalPausedSeconds += (DateTime.Now - _pauseStartTime).TotalSeconds;
            _viewModel.CurrentGameState = GameState.Playing;
            _gameTimer.Start();
            _spawnTimer.Start();
            _durationTimer.Start();
            InputTextBox.Focus();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e) => PauseGame();
        private void ResumeGame_Click(object sender, RoutedEventArgs e) => ResumeGame();

        private async void ExitToMenu_Click(object sender, RoutedEventArgs e)
        {
            OnGameEnded();
            GameCanvas.Children.Clear();
            _activeWords.Clear();
            await _viewModel.EndGameAsync();
        }

        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel == null) return;

            bool isMidGame = (_viewModel.CurrentGameState == GameState.Playing || _viewModel.CurrentGameState == GameState.Paused);
            var saveTask = _viewModel.SaveTask;
            bool isSaving = (saveTask != null && !saveTask.IsCompleted);

            if (isMidGame || isSaving)
            {
                e.Cancel = true;
                this.Hide();

                if (isMidGame)
                {
                    OnGameEnded();
                    await _viewModel.EndGameAsync();
                }
                else if (isSaving && saveTask != null)
                {
                    try { await saveTask; } catch { }
                }

                _viewModel.CurrentGameState = GameState.GameOver;
                this.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  LEVEL / ACCELERATION SYSTEM
        // ═══════════════════════════════════════════════════════════════

        // Thresholds: every 5 words destroyed = new level, capped at 10
        // Speed multiplier: 1.5 + (level-1) * 0.20  → L1=1.5, L5=2.3, L10=3.3
        // Spawn multiplier: max(0.25, 0.80 - (level-1) * 0.08)  → L1=0.80, L2=0.72, L8+=0.25

        private void CheckLevelUp()
        {
            int destroyed = _viewModel.WordsDestroyed;
            int newLevel = Math.Min(10, 1 + destroyed / 5);

            if (newLevel != _currentLevel)
            {
                _currentLevel = newLevel;
                _viewModel.CurrentLevel = newLevel;

                // Apply new spawn interval
                double spawnMult = Math.Max(0.25, 0.80 - (_currentLevel - 1) * 0.08);
                _spawnTimer.Interval = TimeSpan.FromMilliseconds(
                    Math.Max(400, _spawnIntervalMs * spawnMult));
            }
        }

        private double GetSpeedMultiplier()
        {
            return 1.5 + (_currentLevel - 1) * 0.20;
        }

        // ═══════════════════════════════════════════════════════════════
        //  GAME TICK
        // ═══════════════════════════════════════════════════════════════

        private void GameTick(object? sender, EventArgs e)
        {
            double canvasHeight = GameCanvas.ActualHeight;
            if (canvasHeight <= 0) return;

            var wordsToRemove = new List<FallingWord>();

            foreach (var word in _activeWords)
            {
                if (word.IsDestroyed) continue;

                double currentTop = Canvas.GetTop(word.UIElement);
                double newTop = currentTop + word.Speed;
                Canvas.SetTop(word.UIElement, newTop);

                if (newTop + word.UIElement.ActualHeight >= canvasHeight)
                {
                    wordsToRemove.Add(word);
                }
            }

            foreach (var word in wordsToRemove)
            {
                _viewModel.OnWordMissed(word.Text);
                UpdateLivesDisplay(_viewModel.Lives);
                AnimateMiss(word);
                _activeWords.Remove(word);

                if (_viewModel.Lives <= 0) return;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SPAWN
        // ═══════════════════════════════════════════════════════════════

        private void SpawnTick(object? sender, EventArgs e) => SpawnWord();

        private void SpawnWord()
        {
            var wordPool = _viewModel.GetWordPool();
            if (wordPool.Count == 0) return;

            string wordText = wordPool[_random.Next(wordPool.Count)];

            int attempts = 0;
            while (_activeWords.Any(w => w.Text == wordText) && attempts < 10)
            {
                wordText = wordPool[_random.Next(wordPool.Count)];
                attempts++;
            }

            // Terminal-style colors based on difficulty
            Color borderColor = _viewModel.SelectedDifficulty switch
            {
                "easy" => (Color)ColorConverter.ConvertFromString("#00FF41")!,
                "medium" => (Color)ColorConverter.ConvertFromString("#FFB000")!,
                "hard" => (Color)ColorConverter.ConvertFromString("#FF3333")!,
                _ => (Color)ColorConverter.ConvertFromString("#00FF41")!,
            };

            var textBlock = new TextBlock
            {
                Text = wordText,
                Foreground = new SolidColorBrush(borderColor),
                FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 13, 13, 13)),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(10, 5, 10, 5),
                Child = textBlock,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double wordWidth = border.DesiredSize.Width;
            double canvasWidth = GameCanvas.ActualWidth;
            if (canvasWidth <= 0) canvasWidth = 1000;

            double x = _random.Next(20, Math.Max(21, (int)(canvasWidth - wordWidth - 20)));

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, -40);
            GameCanvas.Children.Add(border);

            double speedMult = GetSpeedMultiplier();

            var fallingWord = new FallingWord
            {
                Text = wordText,
                UIElement = border,
                Speed = _baseSpeed * speedMult * (0.85 + _random.NextDouble() * 0.30),
                IsDestroyed = false
            };

            _activeWords.Add(fallingWord);
            _wordsSpawned++;
        }

        // ═══════════════════════════════════════════════════════════════
        //  INPUT
        // ═══════════════════════════════════════════════════════════════

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = InputTextBox.Text.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(input)) return;

                var match = _activeWords.FirstOrDefault(w => !w.IsDestroyed && w.Text.ToLowerInvariant() == input);

                if (match != null)
                {
                    match.IsDestroyed = true;
                    _viewModel.OnWordDestroyed(match.Text);
                    AnimateDestroy(match);
                    _activeWords.Remove(match);

                    // Check if level should increase
                    CheckLevelUp();
                }
                else
                {
                    AnimateInputError();
                }

                InputTextBox.Text = "";
                e.Handled = true;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel.CurrentGameState == GameState.Playing && InputTextBox.Text.Length > 0)
            {
                _viewModel.TotalKeystrokes++;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_viewModel.CurrentGameState == GameState.Playing)
                {
                    PauseGame();
                    e.Handled = true;
                    return;
                }
                else if (_viewModel.CurrentGameState == GameState.Paused)
                {
                    ResumeGame();
                    e.Handled = true;
                    return;
                }
            }

            if (_viewModel.CurrentGameState == GameState.Playing && !InputTextBox.IsFocused)
            {
                InputTextBox.Focus();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  TIME
        // ═══════════════════════════════════════════════════════════════

        private TimeSpan GetEffectiveElapsedTime()
        {
            return (DateTime.Now - _gameStartTime) - TimeSpan.FromSeconds(_totalPausedSeconds);
        }

        private void DurationTick(object? sender, EventArgs e)
        {
            if (_viewModel.CurrentGameState == GameState.Playing)
            {
                _viewModel.GameDurationSeconds = GetEffectiveElapsedTime().TotalSeconds;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ANIMATIONS
        // ═══════════════════════════════════════════════════════════════

        private void AnimateDestroy(FallingWord word)
        {
            var border = word.UIElement;
            var transform = (ScaleTransform)border.RenderTransform;

            var scaleX = new DoubleAnimation(1.0, 1.4, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleY = new DoubleAnimation(1.0, 1.4, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(250));

            border.BorderBrush = new SolidColorBrush(Colors.LimeGreen);
            if (border.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush(Colors.LimeGreen);

            fadeOut.Completed += (s, e) =>
            {
                GameCanvas.Children.Remove(border);
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            border.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateMiss(FallingWord word)
        {
            var border = word.UIElement;

            border.BorderBrush = new SolidColorBrush(Colors.Red);
            if (border.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush(Colors.Red);

            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(350));
            fadeOut.Completed += (s, e) =>
            {
                GameCanvas.Children.Remove(border);
            };
            border.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateInputError()
        {
            InputTextBox.BorderBrush = new SolidColorBrush(Colors.Red);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (s, e) =>
            {
                InputTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")!);
                timer.Stop();
            };
            timer.Start();
        }

        // ═══════════════════════════════════════════════════════════════
        //  LIVES
        // ═══════════════════════════════════════════════════════════════

        private void UpdateLivesDisplay(int lives)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            for (int i = lives; i < _viewModel.MaxLives; i++) hearts += "· ";
            LivesDisplay.Text = hearts.Trim();
        }
    }
}