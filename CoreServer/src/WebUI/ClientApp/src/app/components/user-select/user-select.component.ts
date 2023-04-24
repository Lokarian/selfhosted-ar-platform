import {Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild} from '@angular/core';
import {AppUserDto, PaginatedListOfAppUserDto, UserClient} from "../../web-api-client";
import {BehaviorSubject, debounceTime, Subject} from "rxjs";
import {filter} from "rxjs/operators";
import {NgxPopperjsPlacements, NgxPopperjsTriggers} from "ngx-popperjs";
import {NgxPopperjsContentComponent} from "ngx-popperjs/lib/ngx-popperjs-content/ngx-popper-content.component";

@Component({
  selector: 'app-user-select',
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.css']
})
export class UserSelectComponent implements OnInit {
  public ngxPopperjsPlacements = NgxPopperjsPlacements;
  public popperTrigger = NgxPopperjsTriggers;

  @Input() multi = false;
  @Input() allowSelf = false;
  @Input() usePopper = true;
  @Input() popperStyle ="";
  @Output() usersSelected = new EventEmitter<AppUserDto[]>();
  @ViewChild("searchResultPopup") popper: NgxPopperjsContentComponent;
  @ViewChild("container") container: ElementRef;
  public searchTextSubject:BehaviorSubject<string|undefined> = new BehaviorSubject(undefined);
  public users: PaginatedListOfAppUserDto|undefined;
  public pageNumber = 1;
  public pageSize = 5;
  @Input() selectedUsers: AppUserDto[] = [];
  constructor(private userClient:UserClient) { }

  ngOnInit(): void {
    this.searchTextSubject.asObservable().pipe(debounceTime(500),filter(a=>a!=undefined)).subscribe((searchText)=> {
      this.searchForUsers();
    });
  }
  searchForUsers() {
    this.userClient.getAppUsersByPartialName(this.searchTextSubject.value,this.pageNumber,this.pageSize).subscribe((users)=> {
      this.popper.show();
      this.users = users;
    });
  }
  get popperWidth(){
    return this.container.nativeElement.offsetWidth+"px";
  }
  selectUser(user:AppUserDto){
    if(this.multi) {
      if(!this.selectedUsers.some(a=>a.id === user.id)) {
        this.selectedUsers.push(user);
      }
    }else {
      this.usersSelected.next([user]);
    }
  }

  removeUser(user: AppUserDto) {
    this.selectedUsers=this.selectedUsers.filter(a=>a.id !== user.id);
  }

  confirm() {
    this.usersSelected.next(this.selectedUsers);
    this.popper.hide();
    this.selectedUsers = [];
  }
}