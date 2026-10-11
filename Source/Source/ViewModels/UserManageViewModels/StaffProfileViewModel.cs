using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
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
}