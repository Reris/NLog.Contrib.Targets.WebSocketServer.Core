import { Routes } from "@angular/router";
import { LogViewerComponent } from "./log-viewer/log-viewer.component";

export const ROUTES: Routes = [
  {
    path: "",
    component: LogViewerComponent,
    pathMatch: "full",
  },
];
