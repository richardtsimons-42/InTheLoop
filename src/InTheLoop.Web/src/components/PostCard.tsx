import { useState } from 'react';

interface Comment {
  id: number;
  content: string;
  authorName: string;
  authorAvatar: string | null;
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

  const timeAgo = getTimeAgo(post.createdAt);

  const handleComment = async () => {
    if (!commentText.trim()) return;
    // In a real app, you'd call the API here
    setCommentText('');
    alert('Comment posted!');
  };

  const handleReply = async (commentId: number) => {
    if (!commentText.trim()) return;
    // In a real app, you'd call the API here
    setReplyingTo(null);
    setCommentText('');
    alert('Reply posted!');
  };

  const renderComments = (comments: Comment[], depth = 0) => {
    return comments.map(comment => (
      <div key={comment.id} style={{ marginLeft: depth * 20, marginTop: 10, padding: 10, backgroundColor: depth > 0 ? '#f5f5f5' : 'transparent', borderRadius: 4 }}>
        <div style={{ display: 'flex', alignItems: 'center', marginBottom: 5 }}>
          {comment.authorAvatar && <img src={comment.authorAvatar} alt="" style={{ width: 24, height: 24, borderRadius: '50%', marginRight: 8 }} />}
          <strong style={{ fontSize: 14 }}>{comment.authorName}</strong>
          <span style={{ color: '#999', fontSize: 12, marginLeft: 8 }}>{getTimeAgo(comment.createdAt)}</span>
        </div>
        <p style={{ margin: '5px 0', fontSize: 14 }}>{comment.content}</p>
        <button
          onClick={() => setReplyingTo(replyingTo === comment.id ? null : comment.id)}
          style={{ fontSize: 12, color: '#3498db', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
        >
          Reply
        </button>
        {replyingTo === comment.id && (
          <div style={{ marginTop: 8 }}>
            <input
              type="text"
              placeholder="Write a reply..."
              value={commentText}
              onChange={e => setCommentText(e.target.value)}
              style={{ width: '100%', padding: 8, fontSize: 14, border: '1px solid #ddd', borderRadius: 4, boxSizing: 'border-box' }}
            />
            <button
              onClick={() => handleReply(comment.id)}
              style={{ marginTop: 5, padding: '4px 12px', fontSize: 12, backgroundColor: '#3498db', color: 'white', border: 'none', borderRadius: 4, cursor: 'pointer' }}
            >
              Reply
            </button>
          </div>
        )}
        {comment.replies && comment.replies.length > 0 && renderComments(comment.replies, depth + 1)}
      </div>
    ));
  };

  return (
    <div style={{ border: '1px solid #eee', padding: 16, marginBottom: 12, borderRadius: 8 }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 8 }}>
        {post.authorAvatar && <img src={post.authorAvatar} alt="" style={{ width: 40, height: 40, borderRadius: '50%' }} />}
        <div>
          <strong>{post.authorName}</strong>
          <span style={{ color: '#666', marginLeft: 8 }}>{timeAgo}</span>
        </div>
      </div>
      <p>{post.content}</p>
      {post.photoUrls.map(url => (
        <img key={url} src={url} alt="" style={{ maxWidth: '100%', borderRadius: 8, margin: '8px 0' }} />
      ))}
      
      <div style={{ marginTop: 12, borderTop: '1px solid #eee', paddingTop: 12 }}>
        <button
          onClick={() => setShowComments(!showComments)}
          style={{ fontSize: 14, color: '#3498db', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
        >
          {showComments ? 'Hide' : 'Show'} Comments ({post.comments.length})
        </button>
        
        {showComments && (
          <div style={{ marginTop: 12 }}>
            {/* Comment input */}
            <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
              <input
                type="text"
                placeholder="Write a comment..."
                value={commentText}
                onChange={e => setCommentText(e.target.value)}
                style={{ flex: 1, padding: 8, fontSize: 14, border: '1px solid #ddd', borderRadius: 4 }}
              />
              <button
                onClick={handleComment}
                style={{ padding: '8px 16px', fontSize: 14, backgroundColor: '#3498db', color: 'white', border: 'none', borderRadius: 4, cursor: 'pointer' }}
              >
                Comment
              </button>
            </div>
            
            {/* Comments list */}
            <div>
              {post.comments.length > 0 ? renderComments(post.comments) : (
                <p style={{ color: '#999', textAlign: 'center', padding: 20 }}>No comments yet. Be the first to comment!</p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function getTimeAgo(date: string): string {
  const seconds = Math.floor((Date.now() - new Date(date).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}
