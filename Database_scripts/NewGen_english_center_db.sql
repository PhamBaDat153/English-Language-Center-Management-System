USE master;
GO

IF DB_ID(N'NewGen_english_center_db') IS NOT NULL
BEGIN
    ALTER DATABASE NewGen_english_center_db
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE NewGen_english_center_db;
END;
GO

IF DB_ID(N'NewGen_english_center_db') IS NULL CREATE DATABASE NewGen_english_center_db;
GO
USE NewGen_english_center_db;
GO

CREATE TABLE dbo.Users (
    user_id uniqueidentifier NOT NULL,
    username varchar(100) NULL,
    hashed_password varchar(255) NULL,
    email varchar(255) NULL,
    auth_provider varchar(20) NULL,
    auth_provider_key varchar(255) NULL,
    full_name nvarchar(200) NOT NULL,
    date_of_birth date NULL,
    phone_number varchar(30) NULL,
    gender varchar(20) NULL,
    avatar_url varchar(500) NULL,
    native_language nvarchar(100) NULL,
    emergency_contact_number varchar(30) NULL,
    emergency_contact_name nvarchar(200) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    active_status varchar(20) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (user_id),
    CONSTRAINT UQ_Users_Email UNIQUE (email),
    CONSTRAINT CK_Users_Provider CHECK (auth_provider IN ('Google','Zalo')),
    CONSTRAINT CK_Users_Gender CHECK (gender IS NULL OR gender IN ('Male','Female','Other','PreferNotToSay')),
    CONSTRAINT CK_Users_Status CHECK (active_status IN ('Active','Inactive'))
);
GO

