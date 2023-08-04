import {ChangeDetectionStrategy, Component} from '@angular/core';
import {CommonModule} from '@angular/common';

@Component({
    selector: 'app-log-settings',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './log-settings.component.html',
    styleUrls: ['./log-settings.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogSettingsComponent {

}
