import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MsalBroadcastService, MsalGuardConfiguration, MsalService, MSAL_GUARD_CONFIG } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus, RedirectRequest } from '@azure/msal-browser';
import { filter, firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent {

  loginDisplay = false;
  copyUrlStr = "Copy Url";

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private httpClient: HttpClient,
    private dialog: MatDialog) {
  }

  ngOnInit(): void {
    this.msalBroadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
      )
      .subscribe((result: EventMessage) => {
        console.log(result);
      });

    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None)
      )
      .subscribe(() => {
        this.setLoginDisplay();
      })
    this.login();
  }

  setLoginDisplay() {
    this.loginDisplay = this.authService.instance.getAllAccounts().length > 0;
  }

  getFiles() {
    
    
    
    // firstValueFrom(this.httpClient.get<DriveAccessMessage>('/drive/getgoogledriveaccessurl')).then(x => {
    //   const thisUrl = window.location.href;
    //   const redirect = thisUrl.substring(0, thisUrl.lastIndexOf('/')) + '/callback';
    //   const host = encodeURIComponent(redirect);
    //   const url = x.redirect + 'redirect_uri=' + host;
    //   window.location.replace(url);
    // });
    firstValueFrom(this.httpClient.get('/drive/hasaccess')).then(x => {
      console.log(x);
    });
  }

  login() {
    if (!this.loginDisplay) {
      if (this.msalGuardConfig.authRequest) {
        this.authService.loginRedirect({ ...this.msalGuardConfig.authRequest } as RedirectRequest);
      } else {
        this.authService.loginRedirect();
      }
    }
  }

  logout() {
    this.authService.logout();
  }

  // openDialog(item: Item): void {
  //   let dialogRef = this.dialog.open(FileDialogComponent, {
  //     data: { name: item.name }
  //   });
  //   dialogRef.afterClosed().subscribe(result => {
  //     this.getFiles();
  //   });
  // }
}

export class DriveAccessMessage {
  code:string = '';
  redirect:string = '';
}

