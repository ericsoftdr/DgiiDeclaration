using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DgiiIntegration.Models;

public partial class CompanyCredential
{
    public int Id { get; set; }

    public string Rnc { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string Pwd { get; set; } = null!;

    public bool TokenRequired { get; set; }

    public bool StatusInd { get; set; }

    public int? AccountingManagerId { get; set; }

    public bool SelectedForProcessing { get; set; }

    public DateTime? DateProcessed { get; set; }

    public byte[]? TokenFile { get; set; }
    [NotMapped]
    public string? TokenFileBase64 { get; set; }

    public string? FileType { get; set; }

    public bool NaturalPerson { get; set; }

    public virtual AccountingManager? AccountingManager { get; set; }

    public virtual ICollection<CompanyCredentialToken> CompanyCredentialTokens { get; set; } = new List<CompanyCredentialToken>();
}
