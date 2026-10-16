using System.ComponentModel.DataAnnotations;

namespace Source.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your username or email.")]
        [Display(Name = "Username or email")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}