import { Injectable, computed, signal } from '@angular/core';
import { SignalRService } from './signalr.service';

export interface PresenceChangedEvent {
  userId: string;
  isOnline: boolean;
  lastSeenUtc?: string | null;
}

export interface UserPresenceState {
  online: boolean;
  lastSeenUtc?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PresenceService {
  private readonly _presence = signal<Map<string, UserPresenceState>>(new Map());

  // Derived counts for simple UI
  readonly onlineCount = computed(() => {
    let count = 0;
    for (const val of this._presence().values()) {
      if (val.online) count++;
    }
    return count;
  });

  subscribeToHub(hub: SignalRService) {
    hub.on<PresenceChangedEvent>('UserPresenceChanged', (evt) => {
      const map = new Map(this._presence());
      map.set(evt.userId, { online: evt.isOnline, lastSeenUtc: evt.lastSeenUtc });
      this._presence.set(map);
    });
  }

  set(userId: string, state: UserPresenceState) {
    const map = new Map(this._presence());
    map.set(userId, state);
    this._presence.set(map);
  }

  get(userId: string): UserPresenceState | undefined {
    return this._presence().get(userId);
  }

  all(): Map<string, UserPresenceState> {
    return this._presence();
  }

  async fetchInitial(conversationId: string): Promise<void> {
    // TODO: Wire up to REST endpoint when backend presence API is available
    // For now, no-op
    return;
  }
}
