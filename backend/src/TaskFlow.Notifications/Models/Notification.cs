using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskFlow.Notifications.Models;

// Document stored in MongoDB (Cosmos DB Mongo API in Azure — see infra/bicep).
// Deliberately schema-less/append-friendly: unlike the relational side, a new
// notification type never needs a migration, which is the whole point of
// picking a document store for this slice of the domain.
public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? RelatedProjectId { get; set; }
    public Guid? RelatedTaskId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
