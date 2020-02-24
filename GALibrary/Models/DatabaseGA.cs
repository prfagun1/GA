
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class DatabaseGA
    {
        public DatabaseGA()
        {
            this.SQL = new HashSet<SQL>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }


        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Nome do banco")]
        [DataType(DataType.MultilineText)]
        public string DatabaseName { get; set; }

        [DisplayName("Porta")]
        [RegularExpression("\\d+", ErrorMessage = "Somente são permitidos números")]
        public Nullable<int> Port { get; set; }

        [DisplayName("Usuário")]
        public string DatabaseUser { get; set; }

        [DisplayName("Senha")]
        [DataType(DataType.Password)]
        public string DatabasePassword { get; set; }

        [DisplayName("Servidor/Instância Oracle")]
        [Required(ErrorMessage = "É necessário um servidor")]
        public string Server { get; set; }

        [DisplayName("Conexao Banco")]
        [Required(ErrorMessage = "É necessário uma conexão")]
        public int DatabaseConnectionId { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar uma aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Alterado por")]
        public System.DateTime Date { get; set; }

        [DisplayName("Aplicação")]
        public virtual Application Application { get; set; }

        [DisplayName("Conexão banco")]
        public virtual DatabaseConnection DatabaseConnection { get; set; }

        public virtual ICollection<SQL> SQL { get; set; }
    }
}
