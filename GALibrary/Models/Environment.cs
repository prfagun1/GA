using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class Environment
    {

        public Environment()
        {
            this.Application = new HashSet<Application>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }


        [DisplayName("Aplicação")]
        public virtual ICollection<Application> Application { get; set; }
    }
}
