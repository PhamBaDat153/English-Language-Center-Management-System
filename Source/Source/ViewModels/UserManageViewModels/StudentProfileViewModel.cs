using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
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
}