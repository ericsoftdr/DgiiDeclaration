import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CompanyCredential } from '../models/company-credential.model';
import { ApiService } from 'src/app/core/services/api.service';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class CompanyCredentialService extends ApiService {
  endpoint = 'Companies';

  constructor(http: HttpClient) {
    super(http);
  }

  getAll(): Observable<CompanyCredential[]> {
    return this.get<CompanyCredential[]>(this.endpoint);
  }

  getById(id: number): Observable<CompanyCredential> {
    return this.get<CompanyCredential>(`${this.endpoint}/${id}`);
  }

  create(companyCredential: CompanyCredential): Observable<CompanyCredential> {
    return this.post<CompanyCredential>(this.endpoint, companyCredential);
  }

  declarationInZero(rncList: string[]): Observable<CompanyCredential[]> {
    return this.post<CompanyCredential[]>('DeclarationInZero/Companies', rncList);
  }

  update(id: number, companyCredential: CompanyCredential): Observable<CompanyCredential> {
    return this.put<CompanyCredential>(`${this.endpoint}/${id}`, companyCredential);
  }

  deleteCompany(id: number): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
