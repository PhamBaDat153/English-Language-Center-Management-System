using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Source.ViewModels
{
    public class UserDetailsViewModel
    {
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PasswordStatus { get; set; }
        public string Roles { get; set; }
        public string SelectedRole { get; set; }
        [Required]
        public string Username { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [DataType(DataType.Password), MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        public string NewPassword { get; set; }
        public string AuthProvider { get; set; }
        public string AuthProviderKey { get; set; }
        public string FullName { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public string AvatarUrl { get; set; }
        public string NativeLanguage { get; set; }
        public string EmergencyContactNumber { get; set; }
        public string EmergencyContactName { get; set; }
        public string ActiveStatus { get; set; }

        public IList<StudentProfileViewModel> StudentProfiles { get; set; } = new List<StudentProfileViewModel>();
        public IList<StaffProfileViewModel> StaffProfiles { get; set; } = new List<StaffProfileViewModel>();
    }
}
