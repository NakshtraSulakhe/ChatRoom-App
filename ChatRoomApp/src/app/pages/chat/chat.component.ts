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
<<<<<<< HEAD
=======
    console.log('🔑 Current username from localStorage:', this.username);
    console.log('🔑 All localStorage items:', localStorage);
    
    // If username is "You", we need to determine the actual username
    // This will be handled by the online users list
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
    this.chatService.startConnection(this.username);

    // ✅ Chat history
    this.chatService.chatHistory$.subscribe(history => {
<<<<<<< HEAD
      this.messages = history.map(msg => ({
        username: msg.userName || msg.username,
        text: msg.message || msg.text,
        timestamp: msg.timestamp,
        isMine: (msg.userName || msg.username) === this.username
      }));
=======
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
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
      this.cdr.detectChanges();
    });

    // ✅ New messages (replace, not append)
    this.chatService.chatMessages$.subscribe(newMsgs => {
<<<<<<< HEAD
      this.messages = newMsgs.map(msg => ({
        username: msg.userName || msg.user,
        text: msg.message,
        timestamp: msg.timestamp,
        isMine: (msg.userName || msg.user) === this.username
      }));
=======
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
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
      this.cdr.detectChanges();
    });

    // ✅ Online users
<<<<<<< HEAD
   this.chatService.onlineUsers$.subscribe(users => {
  this.onlineUsers = users; // ✅ keep actual usernames only
  this.cdr.detectChanges();
});
=======
    this.chatService.onlineUsers$.subscribe(users => {
      console.log('👥 Online users received:', users);
      
      // Remove duplicates and ensure current user is represented correctly
      const uniqueUsers = [...new Set(users)];
      
      // If the list contains both "You" and "Nakshtra", keep only "Nakshtra" (the real username)
      if (uniqueUsers.includes('You') && uniqueUsers.includes('Nakshtra')) {
        this.onlineUsers = uniqueUsers.filter(user => user !== 'You');
        // Update the username to the real username if it was "You"
        if (this.username === 'You') {
          this.username = 'Nakshtra';
          localStorage.setItem('username', 'Nakshtra');
          console.log('🔑 Updated username from "You" to "Nakshtra" and saved to localStorage');
        }
      } else {
        this.onlineUsers = uniqueUsers;
      }
      
      console.log('👥 Processed online users:', this.onlineUsers);
      console.log('🔑 Current username after processing:', this.username);
      this.cdr.detectChanges();
    });

    // ✅ Typing users
    this.chatService.typingUsers$.subscribe(users => {
      this.typingUsers = users;
      this.cdr.detectChanges();
    });
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)

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
<<<<<<< HEAD
=======

  private isMyMessage(msgUsername: string): boolean {
    // Direct match
    if (msgUsername === this.username) {
      return true;
    }
    
    // Special cases for common username variations
    if (this.username === 'You' && msgUsername === 'Nakshtra') {
      return true;
    }
    if (this.username === 'Nakshtra' && msgUsername === 'You') {
      return true;
    }
    
    // Case-insensitive match
    if (msgUsername.toLowerCase() === this.username.toLowerCase()) {
      return true;
    }
    
    return false;
  }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
}
