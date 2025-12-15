namespace DgiiIntegration.DTOs
{
    public class CompanyCredentialCreateDto
    {
        public string Rnc { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Pwd { get; set; } = string.Empty;
        public bool TokenRequired { get; set; }
        public bool StatusInd { get; set; }
        public bool SelectedForProcessing { get; set; }
        public int AccountingManagerId { get; set; }
        public string? TokenFileBase64 { get; set; }
        public string? FileType { get; set; }
        public List<CompanyCredentialTokenDto> CompanyCredentialTokens { get; set; } = new List<CompanyCredentialTokenDto>();
    }
}
