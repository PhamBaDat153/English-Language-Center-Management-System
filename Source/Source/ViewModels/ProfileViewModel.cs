using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Source.ViewModels
{
    public class ProfileViewModel
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string AuthProvider { get; set; }
        public string ActiveStatus { get; set; }
        public string Roles { get; set; }
        public string HighestRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AvatarUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(30)]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [StringLength(100)]
        [Display(Name = "Ngôn ngữ mẹ đẻ")]
        public string NativeLanguage { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(30)]
        [Display(Name = "Số điện thoại khẩn cấp")]
        public string EmergencyContactNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Tên người liên hệ khẩn cấp")]
        public string EmergencyContactName { get; set; }

        public string StudentCode { get; set; }
        public string StudentStatus { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string CurrentLevel { get; set; }

        [StringLength(500, ErrorMessage = "Mục tiêu học tập không được vượt quá 500 ký tự.")]
        [Display(Name = "Mục tiêu học tập")]
        public string LearningGoal { get; set; }

        public string StudentAvailableStatus { get; set; }
        public bool HasStudentProfile { get; set; }
        public bool IsStudent { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsAdministrator { get; set; }
        public IList<StaffProfileViewModel> StaffProfiles { get; set; } = new List<StaffProfileViewModel>();
        public IList<TeacherProfileViewModel> TeacherProfiles { get; set; } = new List<TeacherProfileViewModel>();
        public IList<QualificationViewModel> Qualifications { get; set; } = new List<QualificationViewModel>();

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; }
    }
}
