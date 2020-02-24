
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class FileDelete
    {
        public FileDelete()
        {
            this.FileDeleteFolder = new HashSet<FileDeleteFolder>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar o nome")]
        public string Name { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar a aplicação")]
        public int ApplicationId { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Arquivos")]
        [MaxLength(5000)]
        [Required(ErrorMessage = "É necessário informar ao menos um arquivo")]
        public string FilesDirectory { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Aplicação")]
        public virtual Application Application { get; set; }

        [DisplayName("Pastas selecionadas")]
        public virtual ICollection<FileDeleteFolder> FileDeleteFolder { get; set; }
    }
}
