import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environment';
import { User } from '../interfaces/User';
import { AuthenticationService } from './authentication.service';
import { ChatMessage } from '../interfaces/ChatMessage';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private readonly apiUrl = `${environment.APIURL}${environment.APICHAT}`;
  private hubConnection!: signalR.HubConnection;

  private _onlineUsersSubject: Subject<User[]> = new Subject<User[]>();
  public onlineUsers$: Observable<User[]> = this._onlineUsersSubject.asObservable();

  private _privateMessageSubject: Subject<ChatMessage> = new Subject<ChatMessage>();
  public privateMessages$: Observable<ChatMessage> = this._privateMessageSubject.asObservable();

  private _systemMessageSubject: Subject<string> = new Subject<string>();
  public systemMessages$: Observable<string> = this._systemMessageSubject.asObservable();

  constructor(private authService: AuthenticationService) { }

  public async startConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR connection already active.');
      return;
    }

    const token = this.authService.getToken();
    if (!token) {
      console.error('No JWT token found for SignalR connection.');
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.apiUrl, {
        accessTokenFactory: () => token,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.onreconnected(connectionId => {
      console.log('SignalR Recconected.Connection Id:', connectionId);
    });

    try {
      await this.hubConnection.start();
      console.log('SignalR Connection started!');
      this.addListeners();
    } catch (err) {
      console.log('Error while starting SignalR connection: ' + err);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.stop();
      console.log('SignalR Connection stopped.');
    }
  }

  public addListeners = () => {
    this.hubConnection.on('ReceiveOnlineUsers', (users: User[]) => {
      console.log('Received online users from SignalR:', users);
      this._onlineUsersSubject.next(users);
    });

    this.hubConnection.on('ReceivePrivateMessage', (message: ChatMessage) => {
      console.log('Received private message from SignalR:', message);
      this._privateMessageSubject.next(message);
    });

    this.hubConnection.on('ReceiveSystemMessage', (message: string) => {
      console.log('Received system message from SignalR:', message);
      this._systemMessageSubject.next(message);
    });
  }

  public sendPrivateMessage(recipientUserId: string, messageContent: string): Promise<void>{
    if(this.hubConnection.state === signalR.HubConnectionState.Connected){
      return this.hubConnection.invoke('SendPrivateMessage', recipientUserId, messageContent);
    }
    else {
      console.error('SignalR connection not established.');
      return Promise.reject('SignalR connection not established.');
    }
  }
}
