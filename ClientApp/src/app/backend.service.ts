import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { DriveAccessMessage } from './main/main.component';

@Injectable({
  providedIn: 'root'
})
export class BackendService {
  

  constructor(private httpClient: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  async finishDriveAccess(code: string, redirect: string) {
    var req =  new DriveAccessMessage();
    req.code = code;
    req.redirect = redirect;
    return await this.post('setgoogledriveaccess', req);
  }

  private async post<T>(fn: string, req: any): Promise<T> {
    const url = this.baseUrl + 'drive/' + fn;
    const res = lastValueFrom(this.httpClient.post(url, req)) as T;
    return res;
  }
}
