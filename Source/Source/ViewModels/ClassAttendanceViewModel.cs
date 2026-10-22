using System;
using System.Collections.Generic;

namespace Source.ViewModels
{
    public class ClassAttendanceViewModel
    {
        public Guid session_id { get; set; }
        public Guid class_id { get; set; }
        public string class_code { get; set; }
        public DateTime session_date { get; set; }
        public string time_slot_name { get; set; }
        public string topic { get; set; }
        public List<ClassAttendanceStudentViewModel> Students { get; set; }
    }

    public class ClassAttendanceStudentViewModel
    {
        public Guid enrollment_id { get; set; }
        public string student_code { get; set; }
        public string student_name { get; set; }
        public string attendance_status { get; set; }
        public string remarks { get; set; }
    }
}
