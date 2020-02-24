using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GALibrary.Models
{
    public class Command
    {
        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Servidor")]
        [Required(ErrorMessage = "É necessário selecionar um servidor")]
        public int ServerId { get; set; }

        [DisplayName("Comando")]
        [Required(ErrorMessage = "É necessário informar o comando")]
        [DataType(DataType.MultilineText)]
        [MaxLength(1000)]
        public string CommandText { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar uma aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Tipo do comando")]
        [Required(ErrorMessage = "É necessário indormar o tipo")]
        public Nullable<int> Type { get; set; }

        [DisplayName("Aplicação")]
        public virtual Application Application { get; set; }

        [DisplayName("Servidor")]
        public virtual Server Server { get; set; }
    }
}
