using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FurniCraft.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }
    }
}