CREATE TABLE dbo.Roles (
    role_id uniqueidentifier NOT NULL,
    role_name varchar(30) NOT NULL,
    role_description nvarchar(500) NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (role_id),
    CONSTRAINT UQ_Roles_Name UNIQUE (role_name),
    CONSTRAINT CK_Roles_Name CHECK (role_name IN ('Student','Teacher','Administrator')),
    CONSTRAINT CK_Roles_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.User_Roles (
    user_id uniqueidentifier NOT NULL,
    role_id uniqueidentifier NOT NULL,
    CONSTRAINT PK_User_Roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(role_id)
);
GO

CREATE TABLE dbo.Staff_Profiles (
    staff_id uniqueidentifier NOT NULL,
    user_id uniqueidentifier NOT NULL,
    staff_code varchar(50) NOT NULL,
    job_title nvarchar(150) NULL,
    employment_type varchar(20) NULL,
    employment_status varchar(20) NOT NULL,
    department nvarchar(150) NULL,
    salary decimal(18,2) NULL,
    hire_date date NULL,
    termination_date date NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Staff PRIMARY KEY (staff_id), 
    CONSTRAINT UQ_Staff_User UNIQUE (user_id),
    CONSTRAINT UQ_Staff_Code UNIQUE (staff_code),
    CONSTRAINT FK_Staff_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
    CONSTRAINT CK_Staff_Type CHECK (employment_type IS NULL OR employment_type IN ('FullTime','PartTime','Contract','Intern')), 
    CONSTRAINT CK_Staff_Status CHECK (employment_status IN ('Active','OnLeave','Resigned','Terminated')),
    CONSTRAINT CK_Staff_Available CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.Student_Profiles (
    student_id uniqueidentifier NOT NULL,
    user_id uniqueidentifier NOT NULL,
    student_code varchar(50) NOT NULL,
    student_status varchar(20) NOT NULL,
    enrollment_date date NULL,
    current_level varchar(30) NULL,
    learning_goal nvarchar(500) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Students PRIMARY KEY (student_id),
    CONSTRAINT UQ_Students_User UNIQUE (user_id),
    CONSTRAINT UQ_Students_Code UNIQUE (student_code),
    CONSTRAINT FK_Students_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id), 
    CONSTRAINT CK_Students_Status CHECK (student_status IN ('Active','Inactive','Graduated','Suspended')),
    CONSTRAINT CK_Students_Level CHECK (current_level IS NULL OR current_level IN ('Beginner','Elementary','Intermediate','UpperIntermediate','Advanced','IELTS','TOEIC')),  
    CONSTRAINT CK_Students_Available CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.teacher_profiles (
    teacher_id uniqueidentifier NOT NULL,
    staff_id uniqueidentifier NOT NULL,
    professional_title nvarchar(150) NULL,
    years_of_experience int NULL,
    teaching_bio nvarchar(max) NULL,
    specializations nvarchar(500) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Teachers PRIMARY KEY (teacher_id), 
    CONSTRAINT UQ_Teachers_Staff UNIQUE (staff_id),
    CONSTRAINT FK_Teachers_Staff FOREIGN KEY (staff_id) REFERENCES dbo.Staff_Profiles(staff_id),
    CONSTRAINT CK_Teachers_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.teacher_qualifications (
    qualification_id uniqueidentifier NOT NULL,
    teacher_id uniqueidentifier NOT NULL,
    qualification_name nvarchar(200) NOT NULL,
    institution nvarchar(200) NULL,
    issued_date date NULL,
    expiry_date date NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Qualifications PRIMARY KEY (qualification_id),
    CONSTRAINT FK_Qualifications_Teacher FOREIGN KEY (teacher_id) REFERENCES dbo.teacher_profiles(teacher_id),
    CONSTRAINT CK_Qualifications_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.Courses (
    course_id uniqueidentifier NOT NULL,
    course_code varchar(50) NOT NULL,
    course_name nvarchar(200) NOT NULL,
    level varchar(30) NOT NULL,
    description nvarchar(1000) NULL,
    duration_weeks int NOT NULL,
    tuition_fee decimal(18,2) NOT NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Courses PRIMARY KEY (course_id),
    CONSTRAINT UQ_Courses_Code UNIQUE (course_code),
    CONSTRAINT CK_Courses_Level CHECK (level IN ('Beginner','Elementary','Intermediate','UpperIntermediate','Advanced','IELTS','TOEIC')),
    CONSTRAINT CK_Courses_Duration CHECK (duration_weeks > 0), 
    CONSTRAINT CK_Courses_Fee CHECK (tuition_fee >= 0), CONSTRAINT CK_Courses_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.SyllabusMaterials (
    syllabus_id uniqueidentifier NOT NULL,
    course_id uniqueidentifier NOT NULL,
    title nvarchar(200) NOT NULL,
    description nvarchar(1000) NULL,
    file_url varchar(1000) NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Syllabus PRIMARY KEY (syllabus_id),
    CONSTRAINT FK_Syllabus_Course FOREIGN KEY (course_id) REFERENCES dbo.Courses(course_id),  
    CONSTRAINT CK_Syllabus_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.Rooms (
    room_id uniqueidentifier NOT NULL,
    room_code varchar(50) NOT NULL,
    room_name nvarchar(150) NOT NULL,
    max_capacity int NOT NULL,
    location nvarchar(300) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_Rooms PRIMARY KEY (room_id),
    CONSTRAINT UQ_Rooms_Code UNIQUE (room_code),
    CONSTRAINT CK_Rooms_Capacity CHECK (max_capacity > 0),
    CONSTRAINT CK_Rooms_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.Classes (
    class_id uniqueidentifier NOT NULL,
    teacher_id uniqueidentifier NULL,
    course_id uniqueidentifier NOT NULL,
    room_id uniqueidentifier NULL,
    class_code varchar(50) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    class_status varchar(20) NOT NULL,
    CONSTRAINT PK_Classes PRIMARY KEY (class_id),
    CONSTRAINT UQ_Classes_Code UNIQUE (class_code),
    CONSTRAINT FK_Classes_Teacher FOREIGN KEY (teacher_id) REFERENCES dbo.teacher_profiles(teacher_id),
    CONSTRAINT FK_Classes_Course FOREIGN KEY (course_id) REFERENCES dbo.Courses(course_id),
    CONSTRAINT FK_Classes_Room FOREIGN KEY (room_id) REFERENCES dbo.Rooms(room_id),
    CONSTRAINT CK_Classes_Dates CHECK (end_date >= start_date),
    CONSTRAINT CK_Classes_Status CHECK (class_status IN ('Planned','Open','InProgress','Completed','Cancelled'))
);
GO

CREATE TABLE dbo.TimeSlots (
    time_slot_id uniqueidentifier NOT NULL,
    slot_name nvarchar(100) NOT NULL,
    start_time time NOT NULL,
    end_time time NOT NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    available_status varchar(20) NOT NULL,
    CONSTRAINT PK_TimeSlots PRIMARY KEY (time_slot_id),
    CONSTRAINT UQ_TimeSlots_Name UNIQUE (slot_name),
    CONSTRAINT CK_TimeSlots_Range CHECK (end_time > start_time),
    CONSTRAINT CK_TimeSlots_Status CHECK (available_status IN ('Available','Unavailable'))
);
GO

CREATE TABLE dbo.ClassSchedules (
    class_schedule_id uniqueidentifier NOT NULL,
    class_id uniqueidentifier NOT NULL,
    time_slot_id uniqueidentifier NOT NULL,
    room_name nvarchar(150) NULL,
    week_day varchar(10) NOT NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    CONSTRAINT PK_ClassSchedules PRIMARY KEY (class_schedule_id),
    CONSTRAINT FK_Schedules_Class FOREIGN KEY (class_id) REFERENCES dbo.Classes(class_id),
    CONSTRAINT FK_Schedules_TimeSlot FOREIGN KEY (time_slot_id) REFERENCES dbo.TimeSlots(time_slot_id),
    CONSTRAINT CK_Schedules_Day CHECK (week_day IN ('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday'))
);
GO

CREATE TABLE dbo.Enrollments (
    enrollment_id uniqueidentifier NOT NULL,
    class_id uniqueidentifier NOT NULL,
    student_id uniqueidentifier NOT NULL,
    enrollment_date date NOT NULL,
    notes nvarchar(1000) NULL,
    tuition_amount decimal(18,2) NOT NULL,
    confirmed_at datetime2(3) NULL,
    withdrawn_at datetime2(3) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    enrollment_status varchar(20) NOT NULL,
    CONSTRAINT PK_Enrollments PRIMARY KEY (enrollment_id),
    CONSTRAINT UQ_Enrollments_StudentClass UNIQUE (student_id,class_id),
    CONSTRAINT FK_Enrollments_Class FOREIGN KEY (class_id) REFERENCES dbo.Classes(class_id),
    CONSTRAINT FK_Enrollments_Student FOREIGN KEY (student_id) REFERENCES dbo.Student_Profiles(student_id),
    CONSTRAINT CK_Enrollments_Fee CHECK (tuition_amount >= 0),
    CONSTRAINT CK_Enrollments_Status CHECK (enrollment_status IN ('Pending','Confirmed','Studying','Completed','Withdrawn','Cancelled'))
);
GO

CREATE TABLE dbo.ClassSessions (
    session_id uniqueidentifier NOT NULL,
    class_id uniqueidentifier NOT NULL,
    time_slot_id uniqueidentifier NOT NULL,
    session_date date NOT NULL,
    topic nvarchar(300) NULL,
    CONSTRAINT PK_ClassSessions PRIMARY KEY (session_id),
    CONSTRAINT FK_Sessions_Class FOREIGN KEY (class_id) REFERENCES dbo.Classes(class_id),
    CONSTRAINT FK_Sessions_TimeSlot FOREIGN KEY (time_slot_id) REFERENCES dbo.TimeSlots(time_slot_id),
    CONSTRAINT UQ_Sessions_ClassDateSlot UNIQUE (class_id,session_date,time_slot_id)
);
GO

CREATE TABLE dbo.AttendanceRecords (
    attendance_id uniqueidentifier NOT NULL,
    enrollment_id uniqueidentifier NOT NULL,
    session_id uniqueidentifier NOT NULL,
    recorded_by uniqueidentifier NOT NULL,
    remarks nvarchar(1000) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    attendance_status varchar(20) NOT NULL,
    CONSTRAINT PK_Attendance PRIMARY KEY (attendance_id),
    CONSTRAINT UQ_Attendance UNIQUE (enrollment_id,session_id),
    CONSTRAINT FK_Attendance_Enrollment FOREIGN KEY (enrollment_id) REFERENCES dbo.Enrollments(enrollment_id),
    CONSTRAINT FK_Attendance_Session FOREIGN KEY (session_id) REFERENCES dbo.ClassSessions(session_id),
    CONSTRAINT FK_Attendance_User FOREIGN KEY (recorded_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT CK_Attendance_Status CHECK (attendance_status IN ('Present','Absent','Excused'))
);
GO

CREATE TABLE dbo.Assessments (
    assessment_id uniqueidentifier NOT NULL,
    class_id uniqueidentifier NOT NULL,
    assessment_type varchar(20) NOT NULL,
    assessment_name nvarchar(200) NOT NULL,
    max_score decimal(8,2) NOT NULL,
    assessment_date date NULL,
    description nvarchar(1000) NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    CONSTRAINT PK_Assessments PRIMARY KEY (assessment_id),
    CONSTRAINT FK_Assessments_Class FOREIGN KEY (class_id) REFERENCES dbo.Classes(class_id),
    CONSTRAINT CK_Assessments_Type CHECK (assessment_type IN ('Quiz','Assignment','Midterm','Final','Speaking','Listening','Reading','Writing','Project')),
    CONSTRAINT CK_Assessments_Max CHECK (max_score > 0)
);
GO

CREATE TABLE dbo.AssessmentScores (
    assessment_score_id uniqueidentifier NOT NULL,
    entered_by uniqueidentifier NOT NULL,
    assessment_id uniqueidentifier NOT NULL,
    enrollment_id uniqueidentifier NOT NULL,
    score decimal(8,2) NOT NULL,
    feedback nvarchar(2000) NULL,
    created_at datetime2(3) NOT NULL,
    CONSTRAINT PK_AssessmentScores PRIMARY KEY (assessment_score_id),
    CONSTRAINT UQ_Scores_AssessmentEnrollment UNIQUE (assessment_id,enrollment_id),
    CONSTRAINT FK_Scores_User FOREIGN KEY (entered_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_Scores_Assessment FOREIGN KEY (assessment_id) REFERENCES dbo.Assessments(assessment_id),
    CONSTRAINT FK_Scores_Enrollment FOREIGN KEY (enrollment_id) REFERENCES dbo.Enrollments(enrollment_id),
    CONSTRAINT CK_Scores_NonNegative CHECK (score >= 0)
);
GO

CREATE TABLE dbo.Payments (
    payment_id uniqueidentifier NOT NULL,
    recorded_by uniqueidentifier NOT NULL,
    enrollment_id uniqueidentifier NOT NULL,
    amount decimal(18,2) NOT NULL,
    payment_date date NOT NULL,
    receipt_number varchar(100) NULL,
    notes nvarchar(1000) NULL,
    created_at datetime2(3) NOT NULL,
    payment_status varchar(20) NOT NULL,
    payment_method varchar(20) NOT NULL,
    CONSTRAINT PK_Payments PRIMARY KEY (payment_id),
    CONSTRAINT UQ_Payments_Receipt UNIQUE (receipt_number),
    CONSTRAINT FK_Payments_User FOREIGN KEY (recorded_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_Payments_Enrollment FOREIGN KEY (enrollment_id) REFERENCES dbo.Enrollments(enrollment_id),
    CONSTRAINT CK_Payments_Amount CHECK (amount > 0),
    CONSTRAINT CK_Payments_Status CHECK (payment_status IN ('Pending','Completed','Failed','Refunded','Cancelled')),
    CONSTRAINT CK_Payments_Method CHECK (payment_method IN ('Cash','BankTransfer','Card'))
);
GO

CREATE TABLE dbo.Notifications (
    notification_id uniqueidentifier NOT NULL,
    user_id uniqueidentifier NOT NULL,
    title nvarchar(200) NOT NULL,
    message nvarchar(2000) NOT NULL,
    notification_type varchar(30) NOT NULL,
    created_at datetime2(3) NOT NULL,
    updated_at datetime2(3) NOT NULL,
    read_status varchar(10) NOT NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY (notification_id),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
    CONSTRAINT CK_Notifications_Type CHECK (notification_type IN ('ClassReminder','PaymentReminder','ScoreRelease','EnrollmentUpdate','General')),
    CONSTRAINT CK_Notifications_Read CHECK (read_status IN ('Unread','Read'))
);
GO
