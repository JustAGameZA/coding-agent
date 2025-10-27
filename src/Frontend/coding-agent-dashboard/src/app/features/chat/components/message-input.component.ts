import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-message-input',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <div class="message-input">
      <mat-form-field appearance="outline" class="flex-1">
        <input matInput placeholder="Type a message" [(ngModel)]="content" (keyup.enter)="send()" />
      </mat-form-field>
      <button mat-icon-button (click)="file.click()" [disabled]="uploading()" title="Attach file">
        <mat-icon>attach_file</mat-icon>
      </button>
      <input type="file" #file hidden (change)="onFileSelected($event)" />
      <button mat-raised-button color="primary" (click)="send()" [disabled]="!content.trim() || uploading()">Send</button>
    </div>
  `,
  styles: [`
    .message-input { display: flex; gap: 8px; align-items: center; padding: 8px; }
    .flex-1 { flex: 1; }
  `]
})
export class MessageInputComponent {
  @Output() message = new EventEmitter<string>();
  @Output() fileSelected = new EventEmitter<File>();

  content = '';
  uploading = signal<boolean>(false);

  send() {
    const trimmed = this.content.trim();
    if (!trimmed) return;
    this.message.emit(trimmed);
    this.content = '';
  }

  onFileSelected(evt: Event) {
    const input = evt.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    if (file.size > environment.maxUploadSize) {
      alert(`File too large. Max size is ${Math.round(environment.maxUploadSize / (1024*1024))}MB.`);
      return;
    }
    this.fileSelected.emit(file);
  }
}
