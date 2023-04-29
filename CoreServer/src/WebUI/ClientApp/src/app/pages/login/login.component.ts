import {Component, OnInit} from '@angular/core';
import {LoginUserCommand, UserClient} from "../../web-api-client";
import {AuthorizeService} from "../../services/auth/authorize.service";
import {FormControl, FormGroup, Validators} from "@angular/forms";
import {ActivatedRoute, Router} from "@angular/router";

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  constructor(private userClient: UserClient, private authorizeService: AuthorizeService, private route: ActivatedRoute, private router: Router) {
  }

  public loginForm = new FormGroup({
    userName: new FormControl('', [Validators.required]),
    password: new FormControl('', [Validators.required])
  });


  ngOnInit(): void {
  }

  public login() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.userClient.login(new LoginUserCommand(this.loginForm.value)).subscribe(result => {
      this.authorizeService.saveAccessToken(result);
      //navigate to home page or redirectUrl if it is set
      var redirectUrl = this.route.snapshot.queryParams['redirectUrl'] || '/';
      this.router.navigate([redirectUrl]);
    }, error => {
      console.log(error);
    });
  }

  

}
