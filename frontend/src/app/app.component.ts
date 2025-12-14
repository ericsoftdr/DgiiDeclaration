import { AfterViewInit, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { PrimeNGConfig } from 'primeng/api';
import { LoadingService } from './core/services/loading.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html'
})
export class AppComponent implements OnInit, AfterViewInit {
    isLoading$ = this.loadingService.loading$;
    constructor( private cdr: ChangeDetectorRef, private primengConfig: PrimeNGConfig, private loadingService: LoadingService) { }

    ngOnInit() {
        this.primengConfig.ripple = true;
    }
    ngAfterViewInit() {
        this.isLoading$.subscribe(() => {
          this.cdr.detectChanges(); // Forzar la detección de cambios
        });
      }
}
