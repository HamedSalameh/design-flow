import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconButtonComponent } from './components/icon-button/icon-button.component';
import { TableComponent } from './components/table/table.component';
import { TableModule } from 'primeng/table';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FormInputComponent } from './components/form-input/form-input.component';

@NgModule({
  declarations: [
    IconButtonComponent,
    TableComponent,
    FormInputComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,

    TableModule
  ],
  exports: [
    IconButtonComponent,
    TableComponent,
    FormInputComponent
  ]
})
export class SharedModule { }
