import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';
import { MsalRedirectComponent } from '@azure/msal-angular';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CallbackComponent } from './callback/callback.component';
import { MainComponent } from './main/main.component';
import { MsalApplicationModule } from './msal-application.module';
import { FileviewComponent } from './fileview/fileview.component';

@NgModule({
  declarations: [
    AppComponent,
    CallbackComponent,
    MainComponent,
    FileviewComponent,
  
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    MatCardModule,
    MatInputModule,
    BrowserModule,
    AppRoutingModule,
    MatDialogModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    MatProgressBarModule,
    MatListModule,
    FormsModule,
    RouterModule.forRoot([]),
    MsalApplicationModule.forRoot(),
    BrowserAnimationsModule
  ],
  providers: [],
  bootstrap: [AppComponent, MsalRedirectComponent]
})
export class AppModule { }