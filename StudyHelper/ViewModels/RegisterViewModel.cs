using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 16 characters")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username may only contain letters and numbers")]
    [Display(Name = "Username")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public required string ConfirmPassword { get; set; }
}
