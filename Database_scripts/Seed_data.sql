
USE NewGen_english_center_db;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;
    DECLARE @Now datetime2(3) = SYSDATETIME();

    -- 1) Roles
    IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE role_name = 'Student')
        INSERT dbo.[Role] (role_id, role_name, role_description, available_status)
        VALUES (NEWID(), 'Student', N'Học viên của trung tâm', 'Available');

    IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE role_name = 'Teacher')
        INSERT dbo.[Role] (role_id, role_name, role_description, available_status)
        VALUES (NEWID(), 'Teacher', N'Giáo viên tiếng Anh', 'Available');

    IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE role_name = 'Administrator')
        INSERT dbo.[Role] (role_id, role_name, role_description, available_status)
        VALUES (NEWID(), 'Administrator', N'Quản trị viên hệ thống', 'Available');


    -- 2) Users
    -- The schema only permits Google/Zalo as auth_provider.
    -- Seed accounts therefore use synthetic Google/Zalo provider keys;
    -- hashed_password is NULL because password authentication is not modeled.
    DECLARE @UserSeed TABLE (
        username varchar(100) NOT NULL,
        hashed_password varchar(255),
        email varchar(255) NOT NULL,
        full_name nvarchar(200) NOT NULL,
        date_of_birth date NULL,
        phone_number varchar(30) NULL,
        gender varchar(20) NULL,
        native_language nvarchar(100) NULL,
        emergency_contact_number varchar(30) NULL,
        emergency_contact_name nvarchar(200) NULL,
        role_name varchar(30) NOT NULL
    );

    INSERT @UserSeed VALUES

