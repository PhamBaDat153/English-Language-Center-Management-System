using Source.Models;
using System.Collections.Generic;

namespace Source.ViewModels
{
    public class HomePageViewModel
    {
        public int activeStudentCount { get; set; }
        public int courseCount { get; set; }
        public int classCount { get; set; }
        public int activeTeacherCount { get; set; }

        public List<Course> Courses { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}