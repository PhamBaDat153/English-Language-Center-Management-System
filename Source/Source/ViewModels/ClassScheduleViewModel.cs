using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Source.ViewModels
{
    public class ClassScheduleViewModel
    {
        public Guid class_schedule_id { get; set; }

        [Required]
        public Guid class_id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khung giờ.")]
        public Guid time_slot_id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày học.")]
        public string week_day { get; set; }

        [StringLength(150, ErrorMessage = "Tên phòng không được vượt quá 150 ký tự.")]
        public string room_name { get; set; }

        public IEnumerable<SelectListItem> TimeSlots { get; set; }

        public IEnumerable<SelectListItem> WeekDays { get; set; }
    }
}
