using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TourMate.API.Data;
using TourMate.API.Models;
using System;

namespace TourMate.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private static readonly ConcurrentDictionary<int, string> OnlineUsers = new();

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out int userId))
        {
            OnlineUsers[userId] = Context.ConnectionId;
            await Clients.All.SendAsync("UserOnline", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out int userId))
        {
            OnlineUsers.TryRemove(userId, out _);
            await Clients.All.SendAsync("UserOffline", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public Task<IEnumerable<int>> GetOnlineUsers()
    {
        return Task.FromResult(OnlineUsers.Keys.AsEnumerable());
    }

    public async Task SendMessage(int receiverId, string content)
    {
        var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(senderIdStr, out int senderId))
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Read = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            if (OnlineUsers.TryGetValue(receiverId, out var receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", message);
            }
            await Clients.Caller.SendAsync("ReceiveMessage", message);
        }
    }
}
