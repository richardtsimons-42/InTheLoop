import { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import UserStatus from '../components/UserStatus';
import * as signalR from '@microsoft/signalr';

interface ChatMessage {
  id: number;
  content: string;
  sentAt: string;
  isOwn: boolean;
  senderName: string;
}

interface Conversation {
  id: string;
  conversationType: 'dm' | 'family';
  conversationId: number | null;
  partnerName: string;
  partnerAvatar: string | null;
}

export default function ChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const { currentUserId } = useAuth();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const hubConnectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!conversationId) {
      // No conversation selected - load conversations list and set loading to false
      setLoading(false);
      return;
    }

    // Load conversation info
    loadConversationInfo();

    // Load message history
    loadMessages();

    // Setup SignalR
    const token = localStorage.getItem('token');
    const hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => token!,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .build();

    hubConnection.on('ReceiveMessage', (data: any) => {
      // DM message
      const msg: ChatMessage = {
        id: data.id,
        content: data.content,
        sentAt: data.sentAt,
        isOwn: data.senderId === currentUserId,
        senderName: data.senderName,
      };
      setMessages(prev => {
        if (prev.some(m => m.id === msg.id)) return prev;
        return [...prev, msg];
      });
      scrollToBottom();
    });

    hubConnection.on('ReceiveFamilyMessage', (data: any) => {
      const msg: ChatMessage = {
        id: data.id,
        content: data.content,
        sentAt: data.sentAt,
        isOwn: false,
        senderName: data.senderName,
      };
      setMessages(prev => {
        if (prev.some(m => m.id === msg.id)) return prev;
        return [...prev, msg];
      });
      scrollToBottom();
    });

    hubConnection.start().catch(err => console.error('SignalR connection failed:', err));
    hubConnectionRef.current = hubConnection;

    return () => {
      hubConnection.stop();
    };
  }, [conversationId]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const loadConversationInfo = async () => {
    const token = localStorage.getItem('token');
    try {
      const res = await fetch('/api/contacts', {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.ok) {
        const data: Conversation[] = await res.json();
        const found = data.find(c => c.id === conversationId);
        if (found) setConversation(found);
      }
    } catch (err) {
      console.error('Failed to load conversation info:', err);
    }
  };

  const loadMessages = async () => {
    if (!conversationId) return;
    const token = localStorage.getItem('token');
    try {
      const res = await fetch(`/api/contacts/${conversationId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.ok) {
        const data = await res.json();
        setMessages(data);
      }
    } catch (err) {
      console.error('Failed to load messages:', err);
    } finally {
      setLoading(false);
    }
  };

  const sendMessage = async () => {
    if (!input.trim() || sending) return;

    setSending(true);
    const token = localStorage.getItem('token');
    const content = input.trim();
    setInput('');

    try {
      if (conversation?.conversationType === 'family' && conversation.conversationId) {
        // Send via SignalR to family
        await hubConnectionRef.current?.invoke('SendFamilyMessage', conversation.conversationId, content);
      } else {
        // Send DM via REST
        const otherId = conversationId?.replace('dm-', '');
        const res = await fetch('/api/messages', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            recipientId: otherId,
            content,
          }),
        });
        if (res.ok) {
          // Optimistically add the sent message
          const sentMsg: ChatMessage = {
            id: Date.now(),
            content,
            sentAt: new Date().toISOString(),
            isOwn: true,
            senderName: currentUserId ? 'You' : 'Unknown',
          };
          setMessages(prev => [...prev, sentMsg]);
        }
      }
    } catch (err) {
      console.error('Failed to send message:', err);
      setInput(content); // Restore input on failure
    }

    setSending(false);
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}>Loading chat...</div>;
  if (!conversationId) {
    // No conversation selected - show list of conversations
    return (
      <div style={{ maxWidth: 600, margin: '40px auto', padding: 20 }}>
        <h1>Messages</h1>
        <p style={{ color: '#999', textAlign: 'center' }}>Select a conversation to start chatting</p>
      </div>
    );
  }
  if (!conversation) return <div style={{ padding: 40, textAlign: 'center' }}>Conversation not found</div>;

  return (
    <div style={{ display: 'flex', height: 'calc(100vh - 56px)', maxWidth: 900, margin: '0 auto' }}>
      {/* Chat area */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <div
          style={{
            padding: '16px 24px',
            borderBottom: '1px solid #eee',
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            backgroundColor: '#fff',
          }}
        >
          <div
            style={{
              width: 40,
              height: 40,
              borderRadius: '50%',
              backgroundColor: conversation.conversationType === 'family' ? '#2c3e50' : '#3498db',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'white',
              fontSize: 16,
              fontWeight: 'bold',
              overflow: 'hidden',
            }}
          >
            {conversation.partnerAvatar ? (
              <img src={conversation.partnerAvatar} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
            ) : (
              conversation.conversationType === 'family' ? '👨‍👩‍👧' : conversation.partnerName.charAt(0).toUpperCase()
            )}
          </div>
          <div>
            <div style={{ fontWeight: 600 }}>{conversation.partnerName}</div>
            <div style={{ fontSize: 12, color: '#999' }}>
              {conversation.conversationType === 'family' ? 'Group' : 'Direct Message'}
            </div>
            {conversation.conversationType === 'dm' && (
              <UserStatus userId={conversationId?.replace('dm-', '') || ''} userName={conversation.partnerName} />
            )}
          </div>
        </div>

        {/* Messages */}
        <div
          style={{
            flex: 1,
            overflowY: 'auto',
            padding: '20px 24px',
            backgroundColor: '#f7f8fa',
          }}
        >
          {messages.length === 0 ? (
            <div style={{ textAlign: 'center', color: '#999', marginTop: 40 }}>
              No messages yet. Say hello!
            </div>
          ) : (
            messages.map(msg => (
              <div
                key={msg.id}
                style={{
                  display: 'flex',
                  justifyContent: msg.isOwn ? 'flex-end' : 'flex-start',
                  marginBottom: 12,
                }}
              >
                <div
                  style={{
                    maxWidth: '70%',
                    padding: '10px 16px',
                    borderRadius: msg.isOwn ? '18px 18px 4px 18px' : '18px 18px 18px 4px',
                    backgroundColor: msg.isOwn ? '#3498db' : '#fff',
                    color: msg.isOwn ? 'white' : '#333',
                    boxShadow: msg.isOwn ? 'none' : '0 1px 2px rgba(0,0,0,0.06)',
                  }}
                >
                  {!msg.isOwn && (
                    <div style={{ fontSize: 11, fontWeight: 600, marginBottom: 4, color: '#2c3e50' }}>
                      {msg.senderName}
                    </div>
                  )}
                  <div style={{ fontSize: 14, lineHeight: 1.4 }}>{msg.content}</div>
                  <div
                    style={{
                      fontSize: 10,
                      marginTop: 4,
                      textAlign: 'right',
                      opacity: msg.isOwn ? 0.7 : 0.5,
                    }}
                  >
                    {formatTime(msg.sentAt)}
                  </div>
                </div>
              </div>
            ))
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Input */}
        <div
          style={{
            padding: '16px 24px',
            borderTop: '1px solid #eee',
            backgroundColor: '#fff',
          }}
        >
          <form
            onSubmit={e => {
              e.preventDefault();
              sendMessage();
            }}
            style={{ display: 'flex', gap: 12 }}
          >
            <input
              type="text"
              value={input}
              onChange={e => setInput(e.target.value)}
              placeholder={conversation.conversationType === 'family' ? `Message ${conversation.partnerName}` : `Message ${conversation.partnerName}`}
              disabled={sending}
              style={{
                flex: 1,
                padding: '12px 16px',
                border: '1px solid #ddd',
                borderRadius: 24,
                fontSize: 14,
                outline: 'none',
              }}
            />
            <button
              type="submit"
              disabled={sending || !input.trim()}
              style={{
                padding: '12px 24px',
                backgroundColor: input.trim() ? '#3498db' : '#ccc',
                color: 'white',
                border: 'none',
                borderRadius: 24,
                cursor: input.trim() ? 'pointer' : 'default',
                fontWeight: 600,
                fontSize: 14,
              }}
            >
              {sending ? '...' : 'Send'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
