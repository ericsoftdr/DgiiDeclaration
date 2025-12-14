import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from 'src/app/core/services/api.service';
import { HttpClient } from '@angular/common/http';
import { AccountingManager } from 'src/app/features/dgii/models/accounting-manager.model';

@Injectable({
  providedIn: 'root'
})
export class AccountingManagerService extends ApiService {
  endpoint = 'AccountingManager';

  constructor(http: HttpClient) {
    super(http);
  }

  getAll(): Observable<AccountingManager[]> {
    return this.get<AccountingManager[]>(this.endpoint);
  }

  getById(id: number): Observable<AccountingManager> {
    return this.get<AccountingManager>(`${this.endpoint}/${id}`);
  }

  create(AccountingManager: AccountingManager): Observable<AccountingManager> {
    return this.post<AccountingManager>(this.endpoint, AccountingManager);
  }

  update(id: number, AccountingManager: AccountingManager): Observable<AccountingManager> {
    return this.put<AccountingManager>(`${this.endpoint}/${id}`, AccountingManager);
  }

  deleteManager(id: number): Observable<void> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
