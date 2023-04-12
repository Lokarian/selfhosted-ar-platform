import {Component, Input} from '@angular/core';
import {AppUser, FileType, OnlineStatus, UserClient, UserFilesClient} from "../../web-api-client";

@Component({
  selector: 'app-avatar[user]',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss']
})
export class AvatarComponent {
  @Input() user: AppUser;
  @Input() size: number = 4;
  @Input() showStatus: boolean = true;
  @Input() allowEdit: boolean = false;

  // give the enum to the template
  public OnlineStatus = OnlineStatus;

  constructor(private userFileClient: UserFilesClient, private userClient: UserClient) {
  }
  public triggerFileInput() {

  }
  public onFileSelected(event) {
    if (event.target.files.length > 0) {
      const file = event.target.files[0];
      this.userFileClient.upload(FileType.UserImage, {data:file,fileName:file.name}).subscribe(result => {
        console.log(result);
      });
    }
  }


}
