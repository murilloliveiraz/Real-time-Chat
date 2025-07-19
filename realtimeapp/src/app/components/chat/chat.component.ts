import { Component } from '@angular/core';
import { User } from '../../interfaces/User';
import { ChatMessage } from '../../interfaces/ChatMessage';
import { Subscription } from 'rxjs';
import { AuthenticationService } from '../../services/authentication.service';
import { SignalrService } from '../../services/signalr.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent {
  currentUser: User | null = null;
  onlineUsers: User[] = [];
  selectedUser: User | null = null;
  messageInput: string = '';
  chatHistory: { [userId: string]: ChatMessage[] } = {};
  systemMessages: string[] = [];

  private subscriptions: Subscription[] = [];

  constructor(
    public authService: AuthenticationService,
    private signalrService: SignalrService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.currentUser = this.authService.currentUserValue;
    if (!this.currentUser) {
      this.router.navigate(['/login']);
      return;
    }

    this.connectSignalR();
    this.setupSubscriptions();
  }

  private async connectSignalR(): Promise<void> {
    await this.signalrService.startConnection();
  }

  private setupSubscriptions(): void {
    this.subscriptions.push(
      this.signalrService.onlineUsers$.subscribe(users => {
        this.onlineUsers = users.filter(u => u.userId !== this.currentUser?.userId);
        if (this.selectedUser && !this.onlineUsers.some(u => u.userId === this.selectedUser?.userId)) {
          this.selectedUser = null;
        }
      })
    );

    this.subscriptions.push(
      this.signalrService.privateMessages$.subscribe(message => {
        const otherUserId = message.senderId === this.currentUser?.userId ? message.recipientId : message.senderId;
        if (!this.chatHistory[otherUserId]) {
          this.chatHistory[otherUserId] = [];
        }
        this.chatHistory[otherUserId].push(message);
        this.chatHistory[otherUserId].sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
      })
    );

    this.subscriptions.push(
        this.signalrService.systemMessages$.subscribe(msg => {
            this.systemMessages.push(msg);
            if (this.systemMessages.length > 5) {
              this.systemMessages.shift();
            }
        })
    );
  }

  selectUser(user: User): void {
    this.selectedUser = user;
    if (!this.chatHistory[user.userId]) {
      this.chatHistory[user.userId] = [];
    }
  }

  sendMessage(): void {
    if (this.messageInput.trim() && this.selectedUser) {
      this.signalrService.sendPrivateMessage(this.selectedUser.userId, this.messageInput).then(() => {
        this.messageInput = '';
      }).catch(err => {
        console.error('Failed to send message:', err);
        this.systemMessages.push(`Failed to send message: ${err}`);
      });
    }
  }

  getMessagesForChat(userId: string): ChatMessage[] {
    return this.chatHistory[userId] || [];
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.signalrService.stopConnection();
  }
}
