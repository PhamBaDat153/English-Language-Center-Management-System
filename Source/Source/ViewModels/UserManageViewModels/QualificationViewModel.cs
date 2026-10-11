using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
    public class QualificationViewModel
    {
        public Guid QualificationId { get; set; }
        public Guid TeacherId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool PermanentDelete { get; set; }
        public string QualificationName { get; set; }
        public string Institution { get; set; }
        [DataType(DataType.Date)] public DateTime? IssuedDate { get; set; }
        [DataType(DataType.Date)] public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AvailableStatus { get; set; }
    }
}