import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { FormControl } from '@angular/forms';
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
  files: FileItemWrapper[] = [];
  caches: CacheItem[] = [];
  cacheControl = new FormControl();
  currentCache = new CacheItem();
  cacheCreationStatus = -2;
  currentFolder = 'root';
  cacheStatusResponse = new CacheStatusResponseWrapper(new CacheStatusResponse());

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private httpClient: HttpClient) {
  }

  set fontStyle(value: string) {
    console.log(value);
  }

  get showBackup() : boolean {
    return this.cacheStatusResponse.cacheId===this.currentCache.id && !this.cacheStatusResponse.cache.backingUp;
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

  async refresh() {
    var res = await firstValueFrom(this.httpClient.get<CheckStatusResponse>('/drive/checkstatus'));
    this.cacheCreationStatus = res.cacheGenerationStatus;
    this.caches = [];
    var now = new CacheItem();
    this.caches.push(now);
    res.cacheTimeStamps.forEach(n => {
      var item = new CacheItem();
      item.id = "" + n;
      var d = new Date(0); // The 0 there is the key, which sets the date to the epoch
      d.setUTCSeconds(n / 1000);
      item.name = d.toLocaleDateString() + " " + d.toLocaleTimeString();
      this.caches.push(item);
    });
    if (this.currentCache.id !== new CacheItem().id) {// default is "realtime" 
      this.cacheStatusResponse = new CacheStatusResponseWrapper(await firstValueFrom(this.httpClient.get<CacheStatusResponse>('/drive/checkcachestatus?cacheId=' + this.currentCache.id)));
    }

    await this.getFiles(this.currentFolder);
  }

  async setLoginDisplay() {
    this.loginDisplay = this.authService.instance.getAllAccounts().length > 0;
    if (this.loginDisplay) {
      if (await this.verifyAccess()) {
        await this.refresh();
      }
    }
  }

  async generateCache() {
    await firstValueFrom(this.httpClient.get('/drive/generatecache'));
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

  fileClick(item: FileItemWrapper) {
    console.log(item);
    if (item.isFolder) {
      this.getFiles(item.file.id);
    }
  }

  cacheClick(item: CacheItem) {
    this.currentCache = item;
    this.refresh();
  }

  async backup(id: string) {
    await firstValueFrom(this.httpClient.get('/drive/backupfile?id=' + id));
  }

  async verifyAccess(): Promise<boolean> {
    var dam = await firstValueFrom(this.httpClient.get<DriveAccessMessage>('/drive/postlogin'));
    if (!dam.hasAccess) {
      var redirect = window.location.href;
      redirect = redirect.substr(0, redirect.lastIndexOf('/')) + '/callback';
      redirect = encodeURIComponent(redirect);
      const url = dam.redirect + 'redirect_uri=' + redirect;
      document.location.href = url;
      return false;
    }
    return true;
  }

  root() {
    this.getFiles('root');
  }

  async backupCache(cacheId: string) {
    var ans = await firstValueFrom(this.httpClient.get<boolean>('/drive/backupcache?cacheId=' + cacheId));
    console.log(ans);
  }

  getFiles(folder: string) {
    this.currentFolder = folder;
    firstValueFrom(this.httpClient.get<FileItem[]>('/drive/getfiles?folderId=' + folder + "&cacheId=" + this.currentCache.id)).then(x => {
      this.files = x.map(x => new FileItemWrapper(x, y => { this.backup(y); }));
    });
  }
}

export class DriveAccessMessage {
  code: string = '';
  redirect: string = '';
  hasAccess: boolean = false;
}

class FileItemWrapper {
  public constructor(public file: FileItem, private backupFn: (id: string) => void) { }

  get name() { return this.file.name; }

  get isFolder() {
    return this.file.type === 'application/vnd.google-apps.folder';
  }

  get disabled(): boolean {
    return this.file.backedUp || this.file.downloading >= 0;
  }

  get status(): string {
    if (this.file.backedUp) {
      return "Backed up";
    }
    if (this.file.downloading >= 0) {
      var x = Math.round(this.file.downloading * 100);
      return "Backing up " + x + "%";
    }
    return "Back up";
  }

  backup() {
    if (!this.disabled) {
      this.backupFn(this.file.id);
    }
  }
}

class FileItem {
  name: string = '';
  id: string = '';
  type: string = '';
  binary: boolean = false;
  backedUp: boolean = false;
  downloading: number = -2;
}

class CacheItem {
  id: string = "realtime";
  name: string = "Google Drive"
}

class CheckStatusResponse {
  cacheGenerationStatus: number = -2;
  cacheTimeStamps: number[] = [];
}

class CacheStatusResponse {
  cacheId: string = '';
  totalFiles: number = 0;
  backedUpFiles: number = 0;
  totalFileSize: number = 0;
  backedUpFileSize: number = 0;
  backingUp: boolean = false;
}

class CacheStatusResponseWrapper {
  public constructor(public readonly cache: CacheStatusResponse) { }
  get cacheId() { return this.cache.cacheId; }
  get status(): string {
    var ans = "" + this.cache.backedUpFiles + "/" + this.cache.totalFiles + " files, or " + this.getSize(this.cache.backedUpFileSize) + "/" + this.getSize(this.cache.totalFileSize);
    return ans;
  }

  private getSize(size: number): string {
    if (size < 1024) {
      return size + " Bytes";
    }
    size /= 1024;
    if (size < 1024) {
      return Math.round(size * 10) / 10 + " KB";
    }
    size /= 1024;
    if (size < 1024) {
      return Math.round(size * 10) / 10 + " MB";
    }
    size /= 1024;
    return Math.round(size * 10) / 10 + " GB";
  }
}

