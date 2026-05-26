import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { postsApi } from '../services/api';
import PostCard from '../components/PostCard';
import NewPostForm from '../components/NewPostForm';

interface Post {
  id: number;
  content: string;
  authorName: string;
  authorAvatar: string | null;
  familyId: number;
  familyName: string;
  photoUrls: string[];
  createdAt: string;
}

export default function FeedPage() {
  const { familyId } = useParams<{ familyId: string }>();
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchPosts = async () => {
    if (!familyId) return;
    const res = await postsApi.getFeed(parseInt(familyId));
    setPosts(res.data);
    setLoading(false);
  };

  useEffect(() => { fetchPosts(); }, [familyId]);

  if (loading) return <div>Loading feed...</div>;

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
