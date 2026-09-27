using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TaskFlow.Notifications.Models;

namespace TaskFlow.Notifications.Services;

public class NotificationRepository
{
    private readonly IMongoCollection<Notification> _collection;

    public NotificationRepository(IOptions<MongoOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        var database = client.GetDatabase(options.Value.Database);
        _collection = database.GetCollection<Notification>("notifications");
    }

    public Task InsertAsync(Notification notification, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(notification, cancellationToken: cancellationToken);

    public async Task<List<Notification>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _collection
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Limit(100)
            .ToListAsync(cancellationToken);

    public Task<UpdateResult> MarkAsReadAsync(string id, CancellationToken cancellationToken = default) =>
        _collection.UpdateOneAsync(
            n => n.Id == id,
            Builders<Notification>.Update.Set(n => n.IsRead, true),
            cancellationToken: cancellationToken);
}
