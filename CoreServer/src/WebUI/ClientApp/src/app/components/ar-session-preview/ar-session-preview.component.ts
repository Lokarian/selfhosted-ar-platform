import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {ArSessionDto, ArUserRole, VideoSessionDto} from "../../web-api-client";

@Component({
  selector: 'app-ar-session-preview',
  templateUrl: './ar-session-preview.component.html',
  styleUrls: ['./ar-session-preview.component.css']
})
export class ArSessionPreviewComponent implements OnInit {

  @Input() session: ArSessionDto;
  @Output() join: EventEmitter<ArSessionDto> = new EventEmitter();

  public ArUserRole = ArUserRole

  constructor() {
  }

  ngOnInit(): void {
  }

  joinSession() {
    this.join.emit(this.session);
  }


}
