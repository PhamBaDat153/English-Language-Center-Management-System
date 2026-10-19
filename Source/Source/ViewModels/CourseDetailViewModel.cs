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
        public List<Course> RelatedCourses { get; set; } = new List<Course>();
    }
}