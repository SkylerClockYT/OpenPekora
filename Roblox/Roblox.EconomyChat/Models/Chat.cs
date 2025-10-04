using System.ComponentModel.DataAnnotations;

namespace Roblox.EconomyChat.Models;

public class UnreadMessageCount
{
    public long unreadCount { get; set; }
}

public class ChatAuthor
{
    [Required]
    public string? username { get; set; }
    public long userId { get; set; }
    public bool isAdmin { get; set; }
}

public class ChatMessage
{
    public long messageId { get; set; }
    public ChatAuthor author { get; set; } = null!;
    public string content { get; set; } = null!;
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

public class DirectChatMessage : ChatMessage
{
    
}

public class ChannelChatMessage : ChatMessage
{
    public long channelId { get; set; }
}

public class CreateMessageRequest
{
    [Required]
    public string content { get; set; } = null!;
    public long channelId { get; set; }
}