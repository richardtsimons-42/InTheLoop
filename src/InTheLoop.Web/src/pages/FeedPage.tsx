import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { postsApi } from '../services/api';
import PostCard from '../components/PostCard';
import NewPostForm from '../components/NewPostForm';

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
  const [commentTexts, setCommentTexts] = useState<Record<number, string>>({});
  const [replyingTo, setReplyingTo] = useState<number | null>(null);

  const fetchPosts = async () => {
    if (!familyId) return;
    const res = await postsApi.getFeed(parseInt(familyId));
    // Fetch comments for each post
    const postsWithComments = await Promise.all(
      res.data.map(async (post: any) => {
        const commentsRes = await postsApi.getComments(post.id);
        return { ...post, comments: commentsRes.data || [] };
      })
    );
    setPosts(postsWithComments);
    setLoading(false);
  };

  useEffect(() => { fetchPosts(); }, [familyId]);

  const handleComment = async (postId: number) => {
    const text = commentTexts[postId] || '';
    if (!text.trim()) return;

    await postsApi.addComment(postId, text);
    setCommentTexts(prev => ({ ...prev, [postId]: '' }));
    setReplyingTo(null);
    await fetchPosts();
  };

  const handleReply = async (commentId: number, postId: number) => {
    const text = commentTexts[`reply-${commentId}` as any] || '';
    if (!text.trim()) return;

    await postsApi.addReply(commentId, postId, text);
    setReplyingTo(null);
    await fetchPosts();
  };

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${diffDays}d ago`;
  };

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}>Loading feed...</div>;

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 20 }}>
      <h1>Family Feed</h1>
      <NewPostForm familyId={parseInt(familyId || '0')} onPostCreated={fetchPosts} />
      {posts.map(post => (
        <PostCard key={post.id} post={post} />
      ))}
    </div>
  );
}
