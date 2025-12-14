import { RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { AppLayoutComponent } from "./layout/app.layout.component";
import { DgiiComponent } from './features/dgii/pages/list/dgii.component';
import { CreateCompanyCredentialComponent } from './features/dgii/pages/new/dgii-new.component';
import { EditCompanyCredentialComponent } from './features/dgii/pages/edit/dgii-edit.component';
import { managerComponent } from './features/managers/pages/list/manager.component';
import { CreateManagerComponent } from './features/managers/pages/new/manager-new.component';
import { EditManagerComponent } from './features/managers/pages/edit/manager-edit.component';

@NgModule({
    imports: [
        RouterModule.forRoot([
            {
                path: '', component: AppLayoutComponent,
                children: [
                    //{ path: 'companies-list', loadChildren: () => import('../app/features/dgii/pages/list/dgii.component').then(m => m.DgiiComponent) },
                    {
                        path: 'companies-list',
                        component: DgiiComponent
                    },
                    {
                        path: 'companie-create',
                        component: CreateCompanyCredentialComponent
                    },
                    {
                        path: 'companies/edit/:id',
                        component: EditCompanyCredentialComponent
                    },
                    {
                        path: 'managers-list',
                        component: managerComponent
                    },
                    {
                        path: 'manager-create',
                        component: CreateManagerComponent
                    },
                    {
                        path: 'manager/edit/:id',
                        component: EditManagerComponent
                    },
                ]
            },
            { path: '**', redirectTo: '/notfound' },
        ], { scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled', onSameUrlNavigation: 'reload' })
    ],
    exports: [RouterModule]
})
export class AppRoutingModule {
}
