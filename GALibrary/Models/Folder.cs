using System;
using System.Collections.Generic;

namespace GALibrary.Models
{
    public class Folder
    {
        public Folder()
        {
            this.FileDeleteFolder = new HashSet<FileDeleteFolder>();
            this.FileFolder = new HashSet<FileFolder>();
        }

        public int Id { get; set; }
        public string Path { get; set; }
        public Nullable<int> ApplicationId { get; set; }

        public virtual Application Application { get; set; }
        public virtual ICollection<FileDeleteFolder> FileDeleteFolder { get; set; }
        public virtual ICollection<FileFolder> FileFolder { get; set; }
    }
}
