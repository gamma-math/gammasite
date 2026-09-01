import { useEffect, useState } from "react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { libraryApi } from "../services/api.js";
import { LoginRequired } from "./MembersPage.jsx";

export function LibraryPage({ user }) {
  const params = new URLSearchParams(window.location.search);
  const path = params.get("path") ?? "";
  const [listing, setListing] = useState(null);

  useEffect(() => {
    if (user.isAuthenticated) {
      libraryApi.listing(path).then(setListing);
    }
  }, [path, user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <LoginRequired title="Bibliotek" />;
  }

  return (
    <MenuLayout active="/react/library" isAuthenticated={user.isAuthenticated}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Bibliotek</p>
          <h1>{listing?.root ?? "/"}</h1>
        </div>
        {user.roles?.some((role) => role === "Admin" || role === "ADMIN") && <a className="menu-create-button" href="https://github.com/gamma-math/gammastatic">Redigér filer</a>}
      </div>
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead><tr><th>Indhold</th></tr></thead>
          <tbody>
            {(listing?.items ?? []).map((item) => (
              <tr key={item.path}>
                <td>
                  {item.type === "file" || item.type === "blob"
                    ? <a href={`/library?path=${encodeURIComponent(item.path)}`}>{item.icon} {item.name}</a>
                    : <Link href={`/react/library?path=${encodeURIComponent(item.path)}`}>{item.icon} {item.name}</Link>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {listing?.hasParent && <Link className="frontpage-button frontpage-button-secondary page-top-gap" href={`/react/library?path=${encodeURIComponent(listing.parent)}`}>Tilbage</Link>}
    </MenuLayout>
  );
}
