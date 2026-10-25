using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
    public class CourseDetailViewModel
    {
        public System.Guid course_id { get; set; }
        public string course_code { get; set; }
        public string course_name { get; set; }
        public string level { get; set; }
        public string description { get; set; }
        public int duration_weeks { get; set; }
        public decimal tuition_fee { get; set; }
        public int TotalStudents { get; set; }
        public List<Course> RelatedCourses { get; set; } = new List<Course>();
        public List<ClassOptionViewModel> AvailableClasses { get; set; } = new List<ClassOptionViewModel>();
    }

    public class ClassOptionViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public string ScheduleText { get; set; }
        public string RoomName { get; set; }
        public bool IsFull { get; set; }
    }
}