('admin_newgen', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','admin@newgen.edu.vn', N'Nguyễn Minh Anh','1988-04-12','0900000001','Female','Vietnamese','0900000002',N'Trần Quốc Huy','Administrator'),
('teacher01', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','teacher01@newgen.edu.vn', N'Trần Hoàng Nam','1990-02-18','0900001001','Male','Vietnamese',NULL,NULL,'Teacher'),
('teacher02', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','teacher02@newgen.edu.vn', N'Lê Thị Ngọc Mai','1992-07-25','0900001002','Female','Vietnamese',NULL,NULL,'Teacher'),
('teacher03', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','teacher03@newgen.edu.vn', N'Phạm Gia Huy','1989-11-03','0900001003','Male','Vietnamese',NULL,NULL,'Teacher'),
('teacher04', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','teacher04@newgen.edu.vn', N'Võ Thảo Vy','1993-05-16','0900001004','Female','Vietnamese',NULL,NULL,'Teacher'),
('teacher05', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','teacher05@newgen.edu.vn', N'Đặng Tuấn Kiệt','1991-09-09','0900001005','Male','Vietnamese',NULL,NULL,'Teacher'),
('student01', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student01@newgen.edu.vn', N'Nguyễn Khánh Linh','2004-01-14','0900002001','Female','Vietnamese','0900003001',N'Nguyễn Thị Lan','Student'),
('student02', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student02@newgen.edu.vn', N'Trần Minh Khang','2003-03-21','0900002002','Male','Vietnamese','0900003002',N'Trần Thị Hoa','Student'),
('student03', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student03@newgen.edu.vn', N'Lê Hoàng Yến','2005-06-02','0900002003','Female','Vietnamese','0900003003',N'Lê Văn Sơn','Student'),
('student04', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student04@newgen.edu.vn', N'Phạm Anh Đức','2002-09-11','0900002004','Male','Vietnamese','0900003004',N'Phạm Thị Hạnh','Student'),
('student05', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student05@newgen.edu.vn', N'Võ Ngọc Hân','2001-12-08','0900002005','Female','Vietnamese','0900003005',N'Võ Minh Tâm','Student'),
('student06', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student06@newgen.edu.vn', N'Đỗ Thành Long','2000-10-19','0900002006','Male','Vietnamese','0900003006',N'Đỗ Thị Mai','Student'),
('student07', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student07@newgen.edu.vn', N'Bùi Thảo Nguyên','2004-08-30','0900002007','Female','Vietnamese','0900003007',N'Bùi Văn Nam','Student'),
('student08', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student08@newgen.edu.vn', N'Nguyễn Quốc Bảo','2003-02-27','0900002008','Male','Vietnamese','0900003008',N'Nguyễn Thị Hương','Student'),
('student09', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student09@newgen.edu.vn', N'Trương Mỹ Duyên','2002-05-22','0900002009','Female','Vietnamese','0900003009',N'Trương Văn Phúc','Student'),
('student10', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student10@newgen.edu.vn', N'Hoàng Đức Minh','1999-07-07','0900002010','Male','Vietnamese','0900003010',N'Hoàng Thị Vân','Student'),
('student11', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student11@newgen.edu.vn', N'Phan Thanh Hà','2005-01-26','0900002011','Female','Vietnamese','0900003011',N'Phan Văn Hải','Student'),
('student12', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student12@newgen.edu.vn', N'Nguyễn Tấn Phát','2001-04-04','0900002012','Male','Vietnamese','0900003012',N'Nguyễn Thị Thu','Student'),
('student13', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student13@newgen.edu.vn', N'Đinh Quỳnh Anh','2004-11-13','0900002013','Female','Vietnamese','0900003013',N'Đinh Văn Minh','Student'),
('student14', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student14@newgen.edu.vn', N'Lý Minh Quân','2000-06-18','0900002014','Male','Vietnamese','0900003014',N'Lý Thị Ngân','Student'),
('student15', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student15@newgen.edu.vn', N'Phạm Ngọc Trâm','2003-09-29','0900002015','Female','Vietnamese','0900003015',N'Phạm Văn Bình','Student'),
('student16', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student16@newgen.edu.vn', N'Nguyễn Anh Tuấn','2002-12-17','0900002016','Male','Vietnamese','0900003016',N'Nguyễn Thị Hồng','Student'),
('student17', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student17@newgen.edu.vn', N'Trần Gia Hân','2004-03-10','0900002017','Female','Vietnamese','0900003017',N'Trần Văn Dũng','Student'),
('student18', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student18@newgen.edu.vn', N'Phạm Nhật Nam','2001-08-01','0900002018','Male','Vietnamese','0900003018',N'Phạm Thị Nga','Student'),
('student19', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student19@newgen.edu.vn', N'Mai Khả Vy','2005-10-05','0900002019','Female','Vietnamese','0900003019',N'Mai Văn Tùng','Student'),
('student20', N'$2a$12$IEJJyQmz/nPJs.H1CiGMeu/n7n4SljHz7doCD7lfPBvMCdCx6z3ui','student20@newgen.edu.vn', N'Nguyễn Đức Anh','1998-02-15','0900002020','Male','Vietnamese','0900003020',N'Nguyễn Thị Kim','Student');

    INSERT dbo.[User] (
        user_id, username, hashed_password, email,
        full_name, date_of_birth, phone_number, gender, avatar_url, native_language,
        emergency_contact_number, emergency_contact_name,
        created_at, updated_at, active_status
    )
    SELECT
        NEWID(), s.username, s.hashed_password, s.email,
        s.full_name, s.date_of_birth, s.phone_number, s.gender, NULL, s.native_language,
        s.emergency_contact_number, s.emergency_contact_name,
        @Now, @Now, 'Active'
    FROM @UserSeed s
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.[User] u
        WHERE u.username = s.username OR u.email = s.email
    );


    -- 3) Assign the requested roles
    INSERT dbo.[User_Role] (user_id, role_id)
    SELECT u.user_id, r.role_id
    FROM @UserSeed s
    INNER JOIN dbo.[User] u ON u.username = s.username
    INNER JOIN dbo.[Role] r ON r.role_name = s.role_name
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.[User_Role] ur
        WHERE ur.user_id = u.user_id AND ur.role_id = r.role_id
    );


    -- 4) Staff profiles for the administrator + five teachers
    DECLARE @StaffSeed TABLE (
        username varchar(100) NOT NULL,
        staff_code varchar(50) NOT NULL,
        job_title nvarchar(150) NULL,
        employment_type varchar(20) NULL,
        employment_status varchar(20) NOT NULL,
        department nvarchar(150) NULL,
        salary decimal(18,2) NULL,
        hire_date date NULL
    );

    INSERT @StaffSeed VALUES

('admin_newgen','ADM-001','Center Administrator','FullTime','Active','Administration',30000000,'2022-08-15'),
('teacher01','GV-001','Senior English Teacher','FullTime','Active','Academic',38000000,'2023-02-06'),
('teacher02','GV-002','English Teacher - General English','FullTime','Active','Academic',34000000,'2023-08-14'),
('teacher03','GV-003','English Teacher - IELTS','FullTime','Active','Academic',42000000,'2022-11-21'),
('teacher04','GV-004','English Teacher - Speaking','PartTime','Active','Academic',28000000,'2024-03-04'),
('teacher05','GV-005','English Teacher - TOEIC','FullTime','Active','Academic',36000000,'2024-07-15');

    INSERT dbo.[Staff_Profile] (
        staff_id, user_id, staff_code, job_title, employment_type,
        employment_status, department, salary, hire_date, termination_date,
        created_at, updated_at, available_status
    )
    SELECT
        NEWID(), u.user_id, s.staff_code, s.job_title, s.employment_type,
        s.employment_status, s.department, s.salary, s.hire_date, NULL,
        @Now, @Now, 'Available'
    FROM @StaffSeed s
    INNER JOIN dbo.[User] u ON u.username = s.username
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.[Staff_Profile] sp
        WHERE sp.user_id = u.user_id OR sp.staff_code = s.staff_code
    );


    -- 5) Teacher profiles
    DECLARE @TeacherSeed TABLE (
        username varchar(100) NOT NULL,
        professional_title nvarchar(150) NULL,
        years_of_experience int NULL,
        teaching_bio nvarchar(max) NULL,
        specializations nvarchar(500) NULL
    );

    INSERT @TeacherSeed VALUES

('teacher01','Senior English Teacher',7,N'Có kinh nghiệm giảng dạy General English và luyện thi cho sinh viên, người đi làm.',N'General English, Speaking, Grammar'),
('teacher02','English Teacher',5,N'Tập trung xây dựng nền tảng giao tiếp, phát âm và phản xạ cho học viên Việt Nam.',N'General English, Pronunciation, Communication'),
('teacher03','IELTS Instructor',9,N'Chuyên luyện IELTS với trọng tâm Writing, Speaking và chiến lược làm bài theo band score.',N'IELTS, Academic Writing, Speaking'),
('teacher04','Speaking Coach',4,N'Chuyên các lớp phản xạ và giao tiếp thực tế, phù hợp học viên cần tăng sự tự tin khi nói.',N'Speaking, Conversation, Pronunciation'),
('teacher05','TOEIC Instructor',6,N'Chuyên luyện TOEIC cho sinh viên và người đi làm, tập trung Listening và Reading.',N'TOEIC, Business English, Listening');

    INSERT dbo.[Teacher_profile] (
        teacher_id, staff_id, professional_title, years_of_experience,
        teaching_bio, specializations, created_at, updated_at, available_status
    )
    SELECT
        NEWID(), sp.staff_id, t.professional_title, t.years_of_experience,
        t.teaching_bio, t.specializations, @Now, @Now, 'Available'
    FROM @TeacherSeed t
    INNER JOIN dbo.[User] u ON u.username = t.username
    INNER JOIN dbo.[Staff_Profile] sp ON sp.user_id = u.user_id
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.[Teacher_profile] tp
        WHERE tp.staff_id = sp.staff_id
    );


    -- 6) One qualification per teacher for realistic staff seed data
    DECLARE @QualificationSeed TABLE (
        username varchar(100) NOT NULL,
        qualification_name nvarchar(200) NOT NULL,
        institution nvarchar(200) NULL,
        issued_date date NULL
    );

    INSERT @QualificationSeed VALUES

('teacher01',N'TESOL Certificate',N'Vietnam National University - HCMC','2021-07-15'),
('teacher02',N'CELTA',N'Cambridge Assessment English','2020-10-20'),
('teacher03',N'IELTS Teacher Training Certificate',N'British Council Vietnam','2022-03-12'),
('teacher04',N'TEFL Certificate',N'International TEFL Academy','2022-06-18'),
('teacher05',N'TOEIC Trainer Certificate',N'IIG Vietnam','2023-04-22');

    INSERT dbo.[Teacher_qualification] (
        qualification_id, teacher_id, qualification_name, institution,
        issued_date, expiry_date, created_at, updated_at, available_status
    )
    SELECT
        NEWID(), tp.teacher_id, q.qualification_name, q.institution,
        q.issued_date, NULL, @Now, @Now, 'Available'
    FROM @QualificationSeed q
    INNER JOIN dbo.[User] u ON u.username = q.username
    INNER JOIN dbo.[Staff_Profile] sp ON sp.user_id = u.user_id
    INNER JOIN dbo.[Teacher_profile] tp ON tp.staff_id = sp.staff_id
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.[Teacher_qualification] tq
        WHERE tq.teacher_id = tp.teacher_id
          AND tq.qualification_name = q.qualification_name
    );


    -- 7) Student profiles
    DECLARE @StudentSeed TABLE (
        username varchar(100) NOT NULL,
        student_code varchar(50) NOT NULL,
        student_status varchar(20) NOT NULL,
        enrollment_date date NULL,
        current_level varchar(30) NULL,
        learning_goal nvarchar(500) NULL
    );

    INSERT @StudentSeed VALUES

('student01','STU-001','Active','2026-01-04','Beginner',N'Giao tiếp tiếng Anh cơ bản để tự tin trong công việc.'),
('student02','STU-002','Active','2026-02-07','Elementary',N'Cải thiện phát âm và phản xạ giao tiếp hằng ngày.'),
('student03','STU-003','Active','2026-03-10','Elementary',N'Sử dụng tiếng Anh tốt hơn trong môi trường đại học.'),
('student04','STU-004','Active','2026-04-13','Intermediate',N'Nâng cao khả năng giao tiếp và thuyết trình bằng tiếng Anh.'),
('student05','STU-005','Active','2026-05-16','Intermediate',N'Phục vụ công việc văn phòng và giao tiếp với khách hàng quốc tế.'),
('student06','STU-006','Active','2026-06-19','UpperIntermediate',N'Chuẩn bị tiếng Anh cho môi trường làm việc đa quốc gia.'),
('student07','STU-007','Active','2026-07-22','Beginner',N'Xây dựng nền tảng từ vựng, ngữ pháp và nghe nói.'),
('student08','STU-008','Active','2026-08-01','Intermediate',N'Tăng điểm kiểm tra tiếng Anh đầu ra đại học.'),
('student09','STU-009','Active','2026-09-04','UpperIntermediate',N'Cải thiện viết học thuật và kỹ năng thảo luận.'),
('student10','STU-010','Active','2026-01-07','Advanced',N'Duy trì sự chính xác và trôi chảy khi sử dụng tiếng Anh chuyên môn.'),
('student11','STU-011','Active','2026-02-10','IELTS',N'Mục tiêu IELTS 6.5 để phục vụ du học.'),
('student12','STU-012','Active','2026-03-13','IELTS',N'Mục tiêu IELTS 7.0 cho hồ sơ du học và học bổng.'),
('student13','STU-013','Active','2026-04-16','TOEIC',N'Mục tiêu TOEIC 650+ để đáp ứng chuẩn tốt nghiệp.'),
('student14','STU-014','Active','2026-05-19','Intermediate',N'Cải thiện email, họp và thuyết trình trong công việc.'),
('student15','STU-015','Active','2026-06-22','Elementary',N'Học tiếng Anh để chuẩn bị chuyển lên trình độ giao tiếp trung cấp.'),
('student16','STU-016','Active','2026-07-01','Advanced',N'Luyện tiếng Anh chuyên nghiệp cho ngành công nghệ.'),
('student17','STU-017','Active','2026-08-04','IELTS',N'Mục tiêu IELTS 6.5 trong kỳ thi cuối năm.'),
('student18','STU-018','Active','2026-09-07','TOEIC',N'Mục tiêu TOEIC 750 để tăng cơ hội việc làm.'),
('student19','STU-019','Active','2026-01-10','Beginner',N'Tạo nền tảng tiếng Anh để giao tiếp và du lịch.'),
('student20','STU-020','Active','2026-02-13','UpperIntermediate',N'Nâng cao kỹ năng nói và viết để làm việc với đối tác nước ngoài.');

    INSERT dbo.[Student_Profile] (
        student_id, user_id, student_code, student_status, enrollment_date,
        current_level, learning_goal, created_at, updated_at, available_status
    )
    SELECT
        NEWID(), u.user_id, s.student_code, s.student_status, s.enrollment_date,
        s.current_level, s.learning_goal, @Now, @Now, 'Available'
    FROM @StudentSeed s
    INNER JOIN dbo.[User] u ON u.username = s.username
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.[Student_Profile] sp
        WHERE sp.user_id = u.user_id OR sp.student_code = s.student_code
    );


    -- 8) Eight English courses.
    -- The supplied CHECK constraint allows seven distinct level values,
    -- so the eight-course seed covers all seven levels and has two IELTS courses.
    DECLARE @CourseSeed TABLE (
        course_code varchar(50) NOT NULL,
        course_name nvarchar(200) NOT NULL,
        level varchar(30) NOT NULL,
        description nvarchar(1000) NULL,
        duration_weeks int NOT NULL,
        tuition_fee decimal(18,2) NOT NULL
    );

    INSERT @CourseSeed VALUES

('ENG-BEG-01',N'English Starter - Beginner','Beginner',N'Khóa nền tảng cho người mới bắt đầu: phát âm, từ vựng thiết yếu, mẫu câu và giao tiếp cơ bản.',12,2800000),
('ENG-ELE-01',N'English Foundation - Elementary','Elementary',N'Củng cố ngữ pháp căn bản, mở rộng từ vựng và phát triển nghe nói trong các tình huống hằng ngày.',12,3000000),
('ENG-INT-01',N'English Communication - Intermediate','Intermediate',N'Phát triển giao tiếp thực tế, thảo luận, thuyết trình ngắn và viết email cơ bản.',14,3600000),
('ENG-UPI-01',N'English for Work - Upper Intermediate','UpperIntermediate',N'Nâng cao độ chính xác và sự trôi chảy trong họp, thuyết trình, email và giao tiếp nơi công sở.',14,3900000),
('ENG-ADV-01',N'Advanced English Communication','Advanced',N'Phát triển tiếng Anh chuyên sâu cho thảo luận, thuyết trình, viết chuyên nghiệp và môi trường quốc tế.',14,4200000),
('IELTS-FND-01',N'IELTS Foundation - Target 5.5+','IELTS',N'Xây dựng nền tảng bốn kỹ năng IELTS, từ vựng học thuật và chiến lược làm bài cho mục tiêu 5.5+.',12,4800000),
('IELTS-INT-01',N'IELTS Intensive - Target 6.5+','IELTS',N'Luyện chuyên sâu Listening, Reading, Writing và Speaking theo định hướng mục tiêu 6.5+.',10,5200000),
('TOEIC-650-01',N'TOEIC Preparation - 650+','TOEIC',N'Luyện TOEIC Listening và Reading, bổ sung từ vựng và kỹ thuật làm bài cho mục tiêu 650+.',10,4500000);

    INSERT dbo.[Course] (
        course_id, course_code, course_name, level, description,
        duration_weeks, tuition_fee, created_at, updated_at, available_status
    )
    SELECT
        NEWID(), c.course_code, c.course_name, c.level, c.description,
        c.duration_weeks, c.tuition_fee, @Now, @Now, 'Available'
    FROM @CourseSeed c
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.[Course] co
        WHERE co.course_code = c.course_code
    );


    COMMIT;


    -- Seed verification
    SELECT r.role_name, COUNT(*) AS account_count
    FROM dbo.[User_Role] ur
    INNER JOIN dbo.[Role] r ON r.role_id = ur.role_id
    INNER JOIN dbo.[User] u ON u.user_id = ur.user_id
    WHERE u.username IN (SELECT username FROM @UserSeed)
    GROUP BY r.role_name
    ORDER BY r.role_name;

    SELECT level, COUNT(*) AS course_count
    FROM dbo.[Course]
    WHERE course_code IN (SELECT course_code FROM @CourseSeed)
    GROUP BY level
    ORDER BY level;


END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;

