using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GALibrary.Models
{
    public class PermissionGroup
    {
        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Domínio")]
        [Required(ErrorMessage = "É necessário informar o domínio")]
        public string Domain { get; set; }

        [DisplayName("Tipo de permissão")]
        [Required(ErrorMessage = "É necessário selecionar o tipo de permissão")]
        public int AccessType { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

    }
}
