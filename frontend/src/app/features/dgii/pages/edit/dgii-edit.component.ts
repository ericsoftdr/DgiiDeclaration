import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CompanyCredentialService } from '../../services/dgii.service';
import { AccountingManager } from '../../models/accounting-manager.model';
import { CompanyCredential } from '../../models/company-credential.model';
import { MessageService } from 'primeng/api';
import { AccountingManagerService } from 'src/app/shared/services/accounting-maneger.service';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { CommonModule } from '@angular/common';
import { FieldsetModule } from 'primeng/fieldset';
import { CompanyCredentialToken } from '../../models/company-credential-token';
import { TableModule } from 'primeng/table';
@Component({
  selector: 'dgii-edit',
  templateUrl: './dgii-edit.component.html',
  imports: [CommonModule, CheckboxModule, FieldsetModule, ReactiveFormsModule, FileUploadModule, InputTextModule, InputSwitchModule, InputNumberModule,DropdownModule, FormsModule, ButtonModule, ToastModule, TableModule ],
 standalone: true,
  providers: [MessageService]
})
export class EditCompanyCredentialComponent implements OnInit {
  managers: AccountingManager[] = [];
  selectedFile: File | null = null;
  haveTokens = false;
  hideAddButton = false;
  selectedManager: AccountingManager;
  companyCredential: CompanyCredential = {
    id: 0,
    rnc: '',
    companyName: '',
    pwd: '',
    tokenRequired: false,
    statusInd: true,
    accountingManagerId: null,
    selectedForProcessing: false,
    dateProcessed: null,
    accountingManager: null,
    companyCredentialTokens: [],
    tokenFile: null,
    TokenFileBase64: '',
    fileType: null,
  };

  constructor(
    private companyCredentialService: CompanyCredentialService,
    private accountingManagerService: AccountingManagerService,
    private messageService: MessageService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.loadManagers();
    this.loadCompanyData();
  }

  loadManagers() {
    this.accountingManagerService.getAll().subscribe({
      next: (response) => {
        this.managers = response;
      },
      error: (err) => console.error(err)
    });
  }

  loadCompanyData() {
    const companyId = +this.route.snapshot.paramMap.get('id');
    this.companyCredentialService.getById(companyId).subscribe({
      next: (data) => {
        this.companyCredential = data;
        if(this.companyCredential.companyCredentialTokens.length > 0) this.haveTokens = true;
        this.selectedManager = this.managers.find(manager => manager.id === this.companyCredential.accountingManagerId);
      },
      error: (err) => console.error(err)
    });
  }

  updateCompany() {
        if (this.isValid()) {
        this.companyCredential.accountingManagerId = this.selectedManager.id;

        if (this.selectedFile) {
            const reader = new FileReader();
            reader.onload = () => {
                const base64String = reader.result as string;
                this.companyCredential.TokenFileBase64 = base64String.split(',')[1];
                this.companyCredential.fileType = this.selectedFile.type;
                this.sendRequest();
            };
            reader.readAsDataURL(this.selectedFile);
        } else {
            this.companyCredential.TokenFileBase64 = this.companyCredential.tokenFile;
            this.sendRequest();
        }
        }
    }

    sendRequest(){
        this.companyCredentialService.update(this.companyCredential.id, this.companyCredential).subscribe(data => {
              this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Compañia actualizada' });
              this.redirectToList();
          });
    }

  isValid(): boolean {
    if (this.companyCredential.rnc.trim().length < 9) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Ingrese un Rnc valido' });
      return false;
    }
    if (this.companyCredential.companyName.trim().length < 1) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Indique el nombre de la compañia' });
      return false;
    }
    if (this.companyCredential.pwd.trim().length < 1) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Ingrese la contraseña' });
      return false;
    }
    if (!this.selectedManager) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Seleccione un asesor' });
      return false;
    }

    if(this.companyCredential.tokenRequired && this.companyCredential.companyCredentialTokens.length < 1){
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Registre los tokens' });
        return false
    }

    if(this.companyCredential.tokenRequired) return this.areTokensValid();

    return true;
  }

  redirectToList() {
    this.router.navigate(['companies-list']);
  }

  downloadFile() {
    if(this.companyCredential.tokenFile === undefined || this.companyCredential.tokenFile === null){
        return;
    }
    const linkSource = `data:${this.companyCredential.fileType};base64,${this.companyCredential.tokenFile}`;
    const downloadLink = document.createElement("a");
    const fileName = `Tokens${this.companyCredential.companyName}.${this.companyCredential.fileType.split('/')[1]}`;
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
    }
    onFileChange(event: Event) {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
          this.selectedFile = input.files[0];
        }
    }

    areTokensValid(): boolean {
        const seenTokenIds: { [key: number]: boolean } = {};
        const seenTokenValues: { [key: string]: boolean } = {};

        for (const token of this.companyCredential.companyCredentialTokens) {
            if (/^0+$/.test(token.tokenValue)) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Token inválido' });
                return false;
            }
            if (seenTokenIds[token.tokenId]) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: `Token No. duplicado: ${token.tokenId}` });
                return false;
            }
            seenTokenIds[token.tokenId] = true;

            if (seenTokenValues[token.tokenValue]) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: `TokenValue duplicado: ${token.tokenValue}` });
                return false;
            }
            seenTokenValues[token.tokenValue] = true;
        }

        return true;
    }

    onRowEditInit(token: CompanyCredentialToken) {
        this.companyCredential.companyCredentialTokens[token.tokenId - 1] = { ...token };
    }

    onRowEditSave(token: CompanyCredentialToken) {
        if (token.tokenValue.length >= 4) {
            this.companyCredential.companyCredentialTokens[token.tokenId - 1] = token;
            this.messageService.add({ severity: 'success', summary: 'Agregado', detail: 'Token Agregado' });
        } else {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'El token es invalido' });
        }
    }

    onRowEditCancel(token: CompanyCredentialToken, index: number) {
        this.companyCredential.companyCredentialTokens.splice(index, 1);
    }
    addRow(): void{
        for (let index = 0; index < 40; index++) {
            this.companyCredential.companyCredentialTokens.push({id: 0, tokenId: this.companyCredential.companyCredentialTokens.length + 1, tokenValue: '00000', companyCredentialId: 0, validated: false});
        }
        this.hideAddButton = true;
    }
}
