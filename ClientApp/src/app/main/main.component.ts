import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
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
  files: FileItem[] = [];

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private httpClient: HttpClient) {
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
    if (this.loginDisplay) {
      this.root();
    }
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

  itemClick(item: FileItem) {
    console.log(item);
    if (item.type==='application/vnd.google-apps.folder') {
      this.getFiles(item.id);
    }
  }

  root() {
    this.getFiles('root');
  }

  getFiles(folder:string) {
    firstValueFrom(this.httpClient.get<FileItem[]>('/drive/getfiles?folder=' + folder)).then(x => {
      this.files = x;
    });
  }
}

export class DriveAccessMessage {
  code: string = '';
  redirect: string = '';
}

class FileItem {
  name: string = '';
  id: string = '';
  type: string = '';
}

