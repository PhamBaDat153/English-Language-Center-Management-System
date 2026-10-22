using System;
using System.Collections.Generic;

namespace Source.ViewModels
{
    public class AssessmentGradeViewModel
    {
        public Guid assessment_id { get; set; }
        public Guid class_id { get; set; }
        public string class_code { get; set; }
        public string assessment_name { get; set; }
        public string assessment_type { get; set; }
        public decimal max_score { get; set; }
        public DateTime? assessment_date { get; set; }
        public List<AssessmentGradeStudentViewModel> Students { get; set; }
    }

    public class AssessmentGradeStudentViewModel
    {
        public Guid enrollment_id { get; set; }
        public string student_code { get; set; }
        public string student_name { get; set; }
        public decimal? score { get; set; }
        public string feedback { get; set; }
    }
}
