import { useState } from 'react';
import { familiesApi } from '../services/api';
import { useNavigate } from 'react-router-dom';

export default function CreateFamilyPage() {
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await familiesApi.createFamily(name, description || undefined);
    navigate('/families');
  };

  return (
    <div style={{ maxWidth: 400, margin: '100px auto', padding: 20 }}>
      <h1>Create a Family</h1>
      <form onSubmit={handleSubmit}>
        <input type="text" placeholder="Family Name" value={name} onChange={e => setName(e.target.value)} required />
        <textarea placeholder="Description (optional)" value={description} onChange={e => setDescription(e.target.value)} />
        <button type="submit">Create</button>
      </form>
    </div>
  );
}
