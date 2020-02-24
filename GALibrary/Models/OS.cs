
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class OS
    {
        public OS()
        {
            this.Server = new HashSet<Server>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Comando para acesso")]
        [Required(ErrorMessage = "É necessário informar o comando para acesso")]
        public string AccessCommand { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Servidor")]
        public virtual ICollection<Server> Server { get; set; }
    }
}
