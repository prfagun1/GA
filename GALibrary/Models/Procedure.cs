using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class Procedure
    {
        public Procedure()
        {
            this.ProcedureSchedule = new HashSet<ProcedureSchedule>();
            this.ProcedureSteps = new HashSet<ProcedureSteps>();
        }

        public int Id { get; set; }

        [DisplayName("Nome")]
        [Required(ErrorMessage = "É necessário informar um nome")]
        [RegularExpression(@"[^\/\\\[\]\:\;\|\=\,]*$", ErrorMessage = "O nome da atualização não pode conter os caracteres . / \\ [ ] : ; | = ,")]
        public string Name { get; set; }

        [DisplayName("Descrição")]
        public string Description { get; set; }

        [DisplayName("Ativo")]
        public bool Enable { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Agendamento")]
        public virtual ICollection<ProcedureSchedule> ProcedureSchedule { get; set; }

        [DisplayName("Etapas")]
        public virtual ICollection<ProcedureSteps> ProcedureSteps { get; set; }
    }
}
