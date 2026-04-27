import { Link, NavLink } from 'react-router-dom';
import { useCurrentUser } from '../hooks/useCurrentUser';

export default function Navbar() {
  const { user, loading, roles } = useCurrentUser();
  const isAdmin = roles.includes('Admin');

  return (
    <nav className="navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3">
      <div className="container">
        <Link className="navbar-brand" to="/">
          GamMa
        </Link>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#spaNavbar"
          aria-controls="spaNavbar"
          aria-expanded="false"
          aria-label="Skift navigation"
        >
          <span className="navbar-toggler-icon" />
        </button>

        <div className="collapse navbar-collapse" id="spaNavbar">
          <ul className="navbar-nav me-auto">
            <li className="nav-item">
              <NavLink className="nav-link text-dark" to="/arrangementer">Arrangementer</NavLink>
            </li>
            <li className="nav-item">
              <a className="nav-link text-dark" href="https://www.facebook.com/groups/gammamath/" target="_blank" rel="noreferrer">
                Facebook
              </a>
            </li>

            {!loading && user && (
              <li className="nav-item dropdown">
                <a className="nav-link dropdown-toggle text-dark" href="#" role="button" data-bs-toggle="dropdown">
                  Menu
                </a>
                <ul className="dropdown-menu">
                  <li>
                    <NavLink className="dropdown-item" to="/users">Medlemmer</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/pay">Betal</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/library">Bibliotek</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/calendar">Kalender</NavLink>
                  </li>
                </ul>
              </li>
            )}

            {!loading && isAdmin && (
              <li className="nav-item dropdown">
                <a className="nav-link dropdown-toggle text-dark" href="#" role="button" data-bs-toggle="dropdown">
                  Admin
                </a>
                <ul className="dropdown-menu">
                  <li>
                    <NavLink className="dropdown-item" to="/messages">Beskeder</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/roles">Roller</NavLink>
                  </li>
                </ul>
              </li>
            )}
          </ul>

          <ul className="navbar-nav ms-auto">
            {!loading && (
              user
                ? <>
                    <li className="nav-item">
                      <span className="nav-link text-muted">{user.name}</span>
                    </li>
                    <li className="nav-item">
                      <a className="nav-link text-dark" href="/Identity/Account/Manage">Konto</a>
                    </li>
                    <li className="nav-item">
                      <button
                        className="nav-link text-dark btn btn-link"
                        onClick={() => {
                          fetch('/api/auth/logout', { method: 'POST' }).finally(() => {
                            window.location.href = '/';
                          });
                        }}
                      >
                        Log ud
                      </button>
                    </li>
                  </>
                : <li className="nav-item">
                    <a className="nav-link text-dark" href={`/Identity/Account/Login?returnUrl=${encodeURIComponent('/')}`}>
                      Log ind
                    </a>
                  </li>
            )}
          </ul>
        </div>
      </div>
    </nav>
  );
}
