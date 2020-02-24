
namespace GALibrary.Models
{
    public class UpdateSteps
    {

        public int UpdateId { get; set; }
        public int Type { get; set; }
        public int ProcessId { get; set; }
        public int Order { get; set; }

        public virtual UpdateGA Update { get; set; }
    }
}
