using GALibrary.Models;
using Novell.Directory.Ldap;
using System;
using System.Linq;

namespace GALibrary
{
    public class GAAD
    {


        public static UserAD GetADUserData(Ldap ldap, String username)
        {
            UserAD userAD = new UserAD();
            userAD.FullName = string.Empty;
            userAD.Mail = string.Empty;

            using (LdapConnection ldapConnection  = new LdapConnection()) {
                ldapConnection.Connect(ldap.host, ldap.port);
                ldapConnection.Bind(ldap.ldapVersion, ldap.bindLogin, ldap.bindPassword);
                String filter = String.Format(ldap.searchFilterUser, username);
                var result = ldapConnection.Search(ldap.searchBase, LdapConnection.SCOPE_SUB, filter, null, false);
                var resultAD = result.First();

                userAD.Mail = resultAD.getAttribute("mail").StringValue;
                userAD.FullName = resultAD.getAttribute("cn").StringValue;
            }

            return userAD;
        }
    }
}
