import { HttpClient } from '@angular/common/http';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-fileview',
  templateUrl: './fileview.component.html',
  styleUrls: ['./fileview.component.css']
})
export class FileviewComponent {
  @Input() file: FileItem = new FileItem();
  @Output() folderChange = new EventEmitter();
  @Output() onUrl = new EventEmitter<string>();

  constructor(private readonly httpClient: HttpClient) { }

  get isFolder() {
    return this.file.type === 'application/vnd.google-apps.folder';
  }

  async onClick() {
    if (this.isFolder) {
      this.folderChange.emit(this.file.id);
    } else {
      var x = await firstValueFrom(this.httpClient.get<SasUrl>('/drive/getdownloadurl?fileId=' + this.file.id));
      navigator.clipboard.writeText(x.url);
      this.onUrl.emit(x.url);
    }
  }
}

class SasUrl {
  url: string = '';
}

export class FileItem {
  name: string = '';
  id: string = '';
  type: string = '';
  binary: boolean = false;
  backedUp: boolean = false;
  downloading: number = -2;
  actions: string[] = [];
}
