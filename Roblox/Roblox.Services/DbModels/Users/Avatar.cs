using Roblox.Models.Avatar;

namespace Roblox.Services.DbModels;

public class DatabaseAvatar
{
    public int head_color_id { get; set; }
    public int torso_color_id { get; set; }
    public int left_leg_color_id { get; set; }
    public int right_leg_color_id { get; set; }
    public int left_arm_color_id { get; set; }
    public int right_arm_color_id { get; set; }
    public AvatarType avatar_type { get; set;}
    public double scale_height { get; set;}
    public double scale_width { get; set;}
    public double scale_head { get; set;}
    public double scale_depth { get; set;}
    public double scale_proportion { get; set;}
    public double scale_body_type { get; set;}
}

public class DatabaseAvatarWithImages : DatabaseAvatar
{
    public string thumbnail_url { get; set; }
    public string thumbnail_3d_url { get; set; }
    public string headshot_thumbnail_url { get; set; }
}
