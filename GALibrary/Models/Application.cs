using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GALibrary.Models
{
    public class Application
    {
        public Application()
        {
            this.Command = new HashSet<Command>();
            this.Database = new HashSet<DatabaseGA>();
            this.File = new HashSet<File>();
            this.FileDelete = new HashSet<FileDelete>();
            this.FileDeleteFolder = new HashSet<FileDeleteFolder>();
            this.FileFolder = new HashSet<FileFolder>();
            this.Folder = new HashSet<Folder>();
            this.Service = new HashSet<Service>();
            this.Update = new HashSet<UpdateGA>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Ambiente")]
        [Required(ErrorMessage = "É necessário selecionar um ambiente")]
        public Nullable<int> EnvironmentId { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Ambiente")]
        public virtual Environment Environment { get; set; }

        public virtual ICollection<Command> Command { get; set; }
        public virtual ICollection<DatabaseGA> Database { get; set; }
        public virtual ICollection<File> File { get; set; }
        public virtual ICollection<FileDelete> FileDelete { get; set; }
        public virtual ICollection<FileDeleteFolder> FileDeleteFolder { get; set; }
        public virtual ICollection<FileFolder> FileFolder { get; set; }
        public virtual ICollection<Folder> Folder { get; set; }
        public virtual ICollection<Service> Service { get; set; }
        public virtual ICollection<UpdateGA> Update { get; set; }
    }
}
