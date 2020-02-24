//Dicas de anotações: https://docs.microsoft.com/pt-br/aspnet/mvc/overview/older-versions/mvc-music-store/mvc-music-store-part-6

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class Server
    {
        public Server()
        {
            this.Command = new HashSet<Command>();
            this.Service = new HashSet<Service>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar o nome")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Descrição")]
        public string Description { get; set; }

        [DisplayName("Usuário servidor")]
        [Required(ErrorMessage = "É necessário informar o usuário do servidor")]
        public Nullable<int> ServerUserId { get; set; }

        [DisplayName("Sistema Operacional")]
        [Required(ErrorMessage = "É necessário informar o sistema operacional")]
        public int OSId { get; set; }

        [DisplayName("Ativo")]
        [Required(ErrorMessage = "É necessário informar se estará ativo")]
        public bool Enable { get; set; }

        [DisplayName("Alterado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Alterado por")]
        public string User { get; set; }

        public virtual ICollection<Command> Command { get; set; }
        public virtual OS OS { get; set; }
        public virtual ServerUser ServerUser { get; set; }
        public virtual ICollection<Service> Service { get; set; }
    }
}
