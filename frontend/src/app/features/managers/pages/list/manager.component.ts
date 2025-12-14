import { Component, OnInit, ViewChild } from '@angular/core';
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

@Component({
  selector: 'manager-list',
  templateUrl: './manager.component.html',
  standalone: true,
  imports: [TableModule, CommonModule, ConfirmDialogModule, ToastModule, FormsModule, ButtonModule, InputTextModule, TagModule,  MenuModule, PanelMenuModule,OverlayPanelModule,TableModule,
    DropdownModule, MultiSelectModule,],
  providers: [ConfirmationService, MessageService]
})
export class managerComponent implements OnInit {
 @ViewChild('op', { static: false }) overlayPanel: OverlayPanel;

  filterText: string = '';
  filteredManager: AccountingManager[] = [];
  managers: AccountingManager[] = [];
  selectedManagers: AccountingManager[] = [];
  selectedManager: AccountingManager;
  displayDetailDialog: boolean = false;

  constructor(private accountingManagerService: AccountingManagerService,private router: Router,private confirmationService: ConfirmationService, private messageService: MessageService) {}

    ngOnInit() {
        this.loadManagers();
    }

    loadManagers() {
        this.accountingManagerService.getAll().subscribe({
        next: (response) => {
            this.managers = response;
            this.filteredManager = this.managers;
        },
        error: (err) => console.error(err)
        });
    }

    showMenu(event: Event, manager: AccountingManager) {
        this.selectedManager = manager;
        this.overlayPanel.toggle(event);
    }

    editCompany(id: number) {
        this.router.navigate(['/manager/edit', id]);
    }

    deleteCompany(event: Event) {
        this.confirmationService.confirm({
            target: event.target as EventTarget,
            message: `¿Quieres eliminar este asesor [${this.selectedManager.managerName}]?`,
            header: 'Confirmación',
            icon: 'pi pi-info-circle',
            acceptButtonStyleClass:"p-button-danger p-button-text",
            rejectButtonStyleClass:"p-button-text p-button-text",
            acceptIcon:"none",
            rejectIcon:"none",

            accept: () => {
                this.accountingManagerService.deleteManager(this.selectedManager.id).subscribe({
                    next: (response) => {
                        this.messageService.add({ severity: 'info', summary: 'Confirmado', detail: 'Asesor eliminada' });
                        this.loadManagers();
                    },
                    error: (err) => console.error(err)
                    });

            }
        });
    }

    filterData() {
        if (this.filterText) {
        this.filteredManager = this.managers.filter(field =>
            Object.values(field).some(value =>
            value?.toString().toLowerCase().includes(this.filterText.toLowerCase())
            )
        );
        } else {
            this.filteredManager = this.managers;
        }
    }

    redirectToCreate(){
        this.router.navigate(['manager-create']);
    }

    showDetails(manager: AccountingManager) {
        this.displayDetailDialog = false;
        this.selectedManager = manager;
        this.displayDetailDialog = true;
    }
}
