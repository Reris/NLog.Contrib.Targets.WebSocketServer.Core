import { enableProdMode } from "@angular/core";
import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";

import { AppComponent } from "./app/app.component";
import { environment } from "./environments/environment";
import { ROUTES } from "./app/_routes";

export function getBaseUrl() {
  return document.getElementsByTagName("base")[0].href;
}

if (environment.production) {
  enableProdMode();
}

// noinspection JSIgnoredPromiseFromCall
bootstrapApplication(
  AppComponent,
  {
    providers: [
      { provide: "BASE_URL", useFactory: getBaseUrl },
      provideRouter(ROUTES)
    ]
  });
