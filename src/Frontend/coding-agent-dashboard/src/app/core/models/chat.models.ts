export interface ConversationDto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface MessageDto {
  id: string;
  conversationId: string;
  role: 'User' | 'Assistant' | 'System';
  content: string;
  sentAt: string;
  uploadedByUserId?: string;
  attachments?: AttachmentDto[];
}

export interface AttachmentDto {
  id: string;
  messageId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  storageUrl: string;
  thumbnailUrl?: string | null;
  uploadedByUserId: string;
  uploadedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  nextCursor?: string | null;
}
