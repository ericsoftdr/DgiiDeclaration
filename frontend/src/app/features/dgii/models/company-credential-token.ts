import { CompanyCredential } from "./company-credential.model";

export interface CompanyCredentialToken {
    id: number;
    companyCredentialId: number;
    tokenId: number;
    tokenValue: string;
    validated: boolean;
    //companyCredential: CompanyCredential;
}

