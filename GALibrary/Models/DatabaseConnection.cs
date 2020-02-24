
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class DatabaseConnection
    {
        public DatabaseConnection()
        {
            this.Database = new HashSet<DatabaseGA>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Comando para importação")]
        [DataType(DataType.MultilineText)]
        [Required(ErrorMessage = "É necessário informar o comando")]
        public string SQLImportCommand { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Banco de dados")]
        public virtual ICollection<DatabaseGA> Database { get; set; }
    }
}
