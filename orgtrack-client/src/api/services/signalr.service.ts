import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '../../stores/authStore';

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5106';
const HUB_URL = `${API_BASE.replace('/api', '')}/hubs/orgtrack`;

export const signalrService = {
  start(): Promise<void> {
    if (!startPromise) {
      startPromise = this._startInternal();
    }
    return startPromise;
  },

  async _startInternal(): Promise<void> {
    const authStore = useAuthStore();
    const token = authStore.accessToken;
    if (!token) return;

    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => authStore.accessToken || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.onreconnecting(() => {
      console.log('[SignalR] Reconnecting...');
    });

    connection.onreconnected(() => {
      console.log('[SignalR] Reconnected!');
    });

    connection.onclose(() => {
      console.log('[SignalR] Connection closed.');
      startPromise = null;
    });

    try {
      await connection.start();
      console.log('[SignalR] Connected!');
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
      startPromise = null;
    }
  },

  stop(): void {
    if (connection) {
      connection.stop();
      connection = null;
      startPromise = null;
    }
  },

  async joinUnitGroup(unitId: string): Promise<void> {
    await this.start();
    if (connection?.state === signalR.HubConnectionState.Connected) {
      await connection.invoke('JoinUnitGroup', unitId);
    }
  },

  async leaveUnitGroup(unitId: string): Promise<void> {
    await this.start();
    if (connection?.state === signalR.HubConnectionState.Connected) {
      await connection.invoke('LeaveUnitGroup', unitId);
    }
  },

  on(event: string, callback: (...args: any[]) => void): void {
    if (connection) {
      connection.on(event, callback);
    }
  },

  off(event: string, callback?: (...args: any[]) => void): void {
    if (connection) {
      if (callback) {
        connection.off(event, callback);
      } else {
        connection.off(event);
      }
    }
  },

  getConnection(): signalR.HubConnection | null {
    return connection;
  }
};
