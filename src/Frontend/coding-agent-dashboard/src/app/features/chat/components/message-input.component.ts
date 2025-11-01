import { Component, EventEmitter, Output, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-message-input',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="message-input">
      <mat-form-field appearance="outline" class="flex-1">
        <input 
          matInput 
          placeholder="Type a message" 
          [(ngModel)]="content" 
          (keyup.enter)="send()" 
          [disabled]="disabled || uploading() || sending" />
      </mat-form-field>
      <button mat-icon-button (click)="file.click()" [disabled]="uploading() || disabled || sending" title="Attach file">
        <mat-icon>attach_file</mat-icon>
      </button>
      <input type="file" #file hidden (change)="onFileSelected($event)" />
      <button 
        mat-raised-button 
        color="primary" 
        (click)="send()" 
        [disabled]="!content.trim() || uploading() || disabled || sending">
        <mat-spinner *ngIf="sending" diameter="20" class="spinner"></mat-spinner>
        <span *ngIf="!sending">Send</span>
      </button>
    </div>
  `,
  styles: [`
    .message-input { display: flex; gap: 8px; align-items: center; padding: 8px; }
    .flex-1 { flex: 1; }
    .spinner { display: inline-block; margin-right: 8px; }
    button { position: relative; }
  `]
})
export class MessageInputComponent {
  @Output() message = new EventEmitter<string>();
  @Output() fileSelected = new EventEmitter<File>();
  @Input() disabled = false;
  @Input() sending = false;

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
