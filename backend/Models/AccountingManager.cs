using System;
using System.Collections.Generic;

namespace DgiiIntegration.Models;

public partial class AccountingManager
{
    public int Id { get; set; }

    public string ManagerName { get; set; } = null!;

    public string BusinessName { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<CompanyCredential> CompanyCredentials { get; set; } = new List<CompanyCredential>();
}
