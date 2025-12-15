import { Component, OnInit, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CompanyCredentialService } from '../../services/dgii.service';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';
import { AccountingManager } from '../../models/accounting-manager.model';
import { AccountingManagerService } from 'src/app/shared/services/accounting-maneger.service';
import { CompanyCredential } from '../../models/company-credential.model';
import { FileUploadModule, UploadEvent } from 'primeng/fileupload';
import { FieldsetModule } from 'primeng/fieldset';
import { CommonModule } from '@angular/common';
import { CompanyCredentialToken } from '../../models/company-credential-token';
import { TableModule } from 'primeng/table';
@Component({
  selector: 'dgii-new',
  templateUrl: './dgii-new.component.html',
  imports: [CheckboxModule, ReactiveFormsModule, InputTextModule, FieldsetModule, FileUploadModule, InputSwitchModule, InputNumberModule,DropdownModule, FormsModule, ButtonModule, ToastModule, CommonModule, TableModule ],
  standalone: true,
  providers: [MessageService]
})
export class CreateCompanyCredentialComponent implements OnInit{
    @ViewChildren('tokenInput') tokenInputs!: QueryList<ElementRef>;

    managers: AccountingManager[] = [];
    selectedTokens: CompanyCredentialToken[] = [];
    selectedManager: AccountingManager;
    selectedFile: File | null = null;
    isLoadingCompanyName: boolean = false;
    companyCredential: CompanyCredential = {
        id: 0,
        rnc: '',
        companyName: '',
        pwd: '',
        tokenRequired: false,
        statusInd: true,
        accountingManagerId: null,
        selectedForProcessing: true,
        dateProcessed: null,
        accountingManager: null,
        companyCredentialTokens: [],
        tokenFile: null,
        fileType: null,
    };


    constructor(private companyCredentialService: CompanyCredentialService, private accountingManagerService: AccountingManagerService, private messageService: MessageService,private router: Router) {

    }

    ngOnInit(): void {
        this.loadManagers();
    }

    loadManagers() {
        this.accountingManagerService.getAll().subscribe({
            next: (response) => {
            this.managers = response;
            },
            error: (err) => console.error(err)
        });
    }

    saveCompany() {
        if (this.isValid()) {
            this.companyCredential.companyCredentialTokens = this.selectedTokens;
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
                this.sendRequest();
            }
        }
    }



    sendRequest(){
        this.companyCredentialService.create(this.companyCredential).subscribe(data => {
            if(data != null){
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Compañia creada' });
                this.redirectToList();
            }
        });
    }

    isValid(): boolean{
        if(!this.isValidRnc(this.companyCredential.rnc.trim())){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Ingrese un Rnc valido' });
            return false
        };
        if(this.companyCredential.companyName.trim().length < 1){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Indique el nombre de la compañia' });
            return false;
        }
        if(this.companyCredential.pwd.trim().length < 1){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Ingrese la contraseña' });
            return false;
        }
        if(!this.selectedManager){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Seleccione un asesor' });
            return false
        };
        if(this.companyCredential.tokenRequired && this.selectedTokens.length < 1){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Registre los tokens' });
            return false
        }

        if(this.companyCredential.tokenRequired) return this.areTokensValid();

        return true;
    }

    onFileChange(event: Event) {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
        this.selectedFile = input.files[0];
        }
    }

    isValidRnc(rnc: string): boolean{
        if (!rnc || rnc.length === 0) {
            return false;
        }
        /*
        if (rnc.length !== 11) {
            return false;
        }*/
        const rncRegex = /^[0-9]+$/;
        if (!rncRegex.test(rnc)) {
            return false;
        }

        return true;
    }

    redirectToList(){
        this.router.navigate(['companies-list']);
    }

    areTokensValid(): boolean {
        const seenTokenIds: { [key: number]: boolean } = {};
        const seenTokenValues: { [key: string]: boolean } = {};

        for (const token of this.selectedTokens) {
            // Validar que el token no esté vacío o solo contenga espacios
            if (!token.tokenValue || token.tokenValue.trim().length === 0) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Todos los tokens deben tener un valor' });
                return false;
            }

            // Validar que el token no sea solo ceros
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

    onlyNumbers(event: KeyboardEvent): boolean {
        const charCode = event.which ? event.which : event.keyCode;
        // Solo permite números (0-9)
        if (charCode < 48 || charCode > 57) {
            event.preventDefault();
            return false;
        }
        return true;
    }

    focusNextInput(currentIndex: number): void {
        const inputsArray = this.tokenInputs.toArray();
        const nextIndex = currentIndex + 1;

        if (nextIndex < inputsArray.length) {
            inputsArray[nextIndex].nativeElement.focus();
        }
    }

    onTokenRequiredChange(isRequired: boolean): void {
        if (isRequired) {
            // Cargar automáticamente los 40 tokens vacíos
            this.selectedTokens = [];
            for (let index = 0; index < 40; index++) {
                this.selectedTokens.push({
                    id: 0,
                    tokenId: index + 1,
                    tokenValue: '',
                    companyCredentialId: 0,
                    validated: false
                });
            }
        } else {
            // Limpiar los tokens si se desactiva
            this.selectedTokens = [];
        }
    }

    onRncBlur(): void {
        const rnc = this.companyCredential.rnc.trim();

        // Validar que el RNC sea válido antes de hacer la consulta
        if (!this.isValidRnc(rnc)) {
            return;
        }

        // Deshabilitar el campo de nombre de compañía
        this.isLoadingCompanyName = true;

        // Consultar el RNC
        this.companyCredentialService.getRncInfo(rnc).subscribe({
            next: (rncInfo) => {
                // Si encontró información y tiene nombre, asignarlo
                if (rncInfo && rncInfo.nombre) {
                    this.companyCredential.companyName = rncInfo.nombre;
                }
                // Habilitar el campo nuevamente
                this.isLoadingCompanyName = false;
            },
            error: (err) => {
                console.error('Error al consultar RNC:', err);
                // Habilitar el campo incluso si hay error
                this.isLoadingCompanyName = false;
                // No mostrar mensaje de error al usuario ya que puede ser normal que no encuentre
            }
        });
    }
}
