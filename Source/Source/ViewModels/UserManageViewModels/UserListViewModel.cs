using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Source.ViewModels
{
    public class UserListViewModel
    {
        public List<User> filteredUserList { get; set; }

        public int totalUsers { get; set; }

        public int totalStudents { get; set; }

        public int totalActiveUsers { get; set; }

        public int totalInactiveUsers { get; set; }

    }
}