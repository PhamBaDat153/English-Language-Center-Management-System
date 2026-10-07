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

    public class StudentProfileViewModel
    {
        public Guid StudentId { get; set; }
        public Guid UserId { get; set; }
        public string StudentCode { get; set; }
        public string StudentStatus { get; set; }
        [DataType(DataType.Date)] public DateTime? EnrollmentDate { get; set; }
        public string CurrentLevel { get; set; }
        public string LearningGoal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AvailableStatus { get; set; }
    }

    public class StaffProfileViewModel
    {
        public Guid StaffId { get; set; }
        public Guid UserId { get; set; }
        public string StaffCode { get; set; }
        public string JobTitle { get; set; }
        public string EmploymentType { get; set; }
        public string EmploymentStatus { get; set; }
        public string Department { get; set; }
        public decimal? Salary { get; set; }
        [DataType(DataType.Date)] public DateTime? HireDate { get; set; }
        [DataType(DataType.Date)] public DateTime? TerminationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AvailableStatus { get; set; }
        public IList<TeacherProfileViewModel> Teachers { get; set; } = new List<TeacherProfileViewModel>();
    }

    public class TeacherProfileViewModel
    {
        public Guid TeacherId { get; set; }
        public Guid StaffId { get; set; }
        public string ProfessionalTitle { get; set; }
        public int? YearsOfExperience { get; set; }
        public string TeachingBio { get; set; }
        public string Specializations { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AvailableStatus { get; set; }
        public IList<QualificationViewModel> Qualifications { get; set; } = new List<QualificationViewModel>();
    }

    public class QualificationViewModel
    {
        public Guid QualificationId { get; set; }
        public Guid TeacherId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool PermanentDelete { get; set; }
        public string QualificationName { get; set; }
        public string Institution { get; set; }
        [DataType(DataType.Date)] public DateTime? IssuedDate { get; set; }
        [DataType(DataType.Date)] public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AvailableStatus { get; set; }
    }
}
