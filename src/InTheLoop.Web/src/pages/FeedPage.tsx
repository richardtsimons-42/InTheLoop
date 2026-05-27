import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { postsApi } from '../services/api';
import PostCard from '../components/PostCard';
import NewPostForm from '../components/NewPostForm';
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

export default function FeedPage() {
  const { familyId } = useParams<{ familyId: string }>();
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [familyName, setFamilyName] = useState('');

  const fetchPosts = async () => {
    if (!familyId) return;
    try {
      const res = await postsApi.getFeed(parseInt(familyId));
      const postsWithComments = await Promise.all(
        res.data.map(async (post: any) => {
          const commentsRes = await postsApi.getComments(post.id);
          return { ...post, comments: commentsRes.data || [] };
        })
      );
      setPosts(postsWithComments);
      if (res.data.length > 0) setFamilyName(res.data[0].familyName || '');
    } catch (error) {
      console.error('Failed to fetch posts:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchPosts(); }, [familyId]);

  // Listen for comment refresh events from PostCard
  useEffect(() => {
    const handler = () => fetchPosts();
    window.addEventListener('refresh-feed', handler);
    return () => window.removeEventListener('refresh-feed', handler);
  }, []);

  if (loading) {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}>
        <span className="spinner" style={{ width: 32, height: 32 }} />
      </div>
    );
  }

  return (
    <div className="container" style={{ paddingTop: 'var(--space-2xl)', paddingBottom: 'var(--space-3xl)' }}>
      {/* Family Header */}
      <div style={{
        background: 'var(--color-brand-gradient)',
        borderRadius: 'var(--radius-lg)',
        padding: 'var(--space-xl) var(--space-2xl)',
        marginBottom: 'var(--space-2xl)',
        color: 'white',
      }}>
        <h1 style={{ fontSize: 24, fontWeight: 800, marginBottom: 'var(--space-xs)' }}>
          {familyName || 'Family Feed'}
        </h1>
        <p style={{ opacity: 0.85, fontSize: 15 }}>Share moments, stories, and photos with your family</p>
      </div>

      {/* New Post */}
      <NewPostForm familyId={parseInt(familyId || '0')} onPostCreated={fetchPosts} />

      {/* Posts */}
      {posts.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">📰</div>
          <div className="empty-state-title">No posts yet</div>
          <p>Be the first to share something with your family!</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-lg)' }}>
          {posts.map(post => (
            <PostCard key={post.id} post={post} />
          ))}
        </div>
      )}
    </div>
  );
}
