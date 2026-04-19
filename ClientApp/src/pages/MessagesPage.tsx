import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';

interface Recipients {
  statuses: string[];
  roles: string[];
}

type Media = 'Email' | 'SMS' | 'EmailSMS';

function Toolbar({ editor }: { editor: ReturnType<typeof useEditor> }) {
  if (!editor) return null;
  return (
    <div className="btn-toolbar mb-1 gap-1" role="toolbar">
      <button
        type="button"
        className={`btn btn-sm btn-outline-secondary${editor.isActive('bold') ? ' active' : ''}`}
        onClick={() => editor.chain().focus().toggleBold().run()}
      ><strong>B</strong></button>
      <button
        type="button"
        className={`btn btn-sm btn-outline-secondary${editor.isActive('italic') ? ' active' : ''}`}
        onClick={() => editor.chain().focus().toggleItalic().run()}
      ><em>I</em></button>
      <button
        type="button"
        className={`btn btn-sm btn-outline-secondary${editor.isActive('heading', { level: 2 }) ? ' active' : ''}`}
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
      >H2</button>
      <button
        type="button"
        className={`btn btn-sm btn-outline-secondary${editor.isActive('bulletList') ? ' active' : ''}`}
        onClick={() => editor.chain().focus().toggleBulletList().run()}
      >• Liste</button>
      <button
        type="button"
        className={`btn btn-sm btn-outline-secondary${editor.isActive('orderedList') ? ' active' : ''}`}
        onClick={() => editor.chain().focus().toggleOrderedList().run()}
      >1. Liste</button>
    </div>
  );
}

export default function MessagesPage() {
  const [recipients, setRecipients] = useState<Recipients | null>(null);
  const [selectedStatuses, setSelectedStatuses] = useState<Set<string>>(new Set());
  const [selectedRole, setSelectedRole] = useState('');
  const [media, setMedia] = useState<Media>('Email');
  const [subject, setSubject] = useState('');
  const [smsBody, setSmsBody] = useState('');
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const editor = useEditor({
    extensions: [StarterKit],
    content: '',
  });

  useEffect(() => {
    apiFetch<Recipients>('/api/messages/recipients').then(setRecipients);
  }, []);

  const toggleStatus = (s: string) => {
    setSelectedStatuses(prev => {
      const next = new Set(prev);
      if (next.has(s)) next.delete(s); else next.add(s);
      return next;
    });
  };

  const handleSend = async () => {
    setError(null);
    setResult(null);

    if (!subject.trim()) { setError('Emne er obligatorisk'); return; }
    if (selectedStatuses.size === 0 && !selectedRole) { setError('Vælg mindst én modtagergruppe'); return; }

    const messageBody = editor?.getHTML() ?? '';
    const derivedSmsBody = media === 'Email' ? '' : (media === 'SMS' ? smsBody : smsBody);

    setSending(true);
    try {
      const data = await apiFetch<{ sent: number }>('/api/messages/send', {
        method: 'POST',
        body: JSON.stringify({
          statuses: [...selectedStatuses],
          role: selectedRole || null,
          media,
          subject,
          messageBody: media !== 'SMS' ? messageBody : null,
          smsBody: media !== 'Email' ? derivedSmsBody : null,
        }),
      });
      setResult(`Besked sendt til ${data.sent} modtagere`);
      // Reset form
      setSubject('');
      setSmsBody('');
      setSelectedStatuses(new Set());
      setSelectedRole('');
      editor?.commands.clearContent();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSending(false);
    }
  };

  if (!recipients) return <p>Henter modtagerlister...</p>;

  const showEmailEditor = media === 'Email' || media === 'EmailSMS';
  const showSmsField = media === 'SMS' || media === 'EmailSMS';

  return (
    <div style={{ maxWidth: 800 }}>
      <h1>Send besked</h1>

      {/* Recipient selectors */}
      <div className="row g-3 mb-3">
        <div className="col">
          <label className="form-label fw-semibold">Status</label>
          <div className="border rounded p-2">
            {recipients.statuses.map(s => (
              <div className="form-check" key={s}>
                <input
                  className="form-check-input"
                  type="checkbox"
                  id={`status-${s}`}
                  checked={selectedStatuses.has(s)}
                  onChange={() => toggleStatus(s)}
                />
                <label className="form-check-label" htmlFor={`status-${s}`}>{s}</label>
              </div>
            ))}
          </div>
        </div>
        <div className="col">
          <label className="form-label fw-semibold">Rolle</label>
          <select
            className="form-select"
            value={selectedRole}
            onChange={e => setSelectedRole(e.target.value)}
          >
            <option value="">— ingen —</option>
            {recipients.roles.map(r => (
              <option key={r} value={r ?? ''}>{r}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Media selector */}
      <div className="mb-3">
        {(['Email', 'SMS', 'EmailSMS'] as Media[]).map(m => (
          <div className="form-check form-check-inline" key={m}>
            <input
              className="form-check-input"
              type="radio"
              id={`media-${m}`}
              checked={media === m}
              onChange={() => setMedia(m)}
            />
            <label className="form-check-label" htmlFor={`media-${m}`}>
              {m === 'EmailSMS' ? 'Email & SMS' : m}
            </label>
          </div>
        ))}
      </div>

      {/* Subject */}
      <div className="mb-3">
        <input
          type="text"
          className="form-control"
          placeholder="Emne..."
          value={subject}
          onChange={e => setSubject(e.target.value)}
        />
      </div>

      {/* Rich text email body */}
      {showEmailEditor && (
        <div className="mb-3">
          <label className="form-label fw-semibold">Besked (e-mail)</label>
          <Toolbar editor={editor} />
          <div className="border rounded p-2" style={{ minHeight: 200 }}>
            <EditorContent editor={editor} />
          </div>
        </div>
      )}

      {/* Plain text SMS body */}
      {showSmsField && (
        <div className="mb-3">
          <label className="form-label fw-semibold">SMS-tekst</label>
          <textarea
            className="form-control"
            rows={3}
            placeholder="Kort SMS-besked..."
            value={smsBody}
            onChange={e => setSmsBody(e.target.value)}
          />
        </div>
      )}

      {error && <div className="alert alert-danger">{error}</div>}
      {result && <div className="alert alert-success">{result}</div>}

      <button className="btn btn-primary" onClick={handleSend} disabled={sending}>
        {sending ? 'Sender...' : 'Send besked'}
      </button>
    </div>
  );
}
