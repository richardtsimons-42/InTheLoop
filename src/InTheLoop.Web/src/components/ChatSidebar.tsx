import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

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

export default function ChatSidebar() {
  const navigate = useNavigate();
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const fetchConversations = async () => {
    try {
      const token = localStorage.getItem('token');
      const res = await fetch('http://localhost:5000/api/contacts', {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.ok) {
        const data = await res.json();
        setConversations(data);
      }
    } catch (err) {
      console.error('Failed to load conversations:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchConversations();
    const interval = setInterval(fetchConversations, 10000); // Poll every 10s as fallback
    return () => clearInterval(interval);
  }, []);

  const handleSelectConversation = (id: string) => {
    setSelectedId(id);
    navigate(`/chat/${id}`);
  };

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'now';
    if (diffMins < 60) return `${diffMins}m`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}d`;
    return date.toLocaleDateString();
  };

  const truncateMessage = (msg: string, maxLen = 30) => {
    return msg.length > maxLen ? msg.substring(0, maxLen) + '...' : msg;
  };

  if (loading) return <div style={{ padding: 20 }}>Loading chats...</div>;

  return (
    <div
      style={{
        width: 320,
        borderRight: '1px solid #eee',
        backgroundColor: '#fafafa',
        overflowY: 'auto',
      }}
    >
      <div style={{ padding: '16px', borderBottom: '1px solid #eee', fontWeight: 'bold', fontSize: 18 }}>
        Messages
      </div>
      {conversations.length === 0 ? (
        <div style={{ padding: 20, color: '#999', textAlign: 'center' }}>
          No conversations yet. Start chatting!
        </div>
      ) : (
        conversations.map(conv => (
          <div
            key={conv.id}
            onClick={() => handleSelectConversation(conv.id)}
            style={{
              display: 'flex',
              alignItems: 'center',
              padding: '12px 16px',
              cursor: 'pointer',
              borderBottom: '1px solid #f0f0f0',
              backgroundColor: selectedId === conv.id ? '#e8f4fd' : 'transparent',
              transition: 'background-color 0.15s',
            }}
            onMouseEnter={e => {
              if (selectedId !== conv.id) e.currentTarget.style.backgroundColor = '#f5f5f5';
            }}
            onMouseLeave={e => {
              if (selectedId !== conv.id) e.currentTarget.style.backgroundColor = 'transparent';
            }}
          >
            {/* Avatar */}
            <div
              style={{
                width: 48,
                height: 48,
                borderRadius: '50%',
                backgroundColor: conv.conversationType === 'family' ? '#2c3e50' : '#3498db',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                fontSize: 18,
                fontWeight: 'bold',
                flexShrink: 0,
                overflow: 'hidden',
              }}
            >
              {conv.partnerAvatar ? (
                <img src={conv.partnerAvatar} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
              ) : (
                conv.conversationType === 'family' ? '👨‍👩‍👧' : conv.partnerName.charAt(0).toUpperCase()
              )}
            </div>

            {/* Content */}
            <div style={{ flex: 1, marginLeft: 12, minWidth: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontWeight: 600, fontSize: 14, color: '#333' }}>
                  {conv.partnerName}
                  {conv.conversationType === 'family' && <span style={{ fontSize: 11, color: '#999', marginLeft: 4 }}>(group)</span>}
                </span>
                <span style={{ fontSize: 11, color: '#999', flexShrink: 0 }}>{formatTime(conv.lastMessageTime)}</span>
              </div>
              <div style={{ fontSize: 13, color: '#666', marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {conv.unreadCount > 0 && <span style={{ color: '#3498db', fontWeight: 500 }}>{conv.unreadCount} unread · </span>}
                {truncateMessage(conv.lastMessage)}
              </div>
            </div>

            {/* Unread badge */}
            {conv.unreadCount > 0 && (
              <div
                style={{
                  backgroundColor: '#3498db',
                  color: 'white',
                  borderRadius: 12,
                  padding: '2px 8px',
                  fontSize: 11,
                  fontWeight: 'bold',
                  marginLeft: 8,
                }}
              >
                {conv.unreadCount}
              </div>
            )}
          </div>
        ))
      )}
    </div>
  );
}
