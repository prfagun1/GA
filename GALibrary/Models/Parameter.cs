using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GALibrary.Models
{
    public class Parameter
    {
        public int Id { get; set; }

        [DisplayName("Backup")]
        [Required(ErrorMessage = "É necessário informar o caminho do backup")]
        public string PathBackup { get; set; }

        [DisplayName("Atualizações")]
        [Required(ErrorMessage = "É necessário informar o caminho das atualizações")]
        public string PathUpdate { get; set; }

        [DisplayName("Log")]
        [Required(ErrorMessage = "É necessário informar o caminho dos logs")]
        public string PathLog { get; set; }

        [DisplayName("Temporário")]
        [Required(ErrorMessage = "É necessário informar o caminho dos arquivos temporários")]
        public string PathTemp { get; set; }

        [DisplayName("Cabeçalho e-mail")]
        [Required(ErrorMessage = "É necessário informar o caminho do cabeçalho dos e-mails.")]
        public string MailHeader { get; set; }

        [DisplayName("Rodapé e-mail")]
        [Required(ErrorMessage = "É necessário informar o caminho do rodapé dos e-mails.")]
        public string MailFooter { get; set; }

        [DisplayName("Aplicação")]
        [Required(ErrorMessage = "É necessário informar o nível do log")]
        public int LogLevelApplication { get; set; }

        [DisplayName("Atualização")]
        [Required(ErrorMessage = "É necessário informar o nível do log")]
        public int LogLevelUpdate { get; set; }

        [DisplayName("Alterado por")]
        public string User { get; set; }

        [DisplayName("Alterado em")]
        public System.DateTime Datetime { get; set; }

        [DisplayName("Tempo de retenção")]
        [Required(ErrorMessage = "É necessário informar o tempo de retenção")]
        public Nullable<int> RetentionTime { get; set; }

        [DisplayName("Itens por página")]
        [Required(ErrorMessage = "É necessário informar quantos itens aparecerão por página")]
        public Nullable<int> ItensPage { get; set; }
    }
}
