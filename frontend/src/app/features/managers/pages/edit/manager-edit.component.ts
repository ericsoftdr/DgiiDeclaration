import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
  selector: 'manager-edit',
  templateUrl: './manager-edit.component.html',
  imports: [CommonModule, CheckboxModule, FieldsetModule, ReactiveFormsModule, FileUploadModule, InputTextModule, InputSwitchModule, InputNumberModule,DropdownModule, FormsModule, ButtonModule, ToastModule, TableModule ],
 standalone: true,
  providers: [MessageService]
})
export class EditManagerComponent implements OnInit {
    manager: AccountingManager = {
        id: 0,
        managerName: '',
        businessName: '',
        createdDate: new Date().toISOString(),
    };


    constructor(private accountingManagerService: AccountingManagerService, private messageService: MessageService,private router: Router, private route: ActivatedRoute) {

    }

    ngOnInit(): void {
        this.loadManagerData();
    }

    loadManagerData() {
        const id = +this.route.snapshot.paramMap.get('id');
        this.accountingManagerService.getById(id).subscribe({
          next: (data) => {
            this.manager = data;
          },
          error: (err) => console.error(err)
        });
      }

    saveManager() {
        if (this.isValid()) {
            this.accountingManagerService.update(this.manager.id, this.manager).subscribe(data => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Asesor actualizado' });
                this.redirectToList();
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



