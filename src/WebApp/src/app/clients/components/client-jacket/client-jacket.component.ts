import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
} from '@angular/core';
import { Store } from '@ngxs/store';
import {
  EditMode,
  ViewMode,
} from 'src/app/state/client-state/client-state.actions';
import { ClientState } from 'src/app/state/client-state/client-state.state';
import { Client } from '../../models/client.model';
import { ClientsServiceService } from '../../services/clients-service.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { EditClientComponent } from '../edit-client/edit-client.component';

@Component({
  selector: 'app-client-jacket',
  templateUrl: './client-jacket.component.html',
  styleUrls: ['./client-jacket.component.scss'],
  providers: [DialogService],
})
export class ClientJacketComponent implements OnDestroy {
  clientId: any;
  editMode: boolean = false;

  ref: DynamicDialogRef | undefined;

  @Input() client: Client | undefined;
  @Output() CloseClientJacket: EventEmitter<any> = new EventEmitter();

  private unsubscribe$: Subject<void> = new Subject();

  constructor(
    private clientsService: ClientsServiceService,
    private store: Store,
    private dialogService: DialogService
  ) {
    this.store
      .select(ClientState.selectedClient)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((clientId: any) => {
        if (clientId) {
          this.clientId = clientId;
          this.clientsService
            .getClient(clientId)
            .subscribe((client: Client) => {
              this.clientId = clientId;
            });
        }
      });

    this.store
      .select(ClientState.applicationState)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((editMode: any) => {
        this.editMode = editMode.editMode;
      });
  }

  ngOnInit(): void {
    console.debug('[ClientJacketComponent] [ngOnInit]', this.clientId);
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  onClose(): void {
    console.debug('[ClientJacketComponent] [onClose]', this.clientId);
    this.client = undefined;
    this.store.dispatch(new ViewMode());
    this.CloseClientJacket.emit();
  }

  onEdit(): void {
    console.debug('[ClientJacketComponent] [onEdit]', this.clientId);
    if (this.clientId) {
      this.store.dispatch(new EditMode(this.clientId));
      this.ref = this.dialogService.open(EditClientComponent, {
        header: $localize`:@@Clients.EditClient:Edit Client`,
        contentStyle: { 'max-height': '500px', overflow: 'auto' },
        baseZIndex: 10000,
        data: {
          clientId: this.clientId,
        },
      });
    }
  }

  onCloseEditClient(): void {
    console.debug('[ClientJacketComponent] [onCloseEditClient]', this.clientId);
    this.store.dispatch(new ViewMode());
  }

  onShare(): void {
    console.debug('[ClientJacketComponent] [onShare]', this.clientId);
  }
}
