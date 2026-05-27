import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import UserStatus from '../components/UserStatus';
import * as signalR from '@microsoft/signalr';
import '../index.css';

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
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
}

export default function ChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const navigate = useNavigate();
  const { currentUserId } = useAuth();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const hubConnectionRef = useRef<signalR.HubConnection | null>(null);

  const hasConversationId = !!conversationId;

  // Load conversations list
  useEffect(() => {
    const token = localStorage.getItem('token');
    fetch('/api/contacts', {
      headers: { Authorization: `Bearer ${token}` },
    })
      .then(r => r.ok ? r.json() : [])
      .then(data => {
        setConversations(data);
        if (!hasConversationId && data.length > 0) {
          // Auto-select first conversation
          navigate(`/chat/${data[0].id}`, { replace: true });
        }
      })
      .catch(err => console.error('Failed to load conversations:', err));
  }, []);

  // Load conversation + messages when conversationId changes
  useEffect(() => {
    if (!hasConversationId) return;

    setLoading(true);

    // Load conversation info
    const found = conversations.find(c => c.id === conversationId);
    if (found) setConversation(found);

    // Load message history
    fetch(`/api/contacts/${conversationId}`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('token')}` },
    })
      .then(r => r.ok ? r.json() : [])
      .then(data => {
        setMessages(data);
        setLoading(false);
      })
      .catch(err => {
        console.error('Failed to load messages:', err);
        setLoading(false);
      });

    // Setup SignalR
    const token = localStorage.getItem('token');
    const hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: () => token! })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .build();

    hubConnection.on('ReceiveMessage', (data: any) => {
      if (conversation?.conversationType === 'dm' && conversation.conversationId === data.recipientId) {
        const msg: ChatMessage = {
          id: data.id, content: data.content, sentAt: data.sentAt,
          isOwn: data.senderId === currentUserId, senderName: data.senderName,
        };
        setMessages(prev => { if (prev.some(m => m.id === msg.id)) return prev; return [...prev, msg]; });
        scrollToBottom();
      }
    });

    hubConnection.on('ReceiveFamilyMessage', (data: any) => {
      if (conversation?.conversationType === 'family' && conversation.conversationId === data.familyId) {
        const msg: ChatMessage = {
          id: data.id, content: data.content, sentAt: data.sentAt,
          isOwn: false, senderName: data.senderName,
        };
        setMessages(prev => { if (prev.some(m => m.id === msg.id)) return prev; return [...prev, msg]; });
        scrollToBottom();
      }
    });

    hubConnection.start().catch(err => console.error('SignalR failed:', err));
    hubConnectionRef.current = hubConnection;

    return () => hubConnection.stop();
  }, [conversationId]);

  useEffect(() => { scrollToBottom(); }, [messages]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const sendMessage = async () => {
    if (!input.trim() || sending) return;
    setSending(true);
    const content = input.trim();
    setInput('');

    try {
      if (conversation?.conversationType === 'family' && conversation.conversationId) {
        await hubConnectionRef.current?.invoke('SendFamilyMessage', conversation.conversationId, content);
      } else {
        const otherId = conversationId?.replace('dm-', '');
        const res = await fetch('/api/messages', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token')}` },
          body: JSON.stringify({ recipientId: otherId, content }),
        });
        if (res.ok) {
          const sentMsg: ChatMessage = {
            id: Date.now(), content, sentAt: new Date().toISOString(),
            isOwn: true, senderName: currentUserId ? 'You' : 'Unknown',
          };
          setMessages(prev => [...prev, sentMsg]);
        }
      }
    } catch (err) {
      console.error('Failed to send:', err);
      setInput(content);
    }
    setSending(false);
  };

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const formatListTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'now';
    if (diffMins < 60) return `${diffMins}m`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h`;
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  const truncateMessage = (msg: string, maxLen = 35) => {
    return msg.length > maxLen ? msg.substring(0, maxLen) + '...' : msg;
  };

  // === MOBILE: Show conversation list ===
  if (!hasConversationId) {
    return (
      <div className="container" style={{ paddingTop: 'var(--space-xl)', paddingBottom: 'var(--space-3xl)' }}>
        <h1 style={{ fontSize: 28, fontWeight: 800, marginBottom: 'var(--space-xl)', letterSpacing: '-0.5px' }}>
          Messages
        </h1>
        {conversations.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">💬</div>
            <div className="empty-state-title">No conversations yet</div>
            <p>Start a conversation with a family member</p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-xs)' }}>
            {conversations.map(conv => (
              <button
                key={conv.id}
                onClick={() => navigate(`/chat/${conv.id}`)}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 'var(--space-md)',
                  padding: 'var(--space-md)',
                  background: 'var(--color-surface)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--radius-md)',
                  cursor: 'pointer',
                  textAlign: 'left',
                  width: '100%',
                  transition: 'all var(--transition-fast)',
                }}
                onMouseEnter={e => {
                  e.currentTarget.style.backgroundColor = 'var(--color-surface-hover)';
                  e.currentTarget.style.borderColor = 'var(--color-text-tertiary)';
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.backgroundColor = 'var(--color-surface)';
                  e.currentTarget.style.borderColor = 'var(--color-border)';
                }}
              >
                {/* Avatar */}
                <div style={{
                  width: 48, height: 48, borderRadius: 'var(--radius-full)',
                  background: conv.conversationType === 'family' ? '#2c3e50' : 'var(--color-brand-gradient)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  color: 'white', fontSize: 18, fontWeight: 700, flexShrink: 0, overflow: 'hidden',
                }}>
                  {conv.partnerAvatar ? (
                    <img src={conv.partnerAvatar} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                  ) : conv.conversationType === 'family' ? '👨‍👩‍👧' : conv.partnerName?.charAt(0).toUpperCase()}
                </div>
                {/* Info */}
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2px' }}>
                    <span style={{ fontWeight: 600, fontSize: 15 }}>{conv.partnerName}</span>
                    <span style={{ fontSize: 12, color: 'var(--color-text-tertiary)', flexShrink: 0 }}>{formatListTime(conv.lastMessageTime)}</span>
                  </div>
                  <div style={{ fontSize: 13, color: 'var(--color-text-secondary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {conv.unreadCount > 0 && <span style={{ color: 'var(--color-brand)', fontWeight: 500 }}>{conv.unreadCount} unread · </span>}
                    {truncateMessage(conv.lastMessage)}
                  </div>
                </div>
                {conv.unreadCount > 0 && (
                  <span className="badge badge-brand" style={{ flexShrink: 0 }}>{conv.unreadCount}</span>
                )}
              </button>
            ))}
          </div>
        )}
      </div>
    );
  }

  if (!conversation) {
    return (
      <div className="container" style={{ paddingTop: 'var(--space-xl)' }}>
        <div className="empty-state">
          <div className="empty-state-icon">🔍</div>
          <div className="empty-state-title">Conversation not found</div>
          <button className="btn btn-primary" onClick={() => navigate('/chat')}>Back to Messages</button>
        </div>
      </div>
    );
  }

  // === DESKTOP: Split layout | MOBILE: Full chat view ===
  return (
    <div style={{
      display: 'flex',
      height: 'calc(100vh - 60px)',
      maxWidth: 1100,
      margin: '0 auto',
      background: 'var(--color-surface)',
      borderLeft: '1px solid var(--color-border)',
      borderRight: '1px solid var(--color-border)',
      overflow: 'hidden',
    }}>
      {/* Sidebar — hidden on mobile when chat is open */}
      <div style={{
        width: 340,
        borderRight: '1px solid var(--color-border)',
        overflowY: 'auto',
        display: 'none',
      }} className="desktop-only">
        <div style={{ padding: 'var(--space-lg)', borderBottom: '1px solid var(--color-border)', fontWeight: 700, fontSize: 18 }}>
          Messages
        </div>
        {conversations.map(conv => (
          <button
            key={conv.id}
            onClick={() => navigate(`/chat/${conv.id}`)}
            style={{
              display: 'flex', alignItems: 'center', gap: 'var(--space-md)',
              padding: 'var(--space-md) var(--space-lg)',
              width: '100%', border: 'none', background: 'transparent',
              cursor: 'pointer', borderBottom: '1px solid var(--color-border-light)',
              textAlign: 'left',
              backgroundColor: conversationId === conv.id ? 'var(--color-brand-light)' : 'transparent',
              transition: 'background var(--transition-fast)',
            }}
          >
            <div style={{
              width: 44, height: 44, borderRadius: 'var(--radius-full)',
              background: conv.conversationType === 'family' ? '#2c3e50' : 'var(--color-brand-gradient)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: 'white', fontSize: 16, fontWeight: 700, flexShrink: 0, overflow: 'hidden',
            }}>
              {conv.partnerAvatar ? (
                <img src={conv.partnerAvatar} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
              ) : conv.conversationType === 'family' ? '👨‍👩‍👧' : conv.partnerName?.charAt(0).toUpperCase()}
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2px' }}>
                <span style={{ fontWeight: 600, fontSize: 14 }}>{conv.partnerName}</span>
                <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)' }}>{formatListTime(conv.lastMessageTime)}</span>
              </div>
              <div style={{ fontSize: 13, color: 'var(--color-text-secondary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {truncateMessage(conv.lastMessage)}
              </div>
            </div>
          </button>
        ))}
      </div>

      {/* Chat Area */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        {/* Header */}
        <div style={{
          padding: 'var(--space-md) var(--space-lg)',
          borderBottom: '1px solid var(--color-border)',
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-md)',
          background: 'rgba(255,255,255,0.85)',
          backdropFilter: 'blur(12px)',
        }}>
          {/* Mobile back button */}
          <button
            onClick={() => navigate('/chat')}
            style={{
              display: 'none',
              background: 'none', border: 'none', cursor: 'pointer',
              fontSize: 20, color: 'var(--color-brand)', padding: 'var(--space-sm)',
            }}
            className="mobile-only"
          >
            ← Back
          </button>

          <div style={{
            width: 40, height: 40, borderRadius: 'var(--radius-full)',
            background: conversation.conversationType === 'family' ? '#2c3e50' : 'var(--color-brand-gradient)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: 'white', fontSize: 16, fontWeight: 700, overflow: 'hidden',
          }}>
            {conversation.partnerAvatar ? (
              <img src={conversation.partnerAvatar} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
            ) : conversation.conversationType === 'family' ? '👨‍👩‍👧' : conversation.partnerName?.charAt(0).toUpperCase()}
          </div>
          <div>
            <div style={{ fontWeight: 700, fontSize: 15 }}>{conversation.partnerName}</div>
            <div style={{ fontSize: 12, color: 'var(--color-text-tertiary)' }}>
              {conversation.conversationType === 'family' ? 'Group' : 'Direct Message'}
            </div>
          </div>
        </div>

        {/* Messages */}
        <div style={{
          flex: 1, overflowY: 'auto', padding: 'var(--space-lg)',
          background: 'var(--color-bg)',
        }}>
          {messages.length === 0 ? (
            <div className="empty-state" style={{ marginTop: 'var(--space-3xl)' }}>
              <div className="empty-state-icon">👋</div>
              <div className="empty-state-title">No messages yet</div>
              <p>Say hello!</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-sm)' }}>
              {messages.map(msg => (
                <div
                  key={msg.id}
                  style={{
                    display: 'flex',
                    justifyContent: msg.isOwn ? 'flex-end' : 'flex-start',
                  }}
                >
                  <div style={{
                    maxWidth: '75%',
                    padding: 'var(--space-md) var(--space-lg)',
                    borderRadius: msg.isOwn ? 'var(--radius-xl) var(--radius-xl) 4px var(--radius-xl)' : 'var(--radius-xl) var(--radius-xl) var(--radius-xl) 4px',
                    background: msg.isOwn ? 'var(--color-brand)' : 'var(--color-surface)',
                    color: msg.isOwn ? 'white' : 'var(--color-text)',
                    boxShadow: msg.isOwn ? 'none' : 'var(--shadow-sm)',
                  }}>
                    {!msg.isOwn && (
                      <div style={{ fontSize: 11, fontWeight: 700, marginBottom: 'var(--space-xs)', color: 'var(--color-brand)' }}>
                        {msg.senderName}
                      </div>
                    )}
                    <div style={{ fontSize: 14, lineHeight: 1.5, wordBreak: 'break-word' }}>{msg.content}</div>
                    <div style={{ fontSize: 10, marginTop: 'var(--space-xs)', textAlign: 'right', opacity: msg.isOwn ? 0.7 : 0.5 }}>
                      {formatTime(msg.sentAt)}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Input */}
        <div style={{
          padding: 'var(--space-md) var(--space-lg)',
          borderTop: '1px solid var(--color-border)',
          background: 'var(--color-surface)',
        }}>
          <form onSubmit={e => { e.preventDefault(); sendMessage(); }} style={{ display: 'flex', gap: 'var(--space-sm)' }}>
            <input
              type="text"
              className="input"
              placeholder={`Message ${conversation.partnerName}`}
              value={input}
              onChange={e => setInput(e.target.value)}
              disabled={sending}
              style={{ fontSize: 14, borderRadius: 'var(--radius-full)', padding: 'var(--space-md) var(--space-lg)' }}
            />
            <button
              type="submit"
              className="btn btn-primary"
              disabled={sending || !input.trim()}
              style={{ borderRadius: 'var(--radius-full)', padding: 'var(--space-md) var(--space-xl)' }}
            >
              {sending ? <span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> : 'Send'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
