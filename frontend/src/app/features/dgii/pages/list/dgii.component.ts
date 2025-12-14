import { Component, OnInit, ViewChild } from '@angular/core';
import { CompanyCredentialService } from '../../services/dgii.service';
import { CompanyCredential } from '../../models/company-credential.model';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MenuModule } from 'primeng/menu';
import { PanelMenuModule } from 'primeng/panelmenu';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { OverlayPanel, OverlayPanelModule } from 'primeng/overlaypanel';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { AccountingManager } from '../../models/accounting-manager.model';
import { AccountingManagerService } from 'src/app/shared/services/accounting-maneger.service';
import { Router } from '@angular/router';
import { DetailDialogComponent } from '../../components/detail/dgii-detail.component';

@Component({
  selector: 'dgii-list',
  templateUrl: './dgii.component.html',
  standalone: true,
  imports: [TableModule, CommonModule, ConfirmDialogModule, ToastModule, FormsModule, ButtonModule, InputTextModule, TagModule,  MenuModule, PanelMenuModule,OverlayPanelModule,TableModule,
    DropdownModule, MultiSelectModule,DetailDialogComponent ],
  providers: [ConfirmationService, MessageService]
})
export class DgiiComponent implements OnInit {
 @ViewChild('op', { static: false }) overlayPanel: OverlayPanel;
  companyCredentials: CompanyCredential[] = [];
  filteredCredentials: CompanyCredential[] = [];
  selectedCredential: CompanyCredential;
  filterText: string = '';
  managers: AccountingManager[] = [];
  selectedManagers: AccountingManager[] = [];
  selectedCompany: CompanyCredential;
  selectedCompanies: CompanyCredential[] = [];
  selectedManager: AccountingManager;
  displayDetailDialog: boolean = false;
  canDownload= false;

  constructor(private companyCredentialService: CompanyCredentialService, private accountingManagerService: AccountingManagerService,private router: Router,private confirmationService: ConfirmationService, private messageService: MessageService) {}

    ngOnInit() {
        this.loadCompanyCredentials();
        this.loadManagers();
    }

    loadCompanyCredentials() {
        this.companyCredentialService.getAll().subscribe({
        next: (response) => {
            this.companyCredentials = response;
            this.filteredCredentials = response;
        },
        error: (err) => console.error(err)
        });
    }
    loadManagers() {
        this.accountingManagerService.getAll().subscribe({
        next: (response) => {
            this.managers = response;
        },
        error: (err) => console.error(err)
        });
    }
    filterData(agents?: AccountingManager[]) {
        if(agents){
            this.selectedManagers = agents;
            this.filteredCredentials = this.companyCredentials.filter(c =>
                agents.some(agent => agent.managerName === c.accountingManager.managerName)
            );
            return;
        }
        if (this.filterText) {
        this.filteredCredentials = this.companyCredentials.filter(credential =>
            Object.values(credential).some(value =>
            value?.toString().toLowerCase().includes(this.filterText.toLowerCase())
            )
        );
        } else {
        this.filteredCredentials = this.companyCredentials;
        }
    }

    getSeverity(status: boolean) {
        return status ? 'success' : 'danger';
    }

    showMenu(event: Event, credential: CompanyCredential) {
        this.selectedCredential = credential;
        this.canDownload = this.selectedCredential.tokenFile != null ? true : false;
        this.overlayPanel.toggle(event);
    }

    editCompany(id: number) {
        this.router.navigate(['/companies/edit', id]);
    }

    deleteCompany(event: Event) {
        this.confirmationService.confirm({
            target: event.target as EventTarget,
            message: `¿Quieres eliminar esta compañia [${this.selectedCredential.companyName}]?`,
            header: 'Confirmación',
            icon: 'pi pi-info-circle',
            acceptButtonStyleClass:"p-button-danger p-button-text",
            rejectButtonStyleClass:"p-button-text p-button-text",
            acceptIcon:"none",
            rejectIcon:"none",

            accept: () => {
                this.companyCredentialService.deleteCompany(this.selectedCredential.id).subscribe({
                    next: (response) => {
                        this.messageService.add({ severity: 'info', summary: 'Confirmado', detail: 'Compañia eliminada' });
                        this.loadCompanyCredentials();
                    },
                    error: (err) => console.error(err)
                    });

            }
        });
    }

    processCompanies(event: Event) {
        this.confirmationService.confirm({
            target: event.target as EventTarget,
            message: `¿Seguro que desea procesar las compañias seleccionadas?`,
            header: 'Confirmación',
            icon: 'pi pi-info-circle',
            acceptButtonStyleClass:"p-button-sucess p-button-text",
            rejectButtonStyleClass:"p-button-text p-button-text",
            acceptIcon:"none",
            rejectIcon:"none",

            accept: () => {
                const rncArray = this.selectedCompanies.map(company => company.rnc);

                this.companyCredentialService.declarationInZero(rncArray).subscribe({
                    next: (response) => {
                        this.messageService.add({ severity: 'info', summary: 'Confirmado', detail: 'Compañías procesadas' });
                        this.loadCompanyCredentials();
                    },
                    error: (err) => console.error(err)
                });

            }
        });
    }

    redirectToCreate(){
        this.router.navigate(['companie-create']);
    }

    showDetails(company: CompanyCredential) {
        this.displayDetailDialog = false;
        this.selectedCompany = company;
        this.selectedManager = company.accountingManager;
        this.displayDetailDialog = true;
    }


    downloadFile(company: CompanyCredential) {
        if(company.tokenFile === undefined || company.tokenFile === null){
            return;
        }
        const linkSource = `data:${company.fileType};base64,${company.tokenFile}`;
        const downloadLink = document.createElement("a");
        const fileName = `Tokens${company.companyName}.${company.fileType.split('/')[1]}`;
        downloadLink.href = linkSource;
        downloadLink.download = fileName;
        downloadLink.click();
    }
}
