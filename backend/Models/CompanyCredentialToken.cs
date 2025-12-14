using System;
using System.Collections.Generic;

namespace DgiiIntegration.Models;

public partial class CompanyCredentialToken
{
    public int Id { get; set; }

    public int CompanyCredentialId { get; set; }

    public int TokenId { get; set; }

    public string TokenValue { get; set; } = null!;

    public bool Validated { get; set; }

    public virtual CompanyCredential CompanyCredential { get; set; } = null!;
}
