import {
  Component, OnInit, OnDestroy, AfterViewChecked,
  ChangeDetectorRef, ElementRef, ViewChild
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
    this.chatService.startConnection(this.username);

    // ✅ Chat history
    this.chatService.chatHistory$.subscribe(history => {
      this.messages = history.map(msg => ({
        username: msg.userName || msg.username,
        text: msg.message || msg.text,
        timestamp: msg.timestamp,
        isMine: (msg.userName || msg.username) === this.username
      }));
      this.cdr.detectChanges();
    });

    // ✅ New messages (replace, not append)
    this.chatService.chatMessages$.subscribe(newMsgs => {
      this.messages = newMsgs.map(msg => ({
        username: msg.userName || msg.user,
        text: msg.message,
        timestamp: msg.timestamp,
        isMine: (msg.userName || msg.user) === this.username
      }));
      this.cdr.detectChanges();
    });

    // ✅ Online users
   this.chatService.onlineUsers$.subscribe(users => {
  this.onlineUsers = users; // ✅ keep actual usernames only
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
}
