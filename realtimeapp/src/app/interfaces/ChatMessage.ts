export interface ChatMessage {
  id: string;
  senderId: string;
  senderUsername: string;
  recipientId: string;
  recipientUsername: string;
  content: string;
  timestamp: Date;
}