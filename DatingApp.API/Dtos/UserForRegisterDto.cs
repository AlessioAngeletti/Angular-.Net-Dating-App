using System;
using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos {
    public class UserForRegisterDto {
        public UserForRegisterDto () {
            Created = DateTime.Now;
            LastActive = DateTime.Now;
        }

        [Required]
        public string Username { get; set; }

        [Required]
        [StringLength (8, MinimumLength = 4, ErrorMessage = "Devi scegliere una password tra i 4 e gli 8 caratteri.")]
        public string Password { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string KnownAs { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Country { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }
    }
}