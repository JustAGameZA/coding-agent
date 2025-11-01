import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import type { RetryContext } from '@microsoft/signalr';
import { NotificationService } from './notifications/notification.service';
import { firstValueFrom } from 'rxjs';

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

    const hubUrl = environment.chatHubUrl || environment.signalRUrl;
    console.log('Initializing SignalR connection to:', hubUrl);
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false, // Allow SignalR negotiation through gateway
        // Enable all transports and let SignalR negotiate the best one
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling,
        accessTokenFactory: () => {
          const token = this.auth.getToken();
          if (!token) {
            console.warn('No auth token available for SignalR connection - user may need to login');
            return '';
          }
          console.log('SignalR using access token (length):', token.length);
          return token;
        }
      })
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(signalR.LogLevel.Information) // Use Information level to reduce console spam
      .build();

    // Connection state handlers
    this.hubConnection.onreconnecting(() => {
      this.connectionState.set(signalR.HubConnectionState.Reconnecting);
      this.isConnected.set(false);
      this.notify.info('Connection lost. Reconnecting...');
    });

    this.hubConnection.onreconnected((connectionId) => {
      this.connectionState.set(signalR.HubConnectionState.Connected);
      this.isConnected.set(true);
      this.nextRetryDelayMs.set(null);
      console.log('SignalR reconnected with connectionId:', connectionId);
      this.notify.info('Reconnected');
    });

    this.hubConnection.onclose((err) => {
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      this.isConnected.set(false);
      this.nextRetryDelayMs.set(null);
      if (err) {
        console.error('SignalR closed with error', err);
        this.notify.error(`Disconnected from chat: ${err.message || 'Connection closed'}`);
      } else {
        this.notify.info('Disconnected from chat');
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

    const currentState = this.hubConnection!.state;
    console.log('SignalR connection state before connect:', currentState);
    
    // Check if user is authenticated before attempting connection
    const token = this.auth.getToken();
    if (!token) {
      console.warn('No authentication token available. User must login first.');
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      this.isConnected.set(false);
      this.notify.error('Please login to connect to chat');
      return;
    }
    
    if (currentState === signalR.HubConnectionState.Disconnected) {
      try {
        const url = environment.chatHubUrl || environment.signalRUrl;
        console.log('Attempting to connect to SignalR hub:', url);
        console.log('Using auth token (length):', token.length);
        
        await this.hubConnection!.start();
        
        this.connectionState.set(signalR.HubConnectionState.Connected);
        this.isConnected.set(true);
        this.nextRetryDelayMs.set(null);
        
        console.log('SignalR connected successfully. ConnectionId:', this.hubConnection!.connectionId);
        this.notify.info('Connected to chat');
      } catch (error: any) {
        console.error('Error connecting to SignalR hub:', error);
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
        this.isConnected.set(false);
        
        let errorMessage = 'Failed to connect to chat service';
        if (error?.statusCode === 401 || error?.message?.includes('401')) {
          errorMessage = 'Authentication failed. Please login again.';
          // Token might be expired - try to refresh
          console.log('Authentication failed, attempting token refresh...');
          try {
            const newToken = await firstValueFrom(this.auth.refreshToken());
            if (newToken) {
              // Reinitialize connection with new token
              this.initializeConnection();
              // Retry connection after refresh
              console.log('Token refreshed, retrying connection...');
              await this.hubConnection!.start();
              this.connectionState.set(signalR.HubConnectionState.Connected);
              this.isConnected.set(true);
              this.notify.info('Connected to chat');
              return;
            }
          } catch (refreshError) {
            console.error('Token refresh failed:', refreshError);
            errorMessage = 'Session expired. Please login again.';
          }
        } else if (error?.message) {
          errorMessage = error.message;
        }
        
        this.notify.error(errorMessage);
        throw error;
      }
    } else if (currentState === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected');
      // Ensure signals are set correctly even if already connected
      this.connectionState.set(signalR.HubConnectionState.Connected);
      this.isConnected.set(true);
    } else {
      console.log('SignalR connection in progress, state:', currentState);
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

  /**
   * Send a message to the AI agent in a conversation.
   * The backend will publish an event for the Orchestration service to process.
   */
  public async sendMessage(conversationId: string, content: string): Promise<void> {
    await this.invoke('SendMessage', conversationId, content);
  }
}
