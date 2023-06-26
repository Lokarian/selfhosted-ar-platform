import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {
  ArClient,
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
  @Output() join: EventEmitter<ArSessionDto> = new EventEmitter();

  public ArUserRole = ArUserRole
  public ArServerState = ArServerState;

  constructor(private arClient: ArClient) {
  }

  ngOnInit(): void {
  }

  joinSession() {
    this.join.emit(this.session);
  }


  startServer() {
    this.arClient.startArServer(new StartArServerCommand({arSessionId : this.session.baseSessionId}))
  }
}
