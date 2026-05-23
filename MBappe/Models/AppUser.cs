using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBappe.Models
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public byte[] PasswordHash { get; set; } = [];

        public byte[] PasswordSalt { get; set; } = [];

        public UserRole Role { get; set; } = UserRole.Employee;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }
    }
}
