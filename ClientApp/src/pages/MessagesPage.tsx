import { Editor } from '@tinymce/tinymce-react';
import { useEffect, useRef, useState } from 'react';
import Select from 'react-select';
import { apiFetch } from '../api/client';

interface Recipients {
  statuses: string[];
  roles: string[];
}

type Media = 'Email' | 'SMS' | 'EmailSMS';

export default function MessagesPage() {
  const [recipients, setRecipients] = useState<Recipients | null>(null);
  const [selectedStatuses, setSelectedStatuses] = useState<Set<string>>(new Set());
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set());
  const [media, setMedia] = useState<Media>('Email');
  const [subject, setSubject] = useState('');
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const editorRef = useRef<any>(null);

  useEffect(() => {
    apiFetch<Recipients>('/api/messages/recipients').then(setRecipients);
  }, []);

  const handleSend = async () => {
    setError(null);
    setResult(null);

    if (!subject.trim()) { setError('Emne er obligatorisk'); return; }
    if (selectedStatuses.size === 0 && selectedRoles.size === 0) { setError('Vælg mindst én modtagergruppe'); return; }

    const messageBody = editorRef.current?.getContent() ?? '';
    const plainText = editorRef.current?.getContent({ format: 'text' }) ?? '';
    setSending(true);
    try {
      const data = await apiFetch<{ sent: number }>('/api/messages/send', {
        method: 'POST',
        body: JSON.stringify({
          statuses: [...selectedStatuses],
          roles: [...selectedRoles],
          media,
          subject,
          messageBody: media !== 'SMS' ? messageBody : null,
          smsBody: media !== 'Email' ? plainText : null,
        }),
      });
      setResult(`Besked sendt til ${data.sent} modtagere`);
      setSubject('');
      setSelectedStatuses(new Set());
      setSelectedRoles(new Set());
      editorRef.current?.setContent('');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSending(false);
    }
  };

  if (!recipients) return <p>Henter modtagerlister...</p>;

  return (
    <div style={{ maxWidth: 900 }}>
      <h1>Send besked</h1>

      {/* Recipient selectors */}
      <div className="row g-3 mb-3">
        <div className="col">
          <label className="form-label fw-semibold">Status</label>
          <Select
            isMulti
            options={recipients.statuses.map(s => ({ value: s, label: s }))}
            value={[...selectedStatuses].map(s => ({ value: s, label: s }))}
            onChange={opts => setSelectedStatuses(new Set(opts.map(o => o.value)))}
            placeholder="Vælg statusser..."
            noOptionsMessage={() => 'Ingen statusser'}
            classNamePrefix="rs"
            menuPortalTarget={document.body}
            menuPosition="fixed"
            styles={{ menuPortal: base => ({ ...base, zIndex: 9999 }) }}
          />
        </div>
        <div className="col">
          <label className="form-label fw-semibold">Rolle</label>
          <Select
            isMulti
            options={recipients.roles.map(r => ({ value: r ?? '', label: r ?? '' }))}
            value={[...selectedRoles].map(r => ({ value: r, label: r }))}
            onChange={opts => setSelectedRoles(new Set(opts.map(o => o.value)))}
            placeholder="Vælg roller..."
            noOptionsMessage={() => 'Ingen roller'}
            classNamePrefix="rs"
            menuPortalTarget={document.body}
            menuPosition="fixed"
            styles={{ menuPortal: base => ({ ...base, zIndex: 9999 }) }}
          />
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

      {/* TinyMCE rich-text editor — formatting stripped automatically for SMS */}
      <div className="mb-3">
          <label className="form-label fw-semibold">
            Besked{media !== 'Email' && <span className="text-muted fw-normal ms-2" style={{ fontSize: '0.875em' }}>(formatering fjernes automatisk til SMS)</span>}
          </label>
          <Editor
            licenseKey="gpl"
            tinymceScriptSrc="/spa/libs/tinymce/tinymce.min.js"
            onInit={(_evt, editor) => { editorRef.current = editor; }}
            init={{
              height: 400,
              menubar: true,
              language: 'da',
              language_url: '/spa/libs/tinymce/langs/da.js',
              plugins: [
                'advlist', 'autolink', 'lists', 'link', 'image', 'charmap',
                'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
                'insertdatetime', 'media', 'table', 'preview', 'help', 'wordcount',
                'emoticons',
              ],
              toolbar:
                'undo redo | styles | fontfamily fontsize | ' +
                'bold italic underline strikethrough | forecolor backcolor | ' +
                'alignleft aligncenter alignright alignjustify | ' +
                'bullist numlist outdent indent | ' +
                'link image media table | ' +
                'charmap emoticons | code fullscreen preview | help',
              toolbar_mode: 'wrap',
              content_style:
                'body { font-family: Arial, sans-serif; font-size: 14px; }',
              // Allow paste from Word/Outlook with formatting intact
              paste_data_images: true,
              // Images: embed as base64 so no upload server is needed
              images_upload_handler: (blobInfo) =>
                Promise.resolve(`data:${blobInfo.blob().type};base64,${blobInfo.base64()}`),
              promotion: false,
            }}
          />
        </div>

      {error && <div className="alert alert-danger">{error}</div>}
      {result && <div className="alert alert-success">{result}</div>}

      <button className="btn btn-primary" onClick={handleSend} disabled={sending}>
        {sending ? 'Sender...' : 'Send besked'}
      </button>
    </div>
  );
}
