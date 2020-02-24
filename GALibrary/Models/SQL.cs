using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class SQL
    {
        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar o nome")]
        public string Name { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Banco de dados")]
        [Required(ErrorMessage = "É necessário selecionar o banco de dados")]
        public int DatabaseId { get; set; }

        [DisplayName("Tipo")]
        [Required(ErrorMessage = "É necessário selecionar o tipo")]
        public int Type { get; set; }

        [DisplayName("Instrução SQL")]
        [DataType(DataType.MultilineText)]
        [MaxLength(10000)]
        public string SQLScript { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Arquivos removidos")]
        public Nullable<bool> FilesRemoved { get; set; }

        public virtual DatabaseGA Database { get; set; }
    }
}
