import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BackendService } from 'src/app/backend.service';

@Component({
  selector: 'app-callback',
  templateUrl: './callback.component.html',
  styleUrls: ['./callback.component.css']
})
export class CallbackComponent implements OnInit {

  constructor(private readonly backend: BackendService, private readonly router: Router) { }

  ngOnInit(): void {
    const url = '' + window.location.href;
    var redirect = window.location.href;
    redirect = redirect.substring(0, redirect.lastIndexOf('/')) + '/callback';
    if (url.includes('code')) {
      const code = this.getValueFromUrl('code');
      this.backend.finishDriveAccess(decodeURI(code), redirect).then(() => {
        this.router.navigate(['/']);
      });
    }
  }

  private getValueFromUrl(key: string): string {
    const url = '' + window.location.href;
    const kvpairs = ((url.split('?') as string[])[1] as string).split('&') as string[];
    return (kvpairs.find(x => x.startsWith(key)) as string).substr(key.length + 1);
  }
}
