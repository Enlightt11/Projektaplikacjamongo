using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace projektaplikacjamongo.Models
{
    [BsonIgnoreExtraElements]
    public class GameSession
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("player_name")]
        public string PlayerName { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("difficulty")]
        public string Difficulty { get; set; } = "easy";

        [BsonElement("score")]
        public int Score { get; set; }

        [BsonElement("kpm")]
        public double Kpm { get; set; }

        [BsonElement("words_destroyed")]
        public int WordsDestroyed { get; set; }

        [BsonElement("words_missed")]
        public int WordsMissed { get; set; }

        [BsonElement("accuracy_percent")]
        public double AccuracyPercent { get; set; }

        [BsonElement("duration_seconds")]
        public int DurationSeconds { get; set; }

        // ─── Display helpers (not stored in DB) ───
        [BsonIgnore]
        public string DifficultyDisplay => Difficulty switch
        {
            "easy" => "ŁATWY",
            "medium" => "ŚREDNI",
            "hard" => "TRUDNY",
            _ => Difficulty.ToUpper()
        };

        [BsonIgnore]
        public string DateFormatted => Date.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    }
}
