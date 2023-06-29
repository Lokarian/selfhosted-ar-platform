import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {
  ArClient, ArMemberDto,
  ArServerState,
  ArSessionDto,
  ArUserRole,
  StartArServerCommand,
  VideoSessionDto
} from "../../web-api-client";

@Component({
  selector: 'app-ar-session-preview',
  templateUrl: './ar-session-preview.component.html',
  styleUrls: ['./ar-session-preview.component.css']
})
export class ArSessionPreviewComponent implements OnInit {

  @Input() session: ArSessionDto;
  @Output() join: EventEmitter<boolean> = new EventEmitter();

  public ArUserRole = ArUserRole
  public ArServerState = ArServerState;


  constructor(private arClient: ArClient) {
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
