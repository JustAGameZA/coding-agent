import { Component, WritableSignal, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ConversationListComponent } from './components/conversation-list.component';
import { ChatThreadComponent } from './components/chat-thread.component';
import { MessageInputComponent } from './components/message-input.component';
import { SignalRService } from '../../core/services/signalr.service';
import { ChatService } from '../../core/services/chat.service';
import { ConversationDto, MessageDto } from '../../core/models/chat.models';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressBarModule, ConversationListComponent, ChatThreadComponent, MessageInputComponent],
  template: `
    <div class="chat-grid" [attr.data-testid]="'chat-root'">
      <div class="sidebar" [attr.data-testid]="'conversation-list-sidebar'">
        <app-conversation-list 
          [attr.data-testid]="'conversation-list'"
          (conversationSelected)="onConversationSelected($event)">
        </app-conversation-list>
      </div>
      <div class="thread">
        <mat-card class="thread-card">
          <mat-card-header>
            <mat-card-title>
              <div class="title-row">
                <span [attr.data-testid]="'chat-title'">{{ selectedConversation()?.title || 'Chat with AI Agent' }}</span>
                <span class="spacer"></span>
                <span 
                  class="conn" 
                  [class.ok]="isConnected()" 
                  [class.reconnecting]="connState() === 'Reconnecting'"
                  [attr.data-testid]="'connection-status'">
                  <mat-icon>{{ isConnected() ? 'wifi' : 'wifi_off' }}</mat-icon>
                </span>
                <span class="reconnect" *ngIf="connState() === 'Reconnecting' && nextDelay() !== null">
                  Reconnecting in {{ (nextDelay() || 0) / 1000 | number:'1.0-0' }}s
                </span>
                <span 
                  class="agent-status" 
                  *ngIf="agentTyping()" 
                  [attr.data-testid]="'agent-typing'">
                  <mat-icon class="thinking-icon">psychology</mat-icon>
                  <span class="status-text">AI is thinking...</span>
                </span>
              </div>
            </mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <app-chat-thread 
              [attr.data-testid]="'chat-thread'"
              [data]="messages()">
            </app-chat-thread>
          </mat-card-content>
          <mat-card-actions>
            <app-message-input 
              [attr.data-testid]="'message-input'"
              (message)="send($event)" 
              (fileSelected)="upload($event)">
            </app-message-input>
          </mat-card-actions>
          <div class="upload" *ngIf="uploading()" [attr.data-testid]="'upload-progress'">
            <mat-progress-bar mode="determinate" [value]="uploadProgress()"></mat-progress-bar>
          </div>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .chat-grid { 
      display: grid; 
      grid-template-columns: 320px 1fr; 
      height: calc(100vh - 112px); 
      gap: 16px; 
      padding: 16px; 
    }
    
    .sidebar { 
      background: #fafafa; 
      border-right: 1px solid #eee; 
      overflow: auto; 
    }
    
    .thread { 
      display: flex; 
    }
    
    .thread-card { 
      flex: 1; 
      display: flex; 
      flex-direction: column; 
    }
    
    mat-card-content { 
      flex: 1; 
      overflow: auto; 
    }
    
    .title-row { 
      display: flex; 
      align-items: center; 
      width: 100%; 
      flex-wrap: wrap;
      gap: 8px;
    }
    
    .spacer { 
      flex: 1; 
    }
    
    .conn { 
      display: inline-flex; 
      align-items: center; 
      gap: 4px; 
      opacity: 0.7; 
    }
    
    .conn.ok { 
      color: #2e7d32; 
    }
    
    .conn.reconnecting { 
      color: #f9a825; 
    }
    
    .upload { 
      padding: 0 16px 16px; 
    }
    
    .agent-status { 
      display: flex; 
      align-items: center; 
      gap: 8px; 
      padding: 4px 12px; 
      background: rgba(103, 58, 183, 0.1); 
      border-radius: 12px; 
      font-size: 0.875rem; 
      color: #673ab7; 
      animation: pulse 2s infinite; 
    }
    
    .agent-status .thinking-icon { 
      font-size: 20px; 
      width: 20px; 
      height: 20px; 
      animation: rotate 2s linear infinite; 
    }
    
    .agent-status .status-text { 
      font-weight: 500; 
    }
    
    @keyframes pulse { 
      0%, 100% { opacity: 1; } 
      50% { opacity: 0.6; } 
    }
    
    @keyframes rotate { 
      from { transform: rotate(0deg); } 
      to { transform: rotate(360deg); } 
    }

    @media (max-width: 768px) {
      .chat-grid {
        grid-template-columns: 1fr;
        height: calc(100vh - 64px);
      }

      .sidebar {
        display: none; // Hide sidebar on mobile, show as overlay instead
      }
    }
  `]
})
export class ChatComponent {
  constructor(private chatService: ChatService, private signalR: SignalRService) {
    // Initialize signals that depend on injected services in the constructor to avoid TS2729
    this.isConnected = this.signalR.isConnected;
  }

