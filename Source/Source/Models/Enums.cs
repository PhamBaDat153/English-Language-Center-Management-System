namespace Source.Models
{
    public enum AuthProvider
    {
        Google,
        Zalo
    }

    public enum Gender
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }

    public enum UserStatus
    {
        Active,
        Inactive
    }

    public enum RoleName
    {
        Student,
        Teacher,
        Administrator
    }

    public enum AvailabilityStatus
    {
        Available,
        Unavailable
    }

    public enum EmploymentType
    {
        FullTime,
        PartTime,
        Contract,
        Intern
    }

    public enum EmploymentStatus
    {
        Active,
        OnLeave,
        Resigned,
        Terminated
    }

    public enum StudentStatus
    {
        Active,
        Inactive,
        Graduated,
        Suspended
    }

    public enum EnglishLevel
    {
        Beginner,
        Elementary,
        Intermediate,
        UpperIntermediate,
        Advanced,
        IELTS,
        TOEIC
    }

    public enum ClassStatus
    {
        Planned,
        Open,
        InProgress,
        Completed,
        Cancelled
    }

    public enum WeekDay
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    public enum EnrollmentStatus
    {
        Pending,
        Confirmed,
        Studying,
        Completed,
        Withdrawn,
        Cancelled
    }

    public enum AttendanceStatus
    {
        Present,
        Absent,
        Excused
    }

    public enum AssessmentType
    {
        Quiz,
        Assignment,
        Midterm,
        Final,
        Speaking,
        Listening,
        Reading,
        Writing,
        Project
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        Card
    }

    public enum NotificationType
    {
        ClassReminder,
        PaymentReminder,
        ScoreRelease,
        EnrollmentUpdate,
        General
    }

    public enum ReadStatus
    {
        Unread,
        Read
    }
}
