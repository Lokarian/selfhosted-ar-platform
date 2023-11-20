import {Component, Input} from '@angular/core';
import {AppUserDto, FileType, OnlineStatus, UpdateAppUserCommand, UserClient, UserFilesClient} from "../../web-api-client";
import {CurrentUserService} from "../../services/user/current-user.service";

@Component({
  selector: 'app-avatar[user]',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss']
})
export class AvatarComponent {
  @Input() user: AppUserDto|undefined;
  @Input() size: number = 4;
  @Input() showStatus: boolean = true;
  @Input() allowEdit: boolean = false;

  // give the enum to the template
  public OnlineStatus = OnlineStatus;

  constructor(private userFileClient: UserFilesClient, private userClient: UserClient, private curentUserServise: CurrentUserService) {
  }

  public onFileSelected(event) {
    if (event.target.files.length > 0) {
      const file = event.target.files[0];
      this.userFileClient.upload(FileType.UserImage, {data: file, fileName: file.name}).subscribe(result => {
        let currentUser = this.curentUserServise.user;
        currentUser.imageId = result.id;
        this.userClient.update(new UpdateAppUserCommand({...this.curentUserServise.user,userImage: result})).subscribe(user=> {
          this.curentUserServise.setUser(user);
        });
      });
    }
  }

  get avatarUrl() {
    if (this.user.imageId) {
      return "/api/userfiles/Get/" + this.user.imageId;
    }
    return null;
  }

  get avatarColor() {
    let hash = 0;
    for (let i = 0; i < this.user.userName.length; i++) {
      hash = this.user.userName.charCodeAt(i) + ((hash << 5) - hash);
    }
    let color = '#';
    for (let i = 0; i < 3; i++) {
      let value = (hash >> (i * 8)) & 0xFF;
      color += ('00' + value.toString(16)).substr(-2);
    }
    return color;
  }

  get avatarText() {
    let text = this.user.userName;
    if (text.indexOf(' ') > -1) {
      text = text.split(' ').map(word => word[0]).join('');
    }
    if (text.length > 2) {
      text = text.substring(0, 2);
    }
    return text.toUpperCase();
  }

}
