import { useState } from 'react';
import { postsApi } from '../services/api';

interface NewPostFormProps {
  familyId: number;
  onPostCreated: () => void;
}

export default function NewPostForm({ familyId, onPostCreated }: NewPostFormProps) {
  const [content, setContent] = useState('');
  const [photo, setPhoto] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;
    setLoading(true);

    const res = await postsApi.createPost(familyId, content);
    const postId = res.data.id;

    if (photo) {
      await postsApi.uploadPhoto(postId, photo);
    }

    setContent('');
    setPhoto(null);
    setLoading(false);
    onPostCreated();
  };

  return (
    <form onSubmit={handleSubmit} style={{ border: '1px solid #eee', padding: 16, borderRadius: 8, marginBottom: 20 }}>
      <textarea
        placeholder="What's happening in your family?"
        value={content}
        onChange={e => setContent(e.target.value)}
        rows={3}
        style={{ width: '100%', marginBottom: 8, padding: 8 }}
      />
      <input type="file" accept="image/*" onChange={e => setPhoto(e.target.files?.[0] || null)} />
      <button type="submit" disabled={loading} style={{ marginTop: 8 }}>
        {loading ? 'Posting...' : 'Post'}
      </button>
    </form>
  );
}
