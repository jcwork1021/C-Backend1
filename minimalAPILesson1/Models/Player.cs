using System.ComponentModel.DataAnnotations;

namespace minimalAPILesson1.Models
{
    public class Player
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Name is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = "";

        public int Age { get; set; }
        public int Year { get; set; }
        public string Section { get; set; } = "";
        public int Level { get; set; }

        public string RefreshToken { get; set; } = "";
        public DateTime RefreshTokenExpiryTime { get; set; }

    }
}
