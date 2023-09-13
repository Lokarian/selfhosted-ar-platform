import {Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges} from '@angular/core';
import {
  ArClient, ArMemberDto,
  ArServerState,
  ArSessionDto,
  ArUserRole,
  StartArServerCommand,
  VideoSessionDto
} from "../../web-api-client";
import customProtocolCheck from "custom-protocol-check";
import {AuthorizeService} from "../../services/auth/authorize.service";
@Component({
  selector: 'app-ar-session-preview',
  templateUrl: './ar-session-preview.component.html',
  styleUrls: ['./ar-session-preview.component.css']
})
export class ArSessionPreviewComponent implements OnInit, OnChanges {

  @Input() session: ArSessionDto;
  @Output() close: EventEmitter<boolean> = new EventEmitter();
  @Output() join: EventEmitter<boolean> = new EventEmitter();

  public ArUserRole = ArUserRole
  public ArServerState = ArServerState;


  constructor(private arClient: ArClient,private authorizeService: AuthorizeService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.session.previousValue?.serverState != ArServerState.Running && changes.session.currentValue?.serverState == ArServerState.Running) {
      this.authorizeService.getAccessToken().subscribe(token => {
        customProtocolCheck(
          `arplatform://${location.host}/${this.session.baseSessionId}?token=${token}`,
          () => {
            this.join.emit(false);
          },
          () => {
            this.close.emit(true);
          }
        )
      });

    }
  }

  ngOnInit(): void {
  }

  joinSession(asHololens:boolean) {
    this.join.emit(asHololens);
  }

  public get displayMembers(): ArMemberDto[] {
    return this.session.members.filter(m => m.deletedAt == null);
  }

  startServer() {
    this.arClient.startArServer(new StartArServerCommand({arSessionId : this.session.baseSessionId})).subscribe();
  }
}
