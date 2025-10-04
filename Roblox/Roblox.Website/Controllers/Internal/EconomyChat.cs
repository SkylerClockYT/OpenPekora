using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.EconomyChat;
using Roblox.EconomyChat.Models;
using Roblox.Services.Exceptions;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/api/economy-chat/v1")]
public class EconomyChat : ControllerBase
{
    private ChatService chatService { get; }

    public EconomyChat()
    {
        chatService = new ChatService();
    }

    [HttpGet("metadata")]
    public Roblox.EconomyChat.Models.Metadata GetMetadata()
    {
        return new()
        {
            isEnabled = true,
        };
    }

    [HttpGet("channels/list")]
    public IEnumerable<Channel> GetChannels()
    {
        return Channel.channels;
    }

    [HttpGet("channels/{channelId:long}/messages")]
    public IEnumerable<ChannelChatMessage> GetMessages(long channelId, long startMessageId, int limit)
    {
        if (limit is > 100 or < 1)
            limit = 100;
        return chatService.GetMessagesInChannel(channelId, startMessageId, limit);
    }

    [HttpPost("channels/{channelId:long}/typing")]
    public void MarkAsTyping(long channelId)
    {
        chatService.ToggleTyping(safeUserSession.userId, channelId, true);
    }

    [HttpDelete("channels/{channelId:long}/typing")]
    public void MarkAsNotTyping(long channelId)
    {
        chatService.ToggleTyping(safeUserSession.userId, channelId, false);
    }

    [HttpGet("channels/{channelId:long}/read")]
    public UnreadMessageCount GetUnreadMessageCount(long channelId)
    {
        return chatService.GetUnreadMessageCount(safeUserSession.userId, channelId);
    }

    [HttpPost("channels/{channelId:long}/read")]
    public void MarkChannelAsRead(long channelId)
    {
        chatService.SetReadMessage(safeUserSession.userId, channelId);
    }

    [HttpPost("channels/{channelId:long}/send")]
    public async Task<ChatMessage> CreateMessage(long channelId, [Required, FromBody] CreateMessageRequest request)
    {
        request.channelId = channelId;
        return await chatService.CreateChannelMessage(safeUserSession.userId, request);
    }

    [HttpDelete("channels/{channelId:long}/messages/{messageId:long}")]
    public async Task DeleteMessage(long channelId, long messageId)
    {
        await chatService.RemoveMessage(safeUserSession.userId, messageId);
    }
}