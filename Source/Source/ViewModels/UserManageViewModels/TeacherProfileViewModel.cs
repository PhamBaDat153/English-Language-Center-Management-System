using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
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
}