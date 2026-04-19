import { Link, NavLink } from 'react-router-dom';
import { useCurrentUser } from '../hooks/useCurrentUser';

export default function Navbar() {
  const { user, loading, roles } = useCurrentUser();
  const isAdmin = roles.includes('Admin');

  return (
    <nav className="navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3">
      <div className="container">
        <Link className="navbar-brand" to="/app/">
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
              <NavLink className="nav-link text-dark" to="/app/arrangementer">Arrangementer</NavLink>
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
                    <NavLink className="dropdown-item" to="/app/users">Medlemmer</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/app/pay">Betal</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/app/library">Bibliotek</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/app/calendar">Kalender</NavLink>
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
                    <NavLink className="dropdown-item" to="/app/messages">Beskeder</NavLink>
                  </li>
                  <li>
                    <NavLink className="dropdown-item" to="/app/roles">Roller</NavLink>
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
                      <a
                        className="nav-link text-dark"
                        href="/Identity/Account/Logout"
                        onClick={e => {
                          // POST to logout to satisfy antiforgery — let the MVC form handle it
                          e.preventDefault();
                          const form = document.createElement('form');
                          form.method = 'POST';
                          form.action = '/Identity/Account/Logout';
                          const input = document.createElement('input');
                          input.type = 'hidden';
                          input.name = 'returnUrl';
                          input.value = '/app/';
                          form.appendChild(input);
                          document.body.appendChild(form);
                          form.submit();
                        }}
                      >
                        Log ud
                      </a>
                    </li>
                  </>
                : <li className="nav-item">
                    <a className="nav-link text-dark" href={`/Identity/Account/Login?returnUrl=${encodeURIComponent('/app/')}`}>
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
