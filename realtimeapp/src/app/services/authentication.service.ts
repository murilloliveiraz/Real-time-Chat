import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environment';
import { User } from '../interfaces/User';
import { Router } from '@angular/router';
import { LoginResponse } from '../interfaces/LoginResponse';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthenticationService {
  private readonly apiUrl = `${environment.APIURL}${environment.APIAUTH}`;

  private _currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;

  constructor(private http: HttpClient, private router: Router) {
    const storedToken = localStorage.getItem('jwtToken');
    const storedUser = localStorage.getItem('currentUser');
    const initialUser = storedUser ? JSON.parse(storedUser) : null;
    this._currentUserSubject = new BehaviorSubject<User | null>(initialUser);
    this.currentUser$ = this._currentUserSubject.asObservable();

    if (storedToken && initialUser) {
        this._currentUserSubject.next(initialUser);
    }
  }

  public get currentUserValue(): User | null {
    return this._currentUserSubject.value;
  }

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { username, password }).pipe(
      tap(response => {
        localStorage.setItem('jwtToken', response.token);
        const user: User = { userId: response.userId, username: response.username };
        localStorage.setItem('currentUser', JSON.stringify(user));
        this._currentUserSubject.next(user);
        this.router.navigate(['/chat']);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('currentUser');
    this._currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('jwtToken');
  }
}
