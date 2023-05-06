import {BehaviorSubject} from "rxjs";
import {ChatSessionDto} from "../../web-api-client";
import {SessionFacade} from "../session-facade.service";

export abstract class CapabilitySessionFacade<T>{
  private sessionSubject: BehaviorSubject<T[]> = new BehaviorSubject<T[]>([]);

  constructor(private sessionFacade:SessionFacade ) {
    this
  }
}
