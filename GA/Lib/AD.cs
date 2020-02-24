using GALibrary.Models;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace GA.Lib
{

    public class AD
    {

        public static List<Claim> ValidaAcesso(Ldap ldap, LoginData loginData, List<PermissionGroup> grupos) {
            List<Claim> claims = new List<Claim>();

            LdapConnection ldapConnection = ldapConnection = new LdapConnection();
            ldapConnection.Connect(ldap.host, ldap.port);
            ldapConnection.Bind(ldap.ldapVersion, ldap.bindLogin, ldap.bindPassword);

            String usuarioDN = GetDNUsuario(ldapConnection, ldap, loginData);
            if (usuarioDN == null) return null;

            Boolean validaUsuarioSenha = ValidaUsuarioSenha(ldap, loginData, usuarioDN);

            if (validaUsuarioSenha)
            {
                ldapConnection.Disconnect();
                return ValidaPermissaoGrupo(ldap, loginData, usuarioDN, grupos);
            }

            ldapConnection.Disconnect();
            return null;
        }

        private static List<Claim> GetClaimType(int permission) {
            List<Claim> claims = new List<Claim>();

            switch (permission)
            {
                case 0:
                    claims.Add(new Claim("Administração", "true"));
                    claims.Add(new Claim("Cadastro", "true"));
                    claims.Add(new Claim("Aprovação", "true"));
                    claims.Add(new Claim("Atualização", "true"));
                    claims.Add(new Claim("Vizualização", "true"));
                    break;
                case 1:
                    claims.Add(new Claim("Cadastro", "true"));
                    claims.Add(new Claim("Aprovação", "true"));
                    claims.Add(new Claim("Atualização", "true"));
                    claims.Add(new Claim("Vizualização", "true"));
                    break;
                case 2:
                    claims.Add(new Claim("Aprovação", "true"));
                    claims.Add(new Claim("Atualização", "true"));
                    claims.Add(new Claim("Vizualização", "true"));
                    break;
                case 3:
                    claims.Add(new Claim("Atualização", "true"));
                    claims.Add(new Claim("Vizualização", "true"));
                    break;
                case 4:
                    claims.Add(new Claim("Vizualização", "true"));
                    break;
            }

            return claims;
        }

        private static List<Claim> ValidaPermissaoGrupo(Ldap ldap, LoginData loginData, String usuarioDN, List<PermissionGroup> grupos) {
            LdapConnection ldapConnection = ldapConnection = new LdapConnection();
            ldapConnection.Connect(ldap.host, ldap.port);
            ldapConnection.Bind(ldap.ldapVersion, ldap.bindLogin, ldap.bindPassword);

            LdapSearchConstraints cons = new LdapSearchConstraints();
            String[] atributos = new String[] { "member" };

            List<Claim> claims = new List<Claim>();

            try
            {
                foreach (PermissionGroup grupo in grupos)
                {
                    String groupDN = GetDNGrupo(ldapConnection, ldap, grupo.Name);
                    LdapSearchResults searchResults = ldapConnection.Search(groupDN, LdapConnection.SCOPE_BASE, null, atributos, false, cons);

                    var nextEntry = searchResults.Next();
                    nextEntry.getAttributeSet();

                    try
                    {
                        if (nextEntry.getAttribute("member").StringValueArray.Where(x => x == usuarioDN).Count() > 0){
                            claims.AddRange(GetClaimType(grupo.AccessType));
                        }
                    }
                    catch { }
                }
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("AD", "Erro ao validar permissao do usuario: " + erro.ToString(), 1, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
            }

            ldapConnection.Disconnect();
            return claims;
        }

        private static Boolean ValidaUsuarioSenha(Ldap ldap, LoginData loginData, String usuarioDN) {

            LdapConnection ldapConnection = ldapConnection = new LdapConnection();

//Valida usuário e senha
            try
            {
                ldapConnection.Connect(ldap.host, ldap.port);
                ldapConnection.Bind(ldap.ldapVersion, usuarioDN, loginData.Password);
                
                return true;

            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("AD", "Erro ao validar usuario e senha: " + erro.ToString(), 1, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
                return false;
            }

        }

//Busca o caminho completo do usuário no AD
        private static String GetDNUsuario(LdapConnection ldapConnection, Ldap ldap, LoginData loginData) {
            String filter = String.Format(ldap.searchFilterUser, loginData.Username);
            var result = ldapConnection.Search(ldap.searchBase, LdapConnection.SCOPE_SUB, filter, null, false);

            try
            {
                return result.First().DN;
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("AD", "Erro ao buscar DN do usuario: " + erro.ToString(), 1, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
                return null;
            }

        }

        private static String GetDNGrupo(LdapConnection ldapConnection, Ldap ldap, String grupo)
        {
            String filter = String.Format(ldap.searchFilterGroup, grupo);
            var result = ldapConnection.Search(ldap.searchBase, LdapConnection.SCOPE_SUB, filter, null, false);

            try
            {
                return result.First().DN;
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("AD", "Erro ao buscar DN do grupo: " + erro.ToString(), 1, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
                return null;
            }

        }

    }
}
