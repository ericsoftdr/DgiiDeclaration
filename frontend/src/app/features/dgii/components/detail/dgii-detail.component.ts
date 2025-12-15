import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { CompanyCredentialService } from '../../services/dgii.service';
import { CompanyCredential } from '../../models/company-credential.model';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MenuModule } from 'primeng/menu';
import { DialogModule } from 'primeng/dialog';
import { PanelMenuModule } from 'primeng/panelmenu';
import { OverlayPanel, OverlayPanelModule } from 'primeng/overlaypanel';
import { DropdownModule } from 'primeng/dropdown';
import { CheckboxModule } from 'primeng/checkbox';
import { InputSwitchModule } from 'primeng/inputswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';
import { AccountingManager } from '../../models/accounting-manager.model';
import { AccountingManagerService } from 'src/app/shared/services/accounting-maneger.service';
import { FieldsetModule } from 'primeng/fieldset';

@Component({
  selector: 'dgii-detail',
  templateUrl: './dgii-detail.component.html',
  standalone: true,
  imports: [TableModule, CommonModule, FormsModule, ButtonModule, InputTextModule, DialogModule, TagModule,  MenuModule, PanelMenuModule,OverlayPanelModule,TableModule,
    DropdownModule, MultiSelectModule,CheckboxModule, InputSwitchModule, InputNumberModule, FieldsetModule ],
})
export class DetailDialogComponent {
    @Input() visible: boolean = false;
    @Input() companyCredential: CompanyCredential;
    @Input() selectedManager: AccountingManager;
    @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

    closeDialog() {
      this.visible = false;
      this.visibleChange.emit(this.visible);
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
}
