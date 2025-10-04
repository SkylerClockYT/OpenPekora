using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Roblox.Website.Hubs;
public class MessageRouterHub : Hub
{
    // UniverseId -> Topic -> Subscribers
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<string>>> _universes
        = new();

    private string UniverseId =>
        Context.GetHttpContext()?.Request.Query["universeId"].ToString() ?? "default";

    private bool IsRCC
    {
        get
        {
            var ctx = Context.GetHttpContext();
            var rccAccessKey = ctx?.Request.Headers.ContainsKey("accesskey") == true
                ? ctx.Request.Headers["accesskey"].ToString()
                : null;

            return rccAccessKey == Configuration.RccAuthorization;
        }
    }

    public override async Task OnConnectedAsync()
    {
        if (!IsRCC)
        {
            Console.WriteLine($"[Rejected] {Context.ConnectionId} | Invalid RCC access key");
            Context.Abort();
            return;
        }

        Console.WriteLine($"[Connected] {Context.ConnectionId} | Universe: {UniverseId}");
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Disconnected] {Context.ConnectionId} | Universe: {UniverseId}");

        if (_universes.TryGetValue(UniverseId, out var topics))
        {
            foreach (var topic in topics.Keys)
            {
                topics[topic] = new ConcurrentBag<string>(
                    topics[topic].Where(id => id != Context.ConnectionId)
                );
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    public Task Subscribe(string topic, int flags)
    {
        if (!IsRCC) return Task.CompletedTask;

        Console.WriteLine($"[Subscribe] {Context.ConnectionId} -> {topic} (Universe: {UniverseId})");

        var topics = _universes.GetOrAdd(UniverseId, _ => new ConcurrentDictionary<string, ConcurrentBag<string>>());
        topics.AddOrUpdate(topic,
            _ => new ConcurrentBag<string> { Context.ConnectionId },
            (_, list) => { list.Add(Context.ConnectionId); return list; });

        return Task.CompletedTask;
    }

    public Task Publish(string topic, string messageJson, int flags)
    {
        if (!IsRCC) return Task.CompletedTask;

        Console.WriteLine($"[Publish] Universe: {UniverseId} -> {topic} : {messageJson}");

        if (_universes.TryGetValue(UniverseId, out var topics) &&
            topics.TryGetValue(topic, out var subscribers))
        {
            foreach (var connectionId in subscribers)
            {
                Clients.Client(connectionId)
                    .SendAsync("Message", topic, messageJson);
            }
        }

        return Task.CompletedTask;
    }
}
