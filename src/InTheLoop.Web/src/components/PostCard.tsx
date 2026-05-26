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

export default function PostCard({ post }: { post: Post }) {
  const timeAgo = getTimeAgo(post.createdAt);

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
