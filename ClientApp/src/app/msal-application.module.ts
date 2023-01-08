import { HttpBackend, HttpClient, HTTP_INTERCEPTORS } from '@angular/common/http';
import { APP_INITIALIZER, Injectable, InjectionToken, NgModule } from '@angular/core';
import { MsalBroadcastService, MsalGuard, MsalGuardConfiguration, MsalInterceptor, MsalInterceptorConfiguration, MsalModule, MsalService, MSAL_GUARD_CONFIG, MSAL_INSTANCE, MSAL_INTERCEPTOR_CONFIG } from '@azure/msal-angular';
import { BrowserCacheLocation, InteractionType, IPublicClientApplication, LogLevel, PublicClientApplication } from '@azure/msal-browser';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';

export const APPLICATION_SCOPE = new InjectionToken<string>('APPLICATION_SCOPE');

function initializerFactory(service: MsalConfigService): () => Promise<boolean> {
    const promise = service.getConfig('/drive/msal');
    return () => promise;
}

function scopeFacotry(service: MsalConfigService): string {
    return service.config.scope;
}

function MSALInstanceFactory(service: MsalConfigService): IPublicClientApplication {
    var ans = new PublicClientApplication({
        auth: {
            clientId: service.config.clientId,
            authority: service.config.authority,
            knownAuthorities: service.config.knownAuthorities,
            redirectUri: '/',
        },
        cache: {
            cacheLocation: BrowserCacheLocation.LocalStorage,
            storeAuthStateInCookie: false,
        },
        system: {
            loggerOptions: {
                loggerCallback,
                logLevel: LogLevel.Info,
                piiLoggingEnabled: false
            }
        }
    });
    console.log(ans);
    return ans;
}

function MSALInterceptorConfigFactory(service: MsalConfigService): MsalInterceptorConfiguration {
    const protectedResourceMap = new Map<string, Array<string>>();
    protectedResourceMap.set('/drive', [service.config.scope]);
    return {
        interactionType: InteractionType.Popup,
        protectedResourceMap
    };
}

function MSALGuardConfigFactory(service: MsalConfigService): MsalGuardConfiguration {
    return {
        interactionType: InteractionType.Popup,
        authRequest: {
            scopes: [service.config.scope]
        }
    };
}

function loggerCallback(logLevel: LogLevel, message: string) {
    if (!environment.production) {
        console.log(message);
    }
}

@NgModule({
    imports: [MsalModule]
})
export class MsalApplicationModule {

    static forRoot() {
        return {
            ngModule: MsalApplicationModule,
            providers: [
                { provide: APPLICATION_SCOPE, useFactory: scopeFacotry, deps: [MsalConfigService] },
                { provide: APP_INITIALIZER, useFactory: initializerFactory, deps: [MsalConfigService], multi: true },
                { provide: MSAL_INSTANCE, useFactory: MSALInstanceFactory, deps: [MsalConfigService] },
                { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
                {
                    provide: MSAL_GUARD_CONFIG,
                    useFactory: MSALGuardConfigFactory,
                    deps: [MsalConfigService]
                },
                {
                    provide: MSAL_INTERCEPTOR_CONFIG,
                    useFactory: MSALInterceptorConfigFactory,
                    deps: [MsalConfigService]
                },
                MsalService,
                MsalGuard,
                MsalBroadcastService,
                {
                    provide: HTTP_INTERCEPTORS,
                    useClass: MsalInterceptor,
                    multi: true
                }
            ]
        };
    }
}

@Injectable({
    providedIn: 'root'
})
class MsalConfigService {

    private http: HttpClient;
    private _config: MsalConfig | undefined = undefined;

    public get config(): MsalConfig {
        return this._config as MsalConfig;
    }

    constructor(httpHandler: HttpBackend) {
        this.http = new HttpClient(httpHandler);
    }

    getConfig(endpoint: string): Promise<boolean> {
        return new Promise<boolean>((resolve, reject) => {
            firstValueFrom(this.http.get<MsalConfig>(endpoint)).then(c => {
                this._config = c;
                resolve(true);
            }).catch(error => reject(error));
        });
    }
}

class MsalConfig {
    public clientId: string = '';
    public authority: string = '';
    public knownAuthorities: string[] = [];
    public scope: string = '';
}