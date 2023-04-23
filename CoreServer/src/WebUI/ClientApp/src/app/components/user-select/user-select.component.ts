import {Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild} from '@angular/core';
import {AppUserDto, UserClient} from "../../web-api-client";
import {Subject} from "rxjs";

@Component({
  selector: 'app-user-select',
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.css']
})
export class UserSelectComponent implements OnInit {
  @Input() multi = false;
  @Output() usersSelected = new EventEmitter<AppUserDto[]>();
  public searchText = '';
  public searchTextUpdate = new Subject<string>();
  constructor(private userClient:UserClient) { }

  ngOnInit(): void {

  }
  public onSearchInputChanged(event: any) {
    //with debounceTime(500) call userClient.search

  }
}
