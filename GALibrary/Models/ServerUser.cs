using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class ServerUser
    {
        public ServerUser()
        {
            this.Server = new HashSet<Server>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar o nome")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Usuário")]
        [Required(ErrorMessage = "É necessário informar o usuário")]
        public string ServerUsername { get; set; }

        [DisplayName("Senha")]
        [DataType(DataType.Password)]
        public string ServerPassword { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        public virtual ICollection<Server> Server { get; set; }
    }
}
