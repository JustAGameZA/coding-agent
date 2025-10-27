import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MessageDto } from '../../../core/models/chat.models';

@Component({
  selector: 'app-chat-thread',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule],
  template: `
    <div class="chat-thread">
      <mat-list>
        <mat-list-item *ngFor="let m of messages()">
          <mat-icon matListItemIcon>{{ m.role === 'User' ? 'person' : 'smart_toy' }}</mat-icon>
          <div matListItemTitle>{{ m.content }}</div>
          <div matListItemLine>{{ m.sentAt | date:'shortTime' }}</div>
          <div class="attachments" *ngIf="m.attachments?.length">
            <ng-container *ngFor="let a of m.attachments">
              <ng-container [ngSwitch]="isImage(a.contentType)">
                <a *ngSwitchCase="true" [href]="a.storageUrl" target="_blank" class="thumb">
                  <img [src]="a.thumbnailUrl || a.storageUrl" [alt]="a.fileName" />
                </a>
                <a *ngSwitchDefault [href]="a.storageUrl" target="_blank">{{ a.fileName }}</a>
              </ng-container>
            </ng-container>
          </div>
        </mat-list-item>
      </mat-list>
    </div>
  `,
  styles: [`
    .chat-thread { height: 100%; overflow: auto; padding: 8px; }
    .attachments { margin-top: 4px; display: flex; gap: 8px; }
    .thumb img { max-height: 64px; border-radius: 4px; border: 1px solid #e0e0e0; }
  `]
})
export class ChatThreadComponent {
  @Input() set data(value: MessageDto[]) { this.messages.set(value || []); }
  messages = signal<MessageDto[]>([]);

  isImage(contentType: string | undefined): boolean {
    return !!contentType && contentType.startsWith('image/');
  }
}
