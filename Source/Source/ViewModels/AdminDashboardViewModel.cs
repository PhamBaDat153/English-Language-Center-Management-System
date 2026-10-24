using System;
using System.Collections.Generic;

namespace Source.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveStudents { get; set; }
        public int AvailableTeachers { get; set; }
        public int AvailableCourses { get; set; }
        public int OpenClasses { get; set; }
        public int InProgressClasses { get; set; }
        public int ActiveEnrollments { get; set; }
        public int PendingPayments { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public IList<DashboardChartPointViewModel> RevenueByMonth { get; set; } = new List<DashboardChartPointViewModel>();
        public IList<DashboardChartPointViewModel> EnrollmentByMonth { get; set; } = new List<DashboardChartPointViewModel>();
        public IList<DashboardChartPointViewModel> ClassStatusSummary { get; set; } = new List<DashboardChartPointViewModel>();
        public IList<DashboardChartPointViewModel> StudentLevelSummary { get; set; } = new List<DashboardChartPointViewModel>();

        public IList<PendingPaymentViewModel> PendingPaymentList { get; set; } = new List<PendingPaymentViewModel>();
        public IList<UpcomingClassViewModel> UpcomingClasses { get; set; } = new List<UpcomingClassViewModel>();
        public IList<NearlyFullClassViewModel> NearlyFullClasses { get; set; } = new List<NearlyFullClassViewModel>();
    }

    public class DashboardChartPointViewModel
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class PendingPaymentViewModel
    {
        public Guid PaymentId { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class UpcomingClassViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public string TeacherName { get; set; }
        public string RoomName { get; set; }
        public DateTime StartDate { get; set; }
        public int EnrolledCount { get; set; }
        public int Capacity { get; set; }
        public int RemainingSlots { get; set; }
    }

    public class NearlyFullClassViewModel
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string CourseName { get; set; }
        public int EnrolledCount { get; set; }
        public int Capacity { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}
