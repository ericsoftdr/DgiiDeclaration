namespace DgiiIntegration.DTOs
{
    public class CompanyCredentialTokenDto
    {
        public int TokenId { get; set; }
        public string TokenValue { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }
}
