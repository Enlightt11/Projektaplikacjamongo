using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace projektaplikacjamongo.Models
{
    [BsonIgnoreExtraElements]
    public class Word
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("word")]
        public string Text { get; set; } = string.Empty;

        [BsonElement("difficulty")]
        public string Difficulty { get; set; } = "easy";

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;
    }
}
