import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ChatService } from '../../../core/services/chat.service';
import { ConversationDto } from '../../../core/models/chat.models';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [CommonModule, MatListModule, MatProgressSpinnerModule],
  template: `
    <div class="conversation-list">
      <ng-container *ngIf="!loading(); else loadingTpl">
        <mat-nav-list>
          <a mat-list-item *ngFor="let c of conversations()" (click)="select(c)">
            <span matListItemTitle>{{ c.title }}</span>
            <span matListItemLine>{{ c.updatedAt | date:'short' }}</span>
          </a>
        </mat-nav-list>
      </ng-container>
      <ng-template #loadingTpl>
        <div class="loading"><mat-spinner diameter="24"></mat-spinner></div>
      </ng-template>
    </div>
  `,
  styles: [`
    .conversation-list { height: 100%; overflow: auto; }
    .loading { display: flex; align-items: center; justify-content: center; padding: 12px; }
  `]
})
export class ConversationListComponent {
  @Output() conversationSelected = new EventEmitter<ConversationDto>();

  conversations = signal<ConversationDto[]>([]);
  loading = signal<boolean>(true);

  constructor(private chat: ChatService) {}

  ngOnInit() {
    this.chat.listConversations().subscribe({
      next: (data) => {
        this.conversations.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  select(c: ConversationDto) {
    this.conversationSelected.emit(c);
  }
}
