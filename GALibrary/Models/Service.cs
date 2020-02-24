
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class Service
    {
        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar o nome")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar uma aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Servidor")]
        [Required(ErrorMessage = "É necessário selecionar um servidor")]
        public int ServerId { get; set; }

        [DisplayName("Comando para iniciar")]
        [Required(ErrorMessage = "É necessário cadastrar o comando para iniciar")]
        public string CommandStart { get; set; }

        [DisplayName("Comando para parar")]
        [Required(ErrorMessage = "É necessário cadastrar o comando para parar")]
        public string CommandStop { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }


        public virtual Application Application { get; set; }
        public virtual Server Server { get; set; }
    }
}
