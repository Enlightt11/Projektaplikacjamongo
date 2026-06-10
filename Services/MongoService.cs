using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using projektaplikacjamongo.Models;

namespace projektaplikacjamongo.Services
{
    public class MongoService
    {
        private IMongoDatabase? _database;
        private IMongoCollection<Word>? _wordsCollection;
        private IMongoCollection<GameSession>? _sessionsCollection;
        private AppSettings _settings;

        public MongoService(AppSettings settings)
        {
            _settings = settings;
            InitializeClient();
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
                {
                    _database = null;
                    _wordsCollection = null;
                    _sessionsCollection = null;
                    return;
                }

                string connStr = _settings.ConnectionString.Trim();
                if (!connStr.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) &&
                    !connStr.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
                {
                    connStr = "mongodb://" + connStr;
                }

                var clientSettings = MongoClientSettings.FromConnectionString(connStr);
                clientSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
                clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

                var client = new MongoClient(clientSettings);
                _database = client.GetDatabase(_settings.DatabaseName);
                _wordsCollection = _database.GetCollection<Word>("words");
                _sessionsCollection = _database.GetCollection<GameSession>("game_sessions");
            }
            catch
            {
                _database = null;
                _wordsCollection = null;
                _sessionsCollection = null;
            }
        }

        /// <summary>
        /// Pings the database to check if the connection is active.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            if (_database == null) return false;
            try
            {
                var pingTask = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                if (await Task.WhenAny(pingTask, Task.Delay(3000)) == pingTask)
                {
                    await pingTask;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // ─── WORDS ──────────────────────────────────────────────────────

        /// <summary>
        /// Gets words filtered by difficulty.
        /// </summary>
        public async Task<List<Word>> GetWordsByDifficultyAsync(string difficulty)
        {
            if (_wordsCollection == null) return new List<Word>();
            try
            {
                var filter = Builders<Word>.Filter.Eq(w => w.Difficulty, difficulty);
                return await _wordsCollection.Find(filter).ToListAsync();
            }
            catch
            {
                return new List<Word>();
            }
        }

        /// <summary>
        /// Seeds the words collection with default Polish words if it's empty.
        /// </summary>
        public async Task SeedWordsAsync()
        {
            if (_wordsCollection == null) return;
            try
            {
                long count = await _wordsCollection.CountDocumentsAsync(FilterDefinition<Word>.Empty);
                if (count > 0) return;

                var words = new List<Word>();

                // ── EASY (3–5 liter) ──
                string[] easy = {
                    "kot", "dom", "pies", "las", "mysz", "woda", "kawa", "lato", "noc", "lis",
                    "fala", "gra", "dym", "sok", "nos", "ryba", "okno", "klucz", "chleb", "morze",
                    "serce", "dusza", "piwo", "dach", "mapa", "most", "skok", "port", "bieg", "krok",
                    "pole", "noga", "szum", "czas", "tort", "zamek", "kubek", "obraz", "sklep", "metal",
                    "biuro", "buty", "droga", "lampa", "ekran", "szafa", "fotel", "napis", "film", "mech",
                    "smok", "król", "drzewo", "góra", "rzeka", "burza", "deszcz", "śnieg", "wiatr", "słońce",
                    "ogień", "ziemia", "kwiat", "liść", "trawa", "kamień", "piasek", "gwiazda", "chmura", "tęcza",
                    "miód", "mleko", "masło", "jajko", "cebula", "sól", "pieprz", "mąka", "ryż", "ser",
                    "książka", "pióro", "stół", "krzesło", "drzwi", "okno", "ściana", "sufit", "dywan", "łóżko",
                    "radio", "zegar", "lustro", "klej", "guzik", "igła", "nóż", "widelec", "łyżka", "talerz",
                    "kubek", "miska", "garnek", "patelnia", "młotek", "gwóźdź", "śruba", "drut", "lina", "worek",
                    "torba", "plecak", "czapka", "szalik", "rękawiczka", "skarpeta", "pasek", "zegarek", "okulary", "klucz",
                    "moneta", "banknot", "karta", "paszport", "bilet", "znaczek", "koperta", "list", "gazeta", "plakat"
                };

                // ── MEDIUM (6–8 liter) ──
                string[] medium = {
                    "muzyka", "koszyk", "balkon", "zagadka", "samolot", "kwiatek", "kompas", "student",
                    "program", "monitor", "kuchnia", "problem", "planeta", "gitara", "melodia", "kolacja",
                    "zabawka", "koszula", "kapitan", "silnik", "energia", "fabryka", "marzenie", "bohater",
                    "kapusta", "forteca", "serwer", "poduszka", "herbata", "tramwaj", "makaron", "rakieta",
                    "ekspres", "krawiec", "katedra", "apteczka", "kanapka", "jubiler", "parasol", "atrament",
                    "klamka", "zasłona", "podłoga", "schody", "winda", "garaż", "kominek", "ogrzewanie",
                    "malina", "wiśnia", "jabłko", "gruszka", "śliwka", "truskawka", "borówka", "arbuz",
                    "marchew", "pomidor", "ogórek", "papryka", "sałata", "szpinak", "brokuły", "kalafior",
                    "pietruszka", "seler", "dynia", "cukinia", "bakłażan", "rzodkiew", "burak", "chrzan",
                    "czekolada", "ciastko", "herbatnik", "budyń", "galaretka", "lizak", "karmel", "wafle",
                    "lokomotywa", "autobus", "rower", "motorówka", "żaglówka", "helikopter", "ciężarówka", "ambulans",
                    "biblioteka", "muzeum", "teatr", "stadion", "basen", "siłownia", "szpital", "apteka",
                    "poczta", "ratusz", "komisariat", "strażak", "żołnierz", "marynarz", "pilot", "mechanik",
                    "fryzjer", "piekarnia", "cukiernia", "księgarnia", "kwiaciarnia", "jubiler", "optyk", "dentysta",
                    "recepta", "operacja", "diagnoza", "terapia", "konsultacja", "chirurg", "pediatra", "ortopeda",
                    "fotograf", "muzyk", "artysta", "malarz", "pisarz", "reżyser", "aktor", "tancerz"
                };

                // ── HARD (9+ liter) ──
                string[] hard = {
                    "programowanie", "bezpieczeństwo", "odpowiedzialność", "współpraca", "klawiatura",
                    "zaangażowanie", "przedsiębiorca", "przedstawienie", "komunikacja", "niezawodność",
                    "oprogramowanie", "rzeczywistość", "zastosowanie", "przetwarzanie", "automatyzacja",
                    "sprawiedliwość", "doświadczenie", "charakterystyka", "infrastruktura", "transformacja",
                    "przygotowanie", "profesjonalista", "doskonałość", "podsumowanie", "korespondencja",
                    "specjalizacja", "kontynuowanie", "laboratorium", "skomplikowany", "zdeterminowany",
                    "zaproponowanie", "fantastycznie", "restrukturyzacja", "przystosowanie", "prawdopodobieństwo",
                    "uniwersytet", "kompensacja", "administracja", "eksperyment", "funkcjonalność",
                    "kwestionariusz", "hospitalizacja", "temperatura", "entuzjastyczny", "gospodarstwo",
                    "niepowtarzalny", "humanistyczny", "okoliczność", "wielokrotność", "unowocześnienie",
                    "przedsiębiorstwo", "zabezpieczenie", "zainteresowanie", "przyspieszenie", "przynależność",
                    "niepodległość", "zadowolenie", "wyobraźnia", "porozumienie", "dofinansowanie",
                    "przeciwieństwo", "zatwierdzenie", "uwierzytelnienie", "zrównoważony", "wielopłaszczyznowy",
                    "wynagrodzenie", "stowarzyszenie", "międzynarodowy", "wykorzystywanie", "przetransferować",
                    "skategoryzować", "przetestować", "zaimplementować", "zidentyfikować", "zaktualizować",
                    "przeanalizować", "zsynchronizować", "przeorganizować", "zaprzyjaźniony", "ponadprzeciętny",
                    "natychmiastowy", "niespodziewany", "nieuporządkowany", "samodzielność", "perspektywiczny",
                    "systematyczność", "organizatorski", "wielopokoleniowy", "wielokulturowość", "niedopuszczalny",
                    "odpowiedzieć", "przysługiwać", "przekształcenie", "scharakteryzować", "urzeczywistnić",
                    "upowszechnianie", "zagospodarować", "współzawodnictwo", "dowodzenie", "przedsięwzięcie",
                    "przeprowadzenie", "potwierdzenie", "dostarczenie", "uzupełnienie", "pomieszczenie",
                    "zaproszenie", "powiadomienie", "upoważnienie", "rozwiązywanie", "postanowienie",
                    "ubezpieczenie", "opracowanie", "przemieszczenie", "wyposażenie", "przygotowany",
                    "odpowiednio", "przedstawiciel", "bezpieczeństwo", "zainteresowany", "nadzwyczajny"
                };

                foreach (var w in easy.Distinct())
                    words.Add(new Word { Text = w, Difficulty = "easy", Category = "ogólne" });
                foreach (var w in medium.Distinct())
                    words.Add(new Word { Text = w, Difficulty = "medium", Category = "ogólne" });
                foreach (var w in hard.Distinct())
                    words.Add(new Word { Text = w, Difficulty = "hard", Category = "ogólne" });

                await _wordsCollection.InsertManyAsync(words);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seed error: {ex.Message}");
            }
        }

        // ─── GAME SESSIONS ─────────────────────────────────────────────

        /// <summary>
        /// Saves a completed game session.
        /// </summary>
        public async Task SaveGameSessionAsync(GameSession session)
        {
            if (_sessionsCollection == null) throw new InvalidOperationException("Brak połączenia z bazą danych.");
            try
            {
                if (session.Id == ObjectId.Empty)
                    session.Id = ObjectId.GenerateNewId();

                await _sessionsCollection.InsertOneAsync(session);
            }
            catch (Exception ex)
            {
                throw new Exception("Błąd zapisu sesji: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Gets top scores across all players, sorted by score descending.
        /// </summary>
        public async Task<List<GameSession>> GetTopScoresAsync(int limit = 10)
        {
            if (_sessionsCollection == null) return new List<GameSession>();
            try
            {
                var sort = Builders<GameSession>.Sort.Descending(s => s.Score);
                return await _sessionsCollection.Find(FilterDefinition<GameSession>.Empty)
                    .Sort(sort).Limit(limit).ToListAsync();
            }
            catch
            {
                return new List<GameSession>();
            }
        }

        /// <summary>
        /// Gets top scores filtered by difficulty, sorted by score descending.
        /// </summary>
        public async Task<List<GameSession>> GetTopScoresByDifficultyAsync(string difficulty, int limit = 10)
        {
            if (_sessionsCollection == null) return new List<GameSession>();
            try
            {
                var filter = Builders<GameSession>.Filter.Eq(s => s.Difficulty, difficulty);
                var sort = Builders<GameSession>.Sort.Descending(s => s.Score);
                return await _sessionsCollection.Find(filter)
                    .Sort(sort).Limit(limit).ToListAsync();
            }
            catch
            {
                return new List<GameSession>();
            }
        }

        /// <summary>
        /// Gets the most recent game sessions across all players, sorted by date descending.
        /// </summary>
        public async Task<List<GameSession>> GetRecentGamesAsync(int limit = 20)
        {
            if (_sessionsCollection == null) return new List<GameSession>();
            try
            {
                var sort = Builders<GameSession>.Sort.Descending(s => s.Date);
                return await _sessionsCollection.Find(FilterDefinition<GameSession>.Empty)
                    .Sort(sort).Limit(limit).ToListAsync();
            }
            catch
            {
                return new List<GameSession>();
            }
        }

        /// <summary>
        /// Gets the best game session (record) for a specific player and difficulty.
        /// </summary>
        public async Task<GameSession?> GetPlayerRecordAsync(string playerName, string difficulty)
        {
            if (_sessionsCollection == null) return null;
            try
            {
                var filter = Builders<GameSession>.Filter.And(
                    Builders<GameSession>.Filter.Regex(s => s.PlayerName, new BsonRegularExpression("^" + System.Text.RegularExpressions.Regex.Escape(playerName) + "$", "i")),
                    Builders<GameSession>.Filter.Eq(s => s.Difficulty, difficulty)
                );
                var sort = Builders<GameSession>.Sort.Descending(s => s.Score);
                return await _sessionsCollection.Find(filter).Sort(sort).FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}
