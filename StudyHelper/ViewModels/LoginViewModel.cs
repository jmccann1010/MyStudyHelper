using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
