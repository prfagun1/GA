
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class ProcedureSchedule
    {
        public int Id { get; set; }

        [DisplayName("Procedimento")]
        [Required(ErrorMessage = "Selecione um procedimento")]
        public int ProcedureID { get; set; }

        [DisplayName("Data de execução")]
        [Required(ErrorMessage = "Informe a data de execução")]
        public System.DateTime Schedule { get; set; }

        [DisplayName("Status")]
        public int Status { get; set; }

        [DisplayName("Criado por")]
        public string User { get; set; }

        [DisplayName("Criado em")]
        public System.DateTime Date { get; set; }

        [DisplayName("Procedimento")]
        public virtual Procedure Procedure { get; set; }
    }
}
