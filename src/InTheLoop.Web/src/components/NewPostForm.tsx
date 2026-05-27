import { useState } from 'react';
import { postsApi } from '../services/api';
import { useAuth } from '../context/AuthContext';
import '../index.css';

interface NewPostFormProps {
  familyId: number;
  onPostCreated: () => void;
}

export default function NewPostForm({ familyId, onPostCreated }: NewPostFormProps) {
  const { user } = useAuth();
  const [content, setContent] = useState('');
  const [photo, setPhoto] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setPhoto(file);
      const reader = new FileReader();
      reader.onloadend = () => setPhotoPreview(reader.result as string);
      reader.readAsDataURL(file);
    }
  };

  const removePhoto = () => {
    setPhoto(null);
    setPhotoPreview(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() && !photo) return;
    setLoading(true);

    try {
      const res = await postsApi.createPost(familyId, content);
      const postId = res.data.id;

      if (photo) {
        await postsApi.uploadPhoto(postId, photo);
      }

      setContent('');
      setPhoto(null);
      setPhotoPreview(null);
      onPostCreated();
    } catch (err) {
      console.error('Failed to create post:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="card" style={{ marginBottom: 'var(--space-xl)', padding: 'var(--space-lg) var(--space-xl)' }}>
      <form onSubmit={handleSubmit}>
        {/* Author */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-md)', marginBottom: 'var(--space-md)' }}>
          <div className="avatar avatar-sm" style={{ background: 'var(--color-brand-gradient)' }}>
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </div>
          <span style={{ fontWeight: 600, fontSize: 14 }}>
            {user?.firstName} {user?.lastName}
          </span>
        </div>

        {/* Textarea */}
        <textarea
          className="input"
          placeholder="What's happening in your family?"
          value={content}
          onChange={e => setContent(e.target.value)}
          rows={3}
          style={{ marginBottom: 'var(--space-md)', resize: 'vertical' }}
        />

        {/* Photo Preview */}
        {photoPreview && (
          <div style={{
            position: 'relative',
            marginBottom: 'var(--space-md)',
            borderRadius: 'var(--radius-md)',
            overflow: 'hidden',
          }}>
            <img
              src={photoPreview}
              alt="Preview"
              style={{ width: '100%', maxHeight: 240, objectFit: 'cover', borderRadius: 'var(--radius-md)' }}
            />
            <button
              type="button"
              onClick={removePhoto}
              style={{
                position: 'absolute',
                top: 'var(--space-sm)',
                right: 'var(--space-sm)',
                width: 28,
                height: 28,
                borderRadius: 'var(--radius-full)',
                background: 'rgba(0,0,0,0.6)',
                color: 'white',
                border: 'none',
                cursor: 'pointer',
                fontSize: 14,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              ✕
            </button>
          </div>
        )}

        {/* Photo Upload */}
        {!photoPreview && (
          <label style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 'var(--space-sm)',
            padding: 'var(--space-sm) var(--space-md)',
            fontSize: 14,
            color: 'var(--color-text-secondary)',
            cursor: 'pointer',
            borderRadius: 'var(--radius-md)',
            border: '1px dashed var(--color-border)',
            marginBottom: 'var(--space-md)',
            transition: 'all var(--transition-fast)',
          }}
            onMouseEnter={e => {
              e.currentTarget.style.borderColor = 'var(--color-brand)';
              e.currentTarget.style.color = 'var(--color-brand)';
            }}
            onMouseLeave={e => {
              e.currentTarget.style.borderColor = 'var(--color-border)';
              e.currentTarget.style.color = 'var(--color-text-secondary)';
            }}
          >
            <span style={{ fontSize: 18 }}>📷</span> Add Photo
            <input
              type="file"
              accept="image/*"
              onChange={handlePhotoChange}
              style={{ display: 'none' }}
            />
          </label>
        )}

        {/* Submit */}
        <button
          type="submit"
          className="btn btn-primary"
          disabled={loading || (!content.trim() && !photo)}
          style={{ marginTop: 'var(--space-sm)' }}
        >
          {loading ? <span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> : 'Post'}
        </button>
      </form>
    </div>
  );
}