  selectedConversation = signal<ConversationDto | null>(null);
  messages = signal<MessageDto[]>([]);
  agentTyping = signal<boolean>(false);
  isConnected!: WritableSignal<boolean>;
  uploading = signal<boolean>(false);
  uploadProgress = signal<number>(0);
  nextDelay = () => this.signalR.nextRetryDelayMs();
  connState = () => {
    const s = this.signalR.connectionState();
    switch (s) {
      case signalR.HubConnectionState.Disconnected: return 'Disconnected';
      case signalR.HubConnectionState.Connecting: return 'Connecting';
      case signalR.HubConnectionState.Connected: return 'Connected';
      case signalR.HubConnectionState.Disconnecting: return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting: return 'Reconnecting';
      default: return '';
    }
  };

  async ngOnInit() {
    await this.signalR.connect();
    
    this.signalR.on<MessageDto>('ReceiveMessage', (msg) => {
      this.messages.update(curr => [...curr, msg]);
    });
    
    // Listen for agent typing indicator
    this.signalR.on<boolean>('AgentTyping', (isTyping) => {
      this.agentTyping.set(isTyping);
    });
  }

  async onConversationSelected(c: ConversationDto) {
    this.selectedConversation.set(c);
    await this.signalR.joinConversation(c.id);
    // Load last messages (optional, if API supports it)
    this.chatService.listMessages(c.id).subscribe(res => this.messages.set(res.items || []));
  }

  async send(content: string) {
    const c = this.selectedConversation();
    if (!c) return;
    
    // Send message to AI agent for processing
    await this.signalR.sendMessage(c.id, content);
  }

  upload(file: File) {
    const conversation = this.selectedConversation();
    if (!conversation) return;

    this.uploading.set(true);
    this.uploadProgress.set(0);

    this.chatService.uploadAttachmentWithProgress(file).subscribe({
      next: (event) => {
        if ((event as any).type === 1 /* HttpEventType.Sent */) {
          this.uploadProgress.set(5);
        }
        if ((event as any).loaded != null && (event as any).total) {
          const pct = Math.round(((event as any).loaded / (event as any).total) * 100);
          this.uploadProgress.set(pct);
        }
        if ((event as any).body) {
          const attachment = (event as any).body as any; // AttachmentDto
          // Add a client-side message with the uploaded attachment so users see it immediately
          const msg: MessageDto = {
            id: crypto.randomUUID(),
            conversationId: conversation.id,
            role: 'User',
            content: `Uploaded file: ${attachment.fileName}`,
            sentAt: new Date().toISOString(),
            attachments: [attachment]
          } as MessageDto;
          this.messages.update(curr => [...curr, msg]);
          this.uploadProgress.set(100);
          this.uploading.set(false);
        }
      },
      error: () => {
        this.uploading.set(false);
      }
    });
  }
}
