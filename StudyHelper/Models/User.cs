namespace StudyHelper.Models;

public class User
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
