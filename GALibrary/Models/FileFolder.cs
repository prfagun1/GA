
using System.ComponentModel;

namespace GALibrary.Models
{
    public class FileFolder
    {
        [DisplayName("Arquivo")]
        public int FileId { get; set; }

        [DisplayName("Aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Pastas")]
        public int FolderId { get; set; }

        public virtual Application Application { get; set; }
        public virtual File File { get; set; }

        [DisplayName("Pastas selecionadas")]
        public virtual Folder Folder { get; set; }
    }
}
