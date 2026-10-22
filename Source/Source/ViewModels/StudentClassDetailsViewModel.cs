using System;
using System.Collections.Generic;

namespace Source.ViewModels
{
    public class StudentClassDetailsViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string CourseLevel { get; set; }
        public string CourseDescription { get; set; }
        public int CourseDurationWeeks { get; set; }
        public string TeacherName { get; set; }
        public string RoomName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string EnrollmentStatus { get; set; }
        public string ClassStatus { get; set; }
        public decimal TuitionAmount { get; set; }
        public IList<StudentClassSessionViewModel> Sessions { get; set; }
        public IList<StudentAttendanceViewModel> Attendance { get; set; }
        public IList<StudentAssessmentViewModel> Assessments { get; set; }
        public IList<StudentMaterialViewModel> Materials { get; set; }
    }

    public class StudentClassSessionViewModel
    {
        public DateTime SessionDate { get; set; }
        public string TimeSlotName { get; set; }
        public string Topic { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public DateTime SessionDate { get; set; }
        public string TimeSlotName { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
    }

    public class StudentAssessmentViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
        public DateTime? AssessmentDate { get; set; }
    }

    public class StudentMaterialViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileUrl { get; set; }
    }
}
