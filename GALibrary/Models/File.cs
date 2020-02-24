using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class File
    {

        public File()
        {
            this.FileFolder = new HashSet<FileFolder>();
            this.FileHistory = new HashSet<FileHistory>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar uma aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Nome do arquivo")]
        public string FileName { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Arquivo apagado do servidor")]
        public Nullable<bool> FilesRemoved { get; set; }

        [DisplayName("Aplicação")]
        public virtual Application Application { get; set; }

        [DisplayName("Pastas")]
        public virtual ICollection<FileFolder> FileFolder { get; set; }

        public virtual ICollection<FileHistory> FileHistory { get; set; }
    }
}
