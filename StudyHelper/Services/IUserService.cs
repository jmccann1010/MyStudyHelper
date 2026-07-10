using StudyHelper.Models;

namespace StudyHelper.Services;

public interface IUserService
{
    /// <summary>
    /// Validates user credentials and returns user if valid
    /// </summary>
    Task<User?> ValidateUserAsync(string username, string password);

    /// <summary>
    /// Creates a new user account
    /// </summary>
    Task<bool> CreateUserAsync(string username, string password);

    /// <summary>
    /// Checks if username already exists
    /// </summary>
    Task<bool> UserExistsAsync(string username);

    /// <summary>
    /// Updates user's last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(string username);

    /// <summary>
    /// Retrieves user by username
    /// </summary>
    Task<User?> GetUserAsync(string username);
}
