using System.ComponentModel.DataAnnotations;

namespace Source.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có từ 3 đến 100 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }
    }
}
