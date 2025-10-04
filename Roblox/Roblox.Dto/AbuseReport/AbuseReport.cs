using System.Xml.Serialization;
using Roblox.Models.AbuseReport;

namespace Roblox.Dto.AbuseReport;

public class AbuseReportEntry
{
    public string id { get; set; }
    public long userId { get; set; }
    public long authorId { get; set; }
    public AbuseReportReason reportReason { get; set; }
    public AbuseReportStatus reportStatus { get; set; }
    public string reportMessage { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

public class GameMessagesEntry
{
    public string reportId { get; set; }
    public string jobId { get; set; }
    public string messages { get; set; }
    public DateTime createdAt { get; set; }
}

[XmlRoot(ElementName = "message")]
public class InGameMessage
{

    [XmlAttribute(AttributeName = "userID")]
    public int userId { get; set; }

    [XmlAttribute(AttributeName = "guid")]
    public string guid { get; set; }

    [XmlText]
    public string text { get; set; }
}

[XmlRoot(ElementName = "messages")]
public class InGameMessages
{

    [XmlElement(ElementName = "message")]
    public List<InGameMessage> message { get; set; }
}


[XmlRoot(ElementName = "report")]
public class InGameAbuseReportEntry
{

    [XmlElement(ElementName = "comment")]
    public string comment { get; set; }

    [XmlElement(ElementName = "messages")]
    public InGameMessages messages { get; set; }

    [XmlAttribute(AttributeName = "userID")]
    public int userId { get; set; }

    [XmlAttribute(AttributeName = "placeID")]
    public int placeId { get; set; }

    [XmlAttribute(AttributeName = "gameJobID")]
    public string gameJobId { get; set; }
}
