
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class FileDeleteFolder
    {

        public int FileDeleteId { get; set; }
        public int ApplicationId { get; set; }
        public int FolderId { get; set; }

        public virtual Application Application { get; set; }
        public virtual FileDelete FileDelete { get; set; }
        public virtual Folder Folder { get; set; }

    }
}
