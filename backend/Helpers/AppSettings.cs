namespace DgiiIntegration.Helpers
{
    public class AppSettings
    {
        public string OriginCors { get; set; }
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int TokenExpiration { get; set; }
        public string DgiiUrlLogin { get; set; }
        public string ChromePath { get; set; }
        public string PathPdfOfEvidence { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string ToEmail { get; set; }

        

    }
}
