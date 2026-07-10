using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Username must not exceed 16 characters")]
    [Display(Name = "Username")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    // Upper-bound prevents PBKDF2 CPU-exhaustion via very large payloads
    [StringLength(256, ErrorMessage = "Password must not exceed 256 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
