import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewChecked,
  ChangeDetectorRef,
  ElementRef,
  ViewChild
} from '@angular/core';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  standalone: false,
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, OnDestroy, AfterViewChecked {

  @ViewChild('chatContainer') chatContainer!: ElementRef;

  messages: { username: string; text: string; timestamp: string; isMine: boolean }[] = [];
  onlineUsers: string[] = [];
  typingUsers: Set<string> = new Set();

  newMessage = '';
  username = localStorage.getItem('username') || 'You';
  darkMode = localStorage.getItem('darkMode') === 'true';
  notification: string | null = null;

  private typingTimeout?: any;

  constructor(
    private chatService: ChatService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.username = localStorage.getItem('username') || 'You';
    console.log('🔑 Current username from localStorage:', this.username);
    console.log('🔑 All localStorage items:', localStorage);

    // If username is "You", we may determine the actual username from the online list
    this.chatService.startConnection(this.username);

    // ✅ Chat history
    this.chatService.chatHistory$.subscribe(history => {
      this.messages = history.map(msg => {
        const msgUsername = msg.userName || msg.username || msg.user;
        const isMine = this.isMyMessage(msgUsername);

        console.log(`🔍 Message from: "${msgUsername}", Current user: "${this.username}", Is mine: ${isMine}`);
        return {
          username: msgUsername,
          text: msg.message || msg.text,
          timestamp: msg.timestamp,
          isMine: isMine
        };
      });
      console.log('📜 Chat history processed:', this.messages);
      this.cdr.detectChanges();
    });

    // ✅ New messages (replace, not append)
    this.chatService.chatMessages$.subscribe(newMsgs => {
      this.messages = newMsgs.map(msg => {
        const msgUsername = msg.userName || msg.user;
        const isMine = this.isMyMessage(msgUsername);

        console.log(`🔍 New message from: "${msgUsername}", Current user: "${this.username}", Is mine: ${isMine}`);
        return {
          username: msgUsername,
          text: msg.message,
          timestamp: msg.timestamp,
          isMine: isMine
        };
      });
      console.log('💬 New messages processed:', this.messages);
      this.cdr.detectChanges();
    });

    // ✅ Online users
    this.chatService.onlineUsers$.subscribe(users => {
      console.log('👥 Online users received:', users);

      // Remove duplicates and ensure current user is represented correctly
      const uniqueUsers = [...new Set(users)];

      // If the list contains both "You" and a real username, prefer the real username
      if (uniqueUsers.includes('You') && uniqueUsers.length > 1) {
        this.onlineUsers = uniqueUsers.filter(user => user !== 'You');
        // If username was 'You', try to pick a non-'You' username
        if (this.username === 'You' && this.onlineUsers.length > 0) {
          this.username = this.onlineUsers[0];
          localStorage.setItem('username', this.username);
          console.log(`🔑 Updated username to "${this.username}" and saved to localStorage`);
        }
      } else {
        this.onlineUsers = uniqueUsers;
      }

      console.log('👥 Processed online users:', this.onlineUsers);
      this.cdr.detectChanges();
    });

    // ✅ Typing users
    this.chatService.typingUsers$.subscribe(users => {
      this.typingUsers = users;
      this.cdr.detectChanges();
    });

    if (this.darkMode) document.body.classList.add('dark-mode');
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  ngOnDestroy(): void {
    this.chatService.stopConnection();
  }

  async sendMessage(): Promise<void> {
    if (!this.newMessage.trim()) return;

    try {
      await this.chatService.sendMessage(this.newMessage);
      this.newMessage = '';
      this.notification = '✅ Message sent!';
      setTimeout(() => this.notification = null, 3000);
      this.cdr.detectChanges();
    } catch (err) {
      console.error('❌ Error sending message:', err);
    }
  }

  onTyping(): void {
    this.chatService.sendTypingIndicator();

    clearTimeout(this.typingTimeout);
    this.typingTimeout = setTimeout(() => {
      this.chatService.stopTypingIndicator();
    }, 2000);
  }

  toggleTheme(): void {
    this.darkMode = !this.darkMode;
    localStorage.setItem('darkMode', this.darkMode.toString());

    if (this.darkMode) {
      document.body.classList.add('dark-mode');
    } else {
      document.body.classList.remove('dark-mode');
    }

    this.notification = `Switched to ${this.darkMode ? 'Dark' : 'Light'} Mode`;
    setTimeout(() => this.notification = null, 3000);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('username');

    this.chatService.stopConnection();
    this.authService.logout();
    this.router.navigate(['/']);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const chatMessagesContainer = this.chatContainer?.nativeElement;
      if (chatMessagesContainer) {
        chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
      }
    }, 100);
  }

  private isMyMessage(msgUsername: string): boolean {
    if (!msgUsername) return false;

    // Direct match
    if (msgUsername === this.username) return true;

    // Case-insensitive match
    if (msgUsername.toLowerCase() === this.username.toLowerCase()) return true;

    // Common alias handling
    if ((this.username === 'You' && msgUsername.toLowerCase() !== 'you') || (msgUsername === 'You' && this.username.toLowerCase() !== 'you')) {
      return false;
    }

    return false;
  }
}
