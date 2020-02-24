using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GALibrary.Models
{
    public class LoginData
    {
        [Required]
        [Display(Name = "Usuário")]
        [Key]
        public string Username { get; set; }

        [Required, DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        [Display(Name = "Lembrar?")]
        public bool RememberMe { get; set; }
    }
}
