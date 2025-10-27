import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import type { RetryContext } from '@microsoft/signalr';
import { NotificationService } from './notifications/notification.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  
  // Connection state signals
  public connectionState = signal<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  public isConnected = signal<boolean>(false);
  public nextRetryDelayMs = signal<number | null>(null);

  constructor(private auth: AuthService, private notify: NotificationService) {
    this.initializeConnection();
  }

  /**
   * Initialize SignalR hub connection
   */
  private initializeConnection(): void {
    const retrySeq = [0, 2000, 5000, 10000, 20000, 30000];
    const retryPolicy = {
      nextRetryDelayInMilliseconds: (ctx: RetryContext) => {
        const idx = Math.min(ctx.previousRetryCount, retrySeq.length - 1);
        const delay = retrySeq[idx];
        this.nextRetryDelayMs.set(delay);
        return delay;
      }
    } as signalR.IRetryPolicy;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.chatHubUrl || environment.signalRUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => this.auth.getToken() || ''
      })
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Connection state handlers
    this.hubConnection.onreconnecting(() => {
      this.connectionState.set(signalR.HubConnectionState.Reconnecting);
      this.isConnected.set(false);
      this.notify.info('Connection lost. Reconnecting...');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.set(signalR.HubConnectionState.Connected);
      this.isConnected.set(true);
      this.nextRetryDelayMs.set(null);
      this.notify.info('Reconnected');
    });

    this.hubConnection.onclose((err) => {
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      this.isConnected.set(false);
      this.nextRetryDelayMs.set(null);
      this.notify.error('Disconnected from chat');
      if (err) {
        console.error('SignalR closed with error', err);
      }
    });
  }

  /**
   * Start the SignalR connection
   */
  public async connect(): Promise<void> {
    if (!this.hubConnection) {
      this.initializeConnection();
    }

    if (this.hubConnection!.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.hubConnection!.start();
        this.connectionState.set(signalR.HubConnectionState.Connected);
        this.isConnected.set(true);
        console.log('SignalR connected successfully');
      } catch (error) {
        console.error('Error connecting to SignalR hub:', error);
        throw error;
      }
    }
  }

  /**
   * Stop the SignalR connection
   */
  public async disconnect(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.stop();
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
        this.isConnected.set(false);
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting from SignalR hub:', error);
        throw error;
      }
    }
  }

  /**
   * Register a handler for a specific hub method
   * @param methodName The name of the hub method to listen for
   * @param handler The callback function to execute when the method is invoked
   */
  public on<T>(methodName: string, handler: (data: T) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on(methodName, handler);
    }
  }

  /**
   * Invoke a hub method
   * @param methodName The name of the hub method to invoke
   * @param args The arguments to pass to the hub method
   */
  public async invoke(methodName: string, ...args: any[]): Promise<any> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        return await this.hubConnection.invoke(methodName, ...args);
      } catch (error) {
        console.error(`Error invoking hub method ${methodName}:`, error);
        throw error;
      }
    } else {
      throw new Error('SignalR connection is not established');
    }
  }

  /**
   * Remove a handler for a specific hub method
   * @param methodName The name of the hub method to stop listening for
   */
  public off(methodName: string): void {
    if (this.hubConnection) {
      this.hubConnection.off(methodName);
    }
  }

  // Convenience methods for Chat Hub
  public async joinConversation(conversationId: string): Promise<void> {
    await this.invoke('JoinConversation', conversationId);
  }

  public async leaveConversation(conversationId: string): Promise<void> {
    await this.invoke('LeaveConversation', conversationId);
  }

  public async sendMessage(conversationId: string, content: string): Promise<void> {
    await this.invoke('SendMessage', conversationId, content);
  }

  public async typingIndicator(conversationId: string, isTyping: boolean): Promise<void> {
    await this.invoke('TypingIndicator', conversationId, isTyping);
  }
}
