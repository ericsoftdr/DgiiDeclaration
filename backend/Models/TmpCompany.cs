using System;
using System.Collections.Generic;

namespace DgiiIntegration.Models;

public partial class TmpCompany
{
    public string? Rnc { get; set; }

    public string? CompanyName { get; set; }

    public string? Pwd { get; set; }

    public bool? TokenRequired { get; set; }

    public int? AccountingManagerId { get; set; }
}
