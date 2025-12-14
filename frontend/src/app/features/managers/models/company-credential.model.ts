import { AccountingManager } from "./accounting-manager.model";
import { CompanyCredentialToken } from "./company-credential-token";

export interface CompanyCredential {
    id: number;
    rnc: string;
    companyName: string | null;
    pwd: string;
    tokenRequired: boolean;
    statusInd: boolean;
    accountingManagerId: number | null;
    selectedForProcessing: boolean;
    dateProcessed: string | null;
    accountingManager: AccountingManager | null;
    companyCredentialTokens: CompanyCredentialToken[];
    tokenFile?: any;
    TokenFileBase64?: string;
    fileType: string;
}



