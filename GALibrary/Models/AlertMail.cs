using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class AlertMail
    {

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("E-mail")]
        [Required(ErrorMessage = "É necessário informar o endereço de e-mail")]
        public string Email { get; set; }

        [DisplayName("Padrão")]
        [Required(ErrorMessage = "É necessário se este será o endereço padrão para envio de e-mails")]
        public bool Default { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }
    }
}
