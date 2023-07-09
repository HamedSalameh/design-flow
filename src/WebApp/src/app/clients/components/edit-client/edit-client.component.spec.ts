import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgxsModule } from '@ngxs/store';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { SharedModule } from 'src/app/shared/shared.module';
import { ClientState } from 'src/app/state/client-state/client-state.state';

import { EditClientComponent } from './edit-client.component';

describe('EditClientComponent', () => {
  let component: EditClientComponent;
  let fixture: ComponentFixture<EditClientComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ EditClientComponent ],
      imports: [ HttpClientTestingModule, SharedModule, NgxsModule.forRoot([ClientState]) ],
      providers: [DynamicDialogRef]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditClientComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
