import { enableProdMode } from "@angular/core";
import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";

import { AppComponent, ROUTES } from "./app/app.component";
import { environment } from "./environments/environment";

export function getBaseUrl() {
  return document.getElementsByTagName("base")[0].href;
}

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(
  AppComponent,
  {
    providers: [
      { provide: "BASE_URL", useFactory: getBaseUrl },
      provideRouter(ROUTES)
    ]
  });
