import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

interface UserStatus {
  userId: string;
  isOnline: boolean;
  lastSeen: string;
}

interface UserStatusProps {
  userId: string;
  userName?: string;
}

export default function UserStatus({ userId, userName }: UserStatusProps) {
  const [status, setStatus] = useState<UserStatus | null>(null);

  useEffect(() => {
    // Load initial status
    loadUserStatus();

    // Setup SignalR listener for status updates
    const token = localStorage.getItem('token');
    if (!token) return;

    const hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    hubConnection.on('UserStatusChanged', (data: UserStatus) => {
      if (data.userId === userId) {
        setStatus(data);
      }
    });

    hubConnection.start().catch(err => console.error('SignalR connection failed:', err));

    return () => {
      hubConnection.stop();
    };
  }, [userId]);

  const loadUserStatus = async () => {
    try {
      const response = await fetch(`/api/users/${userId}/status`);
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      }
    } catch (error) {
      console.error('Failed to load user status:', error);
    }
  };

  if (!status) return null;

  const formatLastSeen = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return date.toLocaleDateString();
  };

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <div
        style={{
          width: 10,
          height: 10,
          borderRadius: '50%',
          backgroundColor: status.isOnline ? '#2ecc71' : '#95a5a6',
          border: '2px solid white',
        }}
      />
      <span style={{ fontSize: 12, color: '#666' }}>
        {status.isOnline ? 'Online' : `Last seen ${formatLastSeen(status.lastSeen)}`}
      </span>
    </div>
  );
}
