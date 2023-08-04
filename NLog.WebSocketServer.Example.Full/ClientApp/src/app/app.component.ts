import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";

import { LogMenuComponent } from "./log-menu/log-menu.component";
import { LogViewerComponent } from "./log-viewer/log-viewer.component";

// ReSharper disable once InconsistentNaming
export const ROUTES: Routes = [
  {
    path: "",
    component: LogViewerComponent,
    pathMatch: "full"
  },
];

@Component({
  selector: "app-root",
  standalone: true,
  imports: [
    LogMenuComponent,
    LogViewerComponent,
    RouterModule
  ],
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  title = "app";
}
