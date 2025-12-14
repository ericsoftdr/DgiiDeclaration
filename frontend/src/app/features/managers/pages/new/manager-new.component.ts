import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
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
import { TableModule } from 'primeng/table';
@Component({
  selector: 'manager-new',
  templateUrl: './manager-new.component.html',
  imports: [CheckboxModule, ReactiveFormsModule, InputTextModule, FieldsetModule, FileUploadModule, InputSwitchModule, InputNumberModule,DropdownModule, FormsModule, ButtonModule, ToastModule, CommonModule, TableModule ],
  standalone: true,
  providers: [MessageService, AccountingManagerService]
})
export class CreateManagerComponent implements OnInit{
    manager: AccountingManager = {
        id: 0,
        managerName: '',
        businessName: '',
        createdDate: new Date().toISOString(),
    };


    constructor(private accountingManagerService: AccountingManagerService, private messageService: MessageService,private router: Router) {

    }

    ngOnInit(): void {
    }

    saveManager() {
        if (this.isValid()) {
            this.accountingManagerService.create(this.manager).subscribe(data => {
                if(data != null){
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Asesor creado' });
                    this.redirectToList();
                }
            });
        }
    }


    isValid(): boolean{
        if(this.manager.managerName.trim().length < 1){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Indique el nombre del asesor' });
            return false;
        }
        if(this.manager.businessName.trim().length < 1){
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Ingrese el nombre de la compañia' });
            return false;
        }

        return true;
    }



    redirectToList(){
        this.router.navigate(['managers-list']);
    }

}
