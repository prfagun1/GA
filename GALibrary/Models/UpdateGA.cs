using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class UpdateGA
    {
        public UpdateGA()
        {
            this.FileHistory = new HashSet<FileHistory>();
            this.UpdateSteps = new HashSet<UpdateSteps>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [RegularExpression(@"[^\/\\\[\]\:\;\|\=\,]*$", ErrorMessage = "O nome da atualização não pode conter os caracteres . / \\ [ ] : ; | = ,")]
        [Required(ErrorMessage = "É necessário informar o nome da atualização")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário selecionar uma aplicação")]
        public int ApplicationId { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Agendar para")]
        public Nullable<System.DateTime> Schedule { get; set; }

        [DisplayName("Aprovado")]
        public bool Approved { get; set; }

        [DisplayName("Data da aprovação")]
        public Nullable<System.DateTime> AprovedDate { get; set; }

        [DisplayName("Status")]
        public Nullable<int> Status { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        public Nullable<int> Demanda { get; set; }

        [DisplayName("Arquivos removidos")]
        public Nullable<bool> FilesRemoved { get; set; }

        [DisplayName("Atualização manual")]
        public bool Manual { get; set; }

        [DisplayName("Template")]
        public bool Template { get; set; }

        [DisplayName("Receber e-mail com log da atualização?")]
        public bool AlertUser { get; set; }

        [DisplayName("Alerta de e-mail")]
        [Required(ErrorMessage = "É necessário selecionar um e-mail para alertar")]
        public int AlertmailId { get; set; }

        [DisplayName("Aplicação")]
        public virtual Application Application { get; set; }

        [DisplayName("Histórico de arquivos")]
        public virtual ICollection<FileHistory> FileHistory { get; set; }

        [DisplayName("Etapas da atualização")]
        public virtual ICollection<UpdateSteps> UpdateSteps { get; set; }

        [DisplayName("Aviso por e-mail")]
        public virtual AlertMail AlertMail { get; set; }

    }
}


public class CurrentDateAttribute : ValidationAttribute
{
    public CurrentDateAttribute(){ }

    /*
    public override bool IsValid(object value)
    {
        if (value == null) return false;
        var dt = (DateTime)value;
        if (dt >= DateTime.Now)
        {
            return true;
        }
        return false;
    }*/
}