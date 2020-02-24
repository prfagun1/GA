
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class ProcedureSteps
    {
        public int ProcedureID { get; set; }
        public int Type { get; set; }
        public int ProcessId { get; set; }
        public int Order { get; set; }

        public virtual Procedure Procedure { get; set; }
    }
}
