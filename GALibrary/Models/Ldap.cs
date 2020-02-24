namespace GALibrary.Models
{
    public class Ldap
    {
        public string host { get; set; }
        public int port { get; set; }
        public string bindLogin { get; set; }
        public string bindPassword { get; set; }
        public string searchFilterUser { get; set; }
        public string searchFilterGroup { get; set; }
        public string searchBase { get; set; }
        public int ldapVersion { get; set; }
    }
}
