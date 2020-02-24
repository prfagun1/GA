using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class FileHistory
    {
        
        public int FileId { get; set; }

        [DisplayName("Nome arquivo")]
        public string FileName { get; set; }

        [DisplayName("Pastas")]
        public string Folder { get; set; }

        [DisplayName("Atualização")]
        public int UpdateId { get; set; }

        [DisplayName("Criado em")]
        public Nullable<System.DateTime> Date { get; set; }

        [DisplayName("Tamanho")]
        public Nullable<long> Size { get; set; }

        public virtual File File { get; set; }
        public virtual UpdateGA Update { get; set; }
    }
}
