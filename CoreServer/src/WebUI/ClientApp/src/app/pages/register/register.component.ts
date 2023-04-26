import {Component, OnInit} from '@angular/core';
import {FormControl, FormGroup, Validators} from "@angular/forms";
import {RegisterUserCommand, UserClient} from "../../web-api-client";
import {NotificationService} from "../../services/notification.service";
import {AuthorizeService} from "../../services/auth/authorize.service";
import {ActivatedRoute, Router} from "@angular/router";

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {

  public registerForm = new FormGroup({
    userName: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required]),
    password: new FormControl('', [Validators.required, Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$/)]),
    confirmPassword: new FormControl('', [Validators.required])
  });

  constructor(private userClient: UserClient, private route: ActivatedRoute, private router: Router, private notificationService: NotificationService, private authService: AuthorizeService) {
  }

  ngOnInit(): void {
  }

  register() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    if (this.registerForm.controls.password.invalid) {
      this.notificationService.add({
        severity: "error",
        message: "Password must be at least 8 characters long, contain at least 1 uppercase and lowercase letter, 1 number and 1 special character"
      });
      return;
    }
    if (this.registerForm.value.password != this.registerForm.value.confirmPassword) {
      this.notificationService.add({severity: "error", message: "Passwords do not match"});
      return;
    }
    var registerCommand = new RegisterUserCommand(this.registerForm.value);
    this.userClient.register(registerCommand).subscribe(result => {
      this.authService.saveAccessToken(result);
      var redirectUrl = this.route.snapshot.queryParams['redirectUrl'] || '/';
      this.router.navigate([redirectUrl]);
    });
  }
}
