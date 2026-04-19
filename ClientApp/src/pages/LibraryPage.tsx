import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { apiFetch } from '../api/client';

interface LibraryEntry {
  name: string;
  path: string;
  type: string;
  icon: string;
  isFile: boolean;
}

interface FolderResponse {
  isFile: false;
  root: string;
  parent: string | null;
  entries: LibraryEntry[];
}

interface FileResponse {
  isFile: true;
  downloadUrl: string;
}

type LibraryResponse = FolderResponse | FileResponse;

export default function LibraryPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const path = searchParams.get('path') ?? '';

  const [folder, setFolder] = useState<FolderResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);

    apiFetch<LibraryResponse>(`/api/library?path=${encodeURIComponent(path)}`)
      .then(data => {
        if (data.isFile) {
          // Should not normally be reached since files are opened directly from the
          // folder listing. Guard against direct URL access by redirecting to root.
          setSearchParams({});
          setLoading(false);
        } else {
          setFolder(data);
          setLoading(false);
        }
      })
      .catch(err => {
        setError((err as Error).message);
        setLoading(false);
      });
  }, [path]);

  const navigate = (entry: LibraryEntry) => {
    if (entry.isFile) {
      // Navigate directly to the view/download URL without setting ?path= in the SPA
      // first. This adds exactly one history entry, so Back returns to this folder.
      window.location.href = `/api/library/download?path=${encodeURIComponent(entry.path)}`;
    } else {
      setSearchParams(entry.path ? { path: entry.path } : {});
    }
  };

  const navigateToPath = (newPath: string) => {
    setSearchParams(newPath ? { path: newPath } : {});
  };

  if (loading) return <p>Henter bibliotek...</p>;
  if (error) return <p>Fejl: {error}</p>;
  if (!folder) return null;

  return (
    <>
      <h1>Bibliotek</h1>
      <table className="table table-striped table-bordered">
        <thead className="table-dark">
          <tr>
            <th>{folder.root}</th>
          </tr>
        </thead>
        <tbody>
          {folder.entries.map(entry => (
            <tr key={entry.path}>
              <td>
                <button
                  className="btn btn-link p-0 text-start"
                  onClick={() => navigate(entry)}
                >
                  {entry.icon} {entry.name}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {folder.parent !== null && (
        <button
          className="btn btn-secondary me-2"
          onClick={() => navigateToPath(folder.parent!)}
        >
          Tilbage
        </button>
      )}

      <a
        className="btn btn-primary"
        href="https://github.com/gamma-math/gammastatic"
        target="_blank"
        rel="noreferrer"
      >
        Redigér filer
      </a>
    </>
  );
}
