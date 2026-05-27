import { useState } from 'react';
import { postsApi } from '../services/api';
import '../index.css';

interface Comment {
  id: number;
  content: string;
  authorName: string;
  authorAvatar: string | null;
  postId: number;
  parentCommentId: number | null;
  replies: Comment[];
  createdAt: string;
}

interface Post {
  id: number;
  content: string;
  authorName: string;
  authorAvatar: string | null;
  familyId: number;
  familyName: string;
  photoUrls: string[];
  comments: Comment[];
  createdAt: string;
}

export default function PostCard({ post }: { post: Post }) {
  const [showComments, setShowComments] = useState(false);
  const [commentText, setCommentText] = useState('');
  const [replyingTo, setReplyingTo] = useState<number | null>(null);
  const [replyTexts, setReplyTexts] = useState<Record<number, string>>({});

  const handleComment = async () => {
    if (!commentText.trim()) return;
    try {
      await postsApi.addComment(post.id, commentText);
      setCommentText('');
      window.dispatchEvent(new CustomEvent('refresh-feed'));
    } catch (error) {
      console.error('Failed to add comment:', error);
    }
  };

  const handleReply = async (parentCommentId: number) => {
    const text = replyTexts[parentCommentId] || '';
    if (!text.trim()) return;
    try {
      await postsApi.addReply(parentCommentId, post.id, text);
      setReplyingTo(null);
      setReplyTexts(prev => {
        const next = { ...prev };
        delete next[parentCommentId];
        return next;
      });
      window.dispatchEvent(new CustomEvent('refresh-feed'));
    } catch (error) {
      console.error('Failed to add reply:', error);
    }
  };

  const setReplyText = (commentId: number, text: string) => {
    setReplyTexts(prev => ({ ...prev, [commentId]: text }));
  };

  return (
    <div className="card fade-in" style={{ marginBottom: 'var(--space-lg)', padding: 0, overflow: 'hidden' }}>
      {/* Author Header */}
      <div style={{ padding: 'var(--space-lg) var(--space-xl)', display: 'flex', alignItems: 'center', gap: 'var(--space-md)' }}>
        <div className="avatar" style={{ background: 'var(--color-brand-gradient)' }}>
          {post.authorAvatar ? (
            <img src={post.authorAvatar} alt="" />
          ) : (
            post.authorName?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
          )}
        </div>
        <div style={{ flex: 1 }}>
          <div style={{ fontWeight: 700, fontSize: 15 }}>{post.authorName}</div>
          <div style={{ fontSize: 12, color: 'var(--color-text-tertiary)' }}>
            {getTimeAgo(post.createdAt)} · {post.familyName}
          </div>
        </div>
      </div>

      {/* Content */}
      <div style={{ padding: '0 var(--space-xl) var(--space-lg)' }}>
        <p style={{ fontSize: 15, lineHeight: 1.6, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {post.content}
        </p>

        {/* Photos */}
        {post.photoUrls.length > 0 && (
          <div style={{
            marginTop: 'var(--space-md)',
            borderRadius: 'var(--radius-md)',
            overflow: 'hidden',
            maxHeight: 400,
          }}>
            <img
              src={post.photoUrls[0]}
              alt="Post photo"
              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
            />
          </div>
        )}
      </div>

      {/* Actions Bar */}
      <div style={{
        padding: 'var(--space-md) var(--space-xl)',
        borderTop: '1px solid var(--color-border-light)',
        display: 'flex',
        gap: 'var(--space-xs)',
      }}>
        <button
          onClick={() => setShowComments(!showComments)}
          className="btn btn-ghost"
          style={{
            fontSize: 14,
            color: showComments ? 'var(--color-brand)' : 'var(--color-text-secondary)',
            background: showComments ? 'var(--color-brand-light)' : 'transparent',
          }}
        >
          💬 {post.comments.length} {post.comments.length === 1 ? 'comment' : 'comments'}
        </button>
      </div>

      {/* Comments Section */}
      {showComments && (
        <div style={{
          padding: 'var(--space-lg) var(--space-xl) var(--space-xl)',
          borderTop: '1px solid var(--color-border-light)',
          backgroundColor: 'var(--color-bg)',
        }}>
          {/* Comment Input */}
          <div style={{ display: 'flex', gap: 'var(--space-md)', marginBottom: 'var(--space-lg)' }}>
            <div className="avatar avatar-sm" style={{ background: 'var(--color-brand-gradient)', flexShrink: 0 }}>
              {post.authorName?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
            </div>
            <div style={{ flex: 1, display: 'flex', gap: 'var(--space-sm)' }}>
              <input
                type="text"
                className="input"
                placeholder="Write a comment..."
                value={commentText}
                onChange={e => setCommentText(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleComment()}
                style={{ fontSize: 14 }}
              />
              <button
                onClick={handleComment}
                className="btn btn-primary"
                disabled={!commentText.trim()}
                style={{ flexShrink: 0 }}
              >
                Send
              </button>
            </div>
          </div>

          {/* Comments List */}
          {post.comments.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
              {post.comments.filter(c => !c.parentCommentId).map(comment => (
                <CommentItem
                  key={comment.id}
                  comment={comment}
                  postId={post.id}
                  replyingTo={replyingTo}
                  replyText={replyingTo === comment.id ? (replyTexts[comment.id] || '') : ''}
                  onReply={(id) => {
                    setReplyingTo(replyingTo === id ? null : id);
                  }}
                  onReplyTextChange={(id, text) => setReplyText(id, text)}
                  onSendReply={() => handleReply(comment.id)}
                />
              ))}
            </div>
          ) : (
            <p style={{ color: 'var(--color-text-tertiary)', textAlign: 'center', padding: 'var(--space-xl) 0', fontSize: 14 }}>
              No comments yet. Be the first!
            </p>
          )}
        </div>
      )}
    </div>
  );
}

/* --- Comment Item Sub-component --- */
function CommentItem({
  comment,
  postId,
  replyingTo,
  replyText,
  onReply,
  onReplyTextChange,
  onSendReply,
}: {
  comment: Comment;
  postId: number;
  replyingTo: number | null;
  replyText: string;
  onReply: (id: number) => void;
  onReplyTextChange: (id: number, text: string) => void;
  onSendReply: () => void;
}) {
  return (
    <div style={{
      display: 'flex',
      gap: 'var(--space-md)',
      padding: 'var(--space-md)',
      backgroundColor: 'var(--color-surface)',
      borderRadius: 'var(--radius-md)',
    }}>
      <div className="avatar avatar-sm" style={{
        background: 'var(--color-brand-gradient)',
        flexShrink: 0,
        fontSize: 12,
      }}>
        {comment.authorName?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-sm)', marginBottom: 'var(--space-xs)' }}>
          <strong style={{ fontSize: 14 }}>{comment.authorName}</strong>
          <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)' }}>{getTimeAgo(comment.createdAt)}</span>
        </div>
        <p style={{ fontSize: 14, lineHeight: 1.5, marginBottom: 'var(--space-sm)' }}>{comment.content}</p>
        <button
          onClick={() => onReply(comment.id)}
          className="btn btn-ghost"
          style={{ fontSize: 12, padding: 'var(--space-xs) var(--space-sm)' }}
        >
          Reply
        </button>

        {/* Reply Input — shown when replying to this comment */}
        {replyingTo === comment.id && (
          <div style={{ marginTop: 'var(--space-md)', display: 'flex', gap: 'var(--space-sm)' }}>
            <input
              type="text"
              className="input"
              placeholder="Write a reply..."
              value={replyText}
              onChange={e => onReplyTextChange(comment.id, e.target.value)}
              onKeyDown={e => e.key === 'Enter' && onSendReply()}
              style={{ fontSize: 13, padding: 'var(--space-sm) var(--space-md)' }}
            />
            <button onClick={onSendReply} className="btn btn-primary btn-sm">Reply</button>
          </div>
        )}

        {/* Nested Replies */}
        {comment.replies?.length > 0 && (
          <div style={{ marginTop: 'var(--space-md)', display: 'flex', flexDirection: 'column', gap: 'var(--space-sm)' }}>
            {comment.replies.map(reply => (
              <ReplyItem
                key={reply.id}
                reply={reply}
                postId={postId}
                parentCommentId={comment.id}
                replyingTo={replyingTo}
                replyText={replyingTo === comment.id ? (replyTexts[comment.id] || '') : ''}
                onReply={(parentId) => onReply(parentId)}
                onReplyTextChange={(parentId, text) => onReplyTextChange(parentId, text)}
                onSendReply={() => onSendReply()}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/* --- Nested Reply Item --- */
function ReplyItem({
  reply,
  postId,
  parentCommentId,
  replyingTo,
  replyText,
  onReply,
  onReplyTextChange,
  onSendReply,
}: {
  reply: Comment;
  postId: number;
  parentCommentId: number;
  replyingTo: number | null;
  replyText: string;
  onReply: (id: number) => void;
  onReplyTextChange: (id: number, text: string) => void;
  onSendReply: () => void;
}) {
  return (
    <div style={{
      marginLeft: 'var(--space-lg)',
      padding: 'var(--space-sm) var(--space-md)',
      backgroundColor: 'var(--color-bg)',
      borderRadius: 'var(--radius-sm)',
      fontSize: 13,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-sm)', marginBottom: '2px' }}>
        <strong>{reply.authorName}</strong>
        <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)' }}>{getTimeAgo(reply.createdAt)}</span>
      </div>
      <div style={{ marginBottom: 'var(--space-xs)' }}>{reply.content}</div>
      <button
        onClick={() => onReply(parentCommentId)}
        className="btn btn-ghost"
        style={{ fontSize: 11, padding: 'var(--space-xs) var(--space-sm)' }}
      >
        Reply
      </button>

      {/* Reply input for nested replies — shows on the parent comment */}
      {replyingTo === parentCommentId && (
        <div style={{ marginTop: 'var(--space-sm)', display: 'flex', gap: 'var(--space-sm)' }}>
          <input
            type="text"
            className="input"
            placeholder="Write a reply..."
            value={replyText}
            onChange={e => onReplyTextChange(parentCommentId, e.target.value)}
            onKeyDown={e => e.key === 'Enter' && onSendReply()}
            style={{ fontSize: 12, padding: 'var(--space-xs) var(--space-sm)' }}
          />
          <button onClick={onSendReply} className="btn btn-primary btn-sm">Reply</button>
        </div>
      )}
    </div>
  );
}

/* --- Time Utilities --- */
function getTimeAgo(date: string): string {
  const seconds = Math.floor((Date.now() - new Date(date).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}
