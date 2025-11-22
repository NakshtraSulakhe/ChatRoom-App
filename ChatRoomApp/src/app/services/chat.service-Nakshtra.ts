import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubUrl = 'http://localhost:5250/chathub';
  private hubConnection!: signalR.HubConnection;

  private onlineUsersSubject = new BehaviorSubject<string[]>([]);
  onlineUsers$ = this.onlineUsersSubject.asObservable();

  private chatMessagesSubject = new BehaviorSubject<any[]>([]);
  chatMessages$ = this.chatMessagesSubject.asObservable();

  private chatHistorySubject = new BehaviorSubject<any[]>([]);
  chatHistory$ = this.chatHistorySubject.asObservable();

  private typingUsersSubject = new BehaviorSubject<Set<string>>(new Set());
  typingUsers$ = this.typingUsersSubject.asObservable();

  constructor(private _authService: AuthService) {}

  startConnection(username: string) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubUrl}?username=${encodeURIComponent(username)}`, {
        accessTokenFactory: () => localStorage.getItem('token') || ''
      })
      .withAutomaticReconnect()
      .build();

    // Optional: on reconnect, server will re-add user via OnConnectedAsync
    this.hubConnection.onreconnected(() => {
      const newId = this.hubConnection.connectionId || '';
      console.log('🔁 Reconnected. New connectionId:', newId);
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log('✅ SignalR Connected');
        // Server's OnConnectedAsync reads username from query and registers user
        this.requestChatHistory(); // Automatically request chat history after connecting

        /** 🔔 Setup Listeners */
        this.registerListeners();
      })
      .catch(err => console.error('❌ SignalR Connection Error:', err));
  }

  private registerListeners(): void {
    this.hubConnection.on('ReceiveMessage', (user: string, message: string, timestamp: string) => {
      console.log(`💬 ${user} at ${timestamp}: ${message}`);
      const currentMessages = this.chatMessagesSubject.value;
      this.chatMessagesSubject.next([...currentMessages, { user, message, timestamp }]);
    });

    this.hubConnection.on('ReceivePrivateMessage', (sender: string, message: string, timestamp: string) => {
      console.log(`📩 Private from ${sender} at ${timestamp}: ${message}`);
      const currentMessages = this.chatMessagesSubject.value;
      this.chatMessagesSubject.next([...currentMessages, { user: sender, message, timestamp, private: true }]);
    });

    this.hubConnection.on('UpdateUserList', (users: string[]) => {
      console.log('👥 Online users:', users);
      this.onlineUsersSubject.next(users);
    });

    this.hubConnection.on('ReceiveHistory', (messages: any[]) => {
      console.log('📜 Chat history received:', messages);
      this.chatHistorySubject.next(messages);
    });

    // Also handle initial history sent by OnConnectedAsync
    this.hubConnection.on('LoadChatHistory', (messages: any[]) => {
      console.log('📜 LoadChatHistory received:', messages);
      this.chatHistorySubject.next(messages);
    });

    this.hubConnection.on('UserJoined', (user: string) => {
      console.log(`🎉 ${user} joined`);
    });

    this.hubConnection.on('UserLeft', (user: string) => {
      console.log(`👋 ${user} left`);
    });

    this.hubConnection.on('UserTyping', (user: string) => {
      console.log(`⌨️ ${user} is typing`);
      const currentTypingUsers = new Set(this.typingUsersSubject.value);
      currentTypingUsers.add(user);
      this.typingUsersSubject.next(currentTypingUsers);
    });

    this.hubConnection.on('UserStoppedTyping', (user: string) => {
      console.log(`⏹️ ${user} stopped typing`);
      const currentTypingUsers = new Set(this.typingUsersSubject.value);
      currentTypingUsers.delete(user);
      this.typingUsersSubject.next(currentTypingUsers);
    });
  }

  stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop()
        .then(() => console.log('🔌 Disconnected'))
        .catch(err => console.error('❗ Disconnect error:', err));
    }
  }

  sendMessage(message: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('SendMessage', message);
    } else {
      return Promise.reject('SignalR is not connected');
    }
  }

  sendPrivateMessage(recipientUsername: string, message: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('SendPrivateMessage', recipientUsername, message);
    } else {
      return Promise.reject('SignalR is not connected');
    }
  }

  sendTypingIndicator(): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('SendTypingIndicator').catch(err =>
        console.error('❗ Typing indicator error:', err)
      );
    }
  }

  stopTypingIndicator(): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('StopTypingIndicator').catch(err =>
        console.error('❗ Stop typing error:', err)
      );
    }
  }

  // addUserToOnlineList is handled on server OnConnectedAsync; no client call needed

  requestChatHistory(): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('RequestHistory')
        .then(() => console.log('📜 Chat history requested'))
        .catch(err => console.error('❗ History request error:', err));
    }
  }

  // No need to poll for connectionId since server uses Context.ConnectionId
}
