import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpHeaders, HttpRequest } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AttachmentDto, ConversationDto, MessageDto, PagedResponse } from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly base = environment.apiBaseUrl || environment.apiUrl;

  constructor(private http: HttpClient) {}

  listConversations(): Observable<ConversationDto[]> {
    return this.http.get<ConversationDto[]>(`${this.base}/conversations`);
  }

  createConversation(title: string): Observable<ConversationDto> {
    return this.http.post<ConversationDto>(`${this.base}/conversations`, { title });
  }

  getConversation(id: string): Observable<ConversationDto> {
    return this.http.get<ConversationDto>(`${this.base}/conversations/${id}`);
  }

  listMessages(conversationId: string, cursor?: string): Observable<PagedResponse<MessageDto>> {
    const url = new URL(`${this.base}/conversations/${conversationId}/messages`, window.location.origin);
    if (cursor) url.searchParams.set('cursor', cursor);
    return this.http.get<PagedResponse<MessageDto>>(url.toString());
  }

  /**
   * Upload an attachment via multipart/form-data
   */
  uploadAttachment(file: File): Observable<AttachmentDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<AttachmentDto>(`${this.base}/attachments`, form, {
      reportProgress: false
    });
  }

  /**
   * Upload with progress reporting. Emits HttpEvents (progress + final response)
   */
  uploadAttachmentWithProgress(file: File): Observable<HttpEvent<AttachmentDto>> {
    const form = new FormData();
    form.append('file', file, file.name);
    const req = new HttpRequest('POST', `${this.base}/attachments`, form, {
      reportProgress: true
    });
    return this.http.request<AttachmentDto>(req);
  }

  /**
   * Optional: Get a presigned URL if the API exposes such an endpoint
   */
  getPresignedUrl(storageUrl: string): Observable<string> {
    // Adjust to match your API; placeholder implementation
    const url = new URL(`${this.base}/attachments/presign`, window.location.origin);
    url.searchParams.set('url', storageUrl);
    return this.http.get<{ url: string }>(url.toString()).pipe(map(x => x.url));
  }
}
