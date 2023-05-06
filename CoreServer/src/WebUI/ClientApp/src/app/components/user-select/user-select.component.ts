import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild
} from '@angular/core';
import {AppUserDto, PaginatedListOfAppUserDto, UserClient} from "../../web-api-client";
import {BehaviorSubject, debounceTime} from "rxjs";
import {filter, map} from "rxjs/operators";
import {NgxPopperjsPlacements, NgxPopperjsTriggers} from "ngx-popperjs";
import {NgxPopperjsContentComponent} from "ngx-popperjs/lib/ngx-popperjs-content/ngx-popper-content.component";
import {UserFacade} from "../../services/user/user-facade.service";

@Component({
  selector: 'app-user-select',
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.css']
})
export class UserSelectComponent implements OnInit, AfterViewInit {
  public ngxPopperjsPlacements = NgxPopperjsPlacements;
  public popperTrigger = NgxPopperjsTriggers;

  @Input() multi = false;
  @Input() allowSelf = false;
  @Input() usePopper = true;
  @Input() popperStyle = "";
  @Input() preselectedUserIds: string[] = [];
  @Output() usersSelected = new EventEmitter<AppUserDto[]>();
  @ViewChild("searchResultPopup") popper: NgxPopperjsContentComponent;
  @ViewChild("container") container: ElementRef;
  public searchTextSubject: BehaviorSubject<string | undefined> = new BehaviorSubject(undefined);
  public users: PaginatedListOfAppUserDto | undefined;
  public pageNumber = 1;
  public pageSize = 5;
  public selectedUsers: AppUserDto[] = [];
  public popperWidth = 0;
  constructor(private userClient: UserClient, private userFacade: UserFacade, private cdr: ChangeDetectorRef) {
  }


  ngOnInit(): void {
    this.searchTextSubject.asObservable().pipe(debounceTime(500), filter(a => a != undefined)).subscribe((searchText) => {
      this.searchForUsers();
    });
    this.userFacade.getUsers$(this.preselectedUserIds).subscribe((users) => {
      this.selectedUsers = users;
    });
  }

  ngAfterViewInit(): void {
    this.popperWidth = this.container.nativeElement.offsetWidth;
    this.cdr.detectChanges();
  }
  searchForUsers() {
    this.userClient.getAppUsersByPartialName(this.searchTextSubject.value, this.pageNumber, this.pageSize).subscribe((users) => {
      this.popper.show();
      this.users = users;
    });
  }


  selectUser(user: AppUserDto) {
    if (this.multi) {
      if (!this.selectedUsers.some(a => a.id === user.id)) {
        this.selectedUsers.push(user);
      }
    } else {
      this.usersSelected.next([user]);
    }
  }

  removeUser(user: AppUserDto) {
    this.selectedUsers = this.selectedUsers.filter(a => a.id !== user.id);
  }

  confirm() {
    this.usersSelected.next(this.selectedUsers);
    this.popper.hide();
    this.selectedUsers = [];
  }

}
