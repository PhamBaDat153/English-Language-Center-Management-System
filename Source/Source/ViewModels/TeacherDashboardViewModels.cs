using System;
using System.Collections.Generic;

namespace Source.ViewModels
{
    /// <summary>
    /// Dữ liệu thật dùng cho trang chủ giáo viên; riêng danh sách thông báo vẫn để mẫu tĩnh.
    /// </summary>
    public class TeacherDashboardViewModel
    {
        public string TeacherName { get; set; }
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public int SessionsThisWeek { get; set; }
        public int AttendanceToComplete { get; set; }
        public TeacherSessionItemViewModel NextSession { get; set; }
        public IList<TeacherClassItemViewModel> Classes { get; set; }
        public IList<TeacherSessionItemViewModel> UpcomingSessions { get; set; }
        public IList<TeacherRecurringScheduleItemViewModel> WeeklySchedules { get; set; }
        public IList<TeacherAssessmentItemViewModel> LatestAssessments { get; set; }
    }

    public class TeacherClassesViewModel
    {
        public IList<TeacherClassItemViewModel> Classes { get; set; }
    }

    public class TeacherClassDetailsViewModel
    {
        public TeacherClassItemViewModel Class { get; set; }
        public IList<TeacherStudentItemViewModel> Students { get; set; }
        public IList<TeacherAssessmentItemViewModel> Assessments { get; set; }
    }

    public class TeacherClassItemViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public string RoomName { get; set; }
        public string ClassStatus { get; set; }
        public string ClassStatusLabel { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int StudentCount { get; set; }
    }

    public class TeacherStudentItemViewModel
    {
        public Guid EnrollmentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public string EnrollmentStatus { get; set; }
    }

    public class TeacherScheduleViewModel
    {
        public IList<TeacherSessionItemViewModel> Sessions { get; set; }
        public IList<TeacherRecurringScheduleItemViewModel> WeeklySchedules { get; set; }
    }

    public class TeacherSessionItemViewModel
    {
        public Guid SessionId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public string RoomName { get; set; }
        public DateTime SessionDate { get; set; }
        public string TimeSlotName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Topic { get; set; }
        public int StudentCount { get; set; }
        public int AttendanceCount { get; set; }
        public bool CanTakeAttendance { get; set; }
    }

    public class TeacherRecurringScheduleItemViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public string RoomName { get; set; }
        public string WeekDay { get; set; }
        public string TimeSlotName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class TeacherResultsViewModel
    {
        public IList<TeacherAssessmentItemViewModel> Assessments { get; set; }
        public AssessmentGradeViewModel SelectedAssessment { get; set; }
    }

    public class TeacherAssessmentItemViewModel
    {
        public Guid AssessmentId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public string AssessmentName { get; set; }
        public string AssessmentType { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime? AssessmentDate { get; set; }
        public int StudentCount { get; set; }
        public int ScoredCount { get; set; }
    }
}
