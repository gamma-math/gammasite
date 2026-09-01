import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { Menu, UserCircle } from "lucide-react";
import { AdminContentPage } from "../pages/AdminContentPage.jsx";
import { AdminMembersPage } from "../pages/AdminMembersPage.jsx";
import { AdminMessagesPage } from "../pages/AdminMessagesPage.jsx";
import { AdminRegistrationsPage } from "../pages/AdminRegistrationsPage.jsx";
import { AdminRolesPage } from "../pages/AdminRolesPage.jsx";
import { AdminTemplatesPage } from "../pages/AdminTemplatesPage.jsx";
import { CalendarPage } from "../pages/CalendarPage.jsx";
import { ContentDetailPage } from "../pages/ContentDetailPage.jsx";
import { EventRegistrationsPage } from "../pages/EventRegistrationsPage.jsx";
import { FrontPage } from "../pages/FrontPage.jsx";
import { LibraryPage } from "../pages/LibraryPage.jsx";
import { MembersPage } from "../pages/MembersPage.jsx";
import { GenericPaymentPage, PaymentPage, PaymentStatusPage, ProductPaymentPage } from "../pages/PaymentPage.jsx";
import { StaticPage } from "../pages/StaticPage.jsx";
import { Link } from "../routes/navigation.jsx";
import { meApi } from "../services/api.js";
import "../styles/app.css";

function useRoute() {
  const [route, setRoute] = useState({ path: window.location.pathname, search: window.location.search });

  useEffect(() => {
    const update = () => setRoute({ path: window.location.pathname, search: window.location.search });
    window.addEventListener("popstate", update);
    window.addEventListener("gammasite:navigate", update);
    return () => {
      window.removeEventListener("popstate", update);
      window.removeEventListener("gammasite:navigate", update);
    };
  }, []);

  return route;
}

function useCurrentUser() {
  const [user, setUser] = useState({ isAuthenticated: false, roles: [] });

  useEffect(() => {
    let active = true;
    meApi.get().then((result) => {
      if (active) {
        setUser(result);
      }
    });
    return () => {
      active = false;
    };
  }, []);

  return user;
}

function App() {
  const route = useRoute();
  const user = useCurrentUser();
  const roles = new Set(user.roles ?? []);
  const isAdmin = roles.has("Admin") || roles.has("ADMIN");
  const isReadAdmin = isAdmin || roles.has("READ_ADMIN");

  return (
    <div className="app-shell">
      <Header user={user} isReadAdmin={isReadAdmin} />
      <main>{renderRoute(route, user, isAdmin, isReadAdmin)}</main>
      <Footer />
    </div>
  );
}

function Header({ user, isReadAdmin }) {
  return (
    <header className="site-header frontpage-header">
      <div className="page-shell">
        <div className="frontpage-topbar">
          <Link className="brand" href="/react">
            <img src="/lib/logo_blue.png" alt="GamMa logo" />
          </Link>

          <nav className="frontpage-topbar-actions" aria-label="Navigation">
            <div className="frontpage-left-actions">
              <Link className="frontpage-button frontpage-button-secondary" href="/react/events">
                <Menu size={16} />
                Menu
              </Link>
              {isReadAdmin && (
                <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/events">
                  Admin
                </Link>
              )}
            </div>
            <div className="frontpage-right-actions">
              {user.isAuthenticated ? (
                <a className="frontpage-profile-link frontpage-profile-link-default" href="/Identity/Account/Manage" aria-label="Profil">
                  <UserCircle size={23} />
                </a>
              ) : (
                <a className="frontpage-profile-link frontpage-profile-link-default" href="/Identity/Account/Login" aria-label="Login">
                  <UserCircle size={23} />
                </a>
              )}
            </div>
          </nav>
        </div>
      </div>
    </header>
  );
}

function renderRoute(route, user, isAdmin, isReadAdmin) {
  const path = route.path;

  if (path === "/" || path === "/Home" || path === "/Home/Index") {
    return <FrontPage />;
  }
  if (path === "/react/events" || path === "/react/arrangementer" || path === "/Home/Arrangementer") {
    return <FrontPage mode="EVENT" title="Begivenheder" />;
  }
  if (path === "/react/news") {
    return <FrontPage mode="NEWS" title="Nyheder" />;
  }
  if (path === "/react/members" || path === "/Users") {
    return <MembersPage user={user} />;
  }
  if (path === "/react/calendar" || path === "/Calendar") {
    return <CalendarPage user={user} />;
  }
  if (path === "/react/library" || path === "/Library") {
    return <LibraryPage user={user} search={route.search} />;
  }
  if (path === "/react/pay" || path === "/Pay" || path === "/Pay/Index") {
    return <PaymentPage user={user} />;
  }
  if (path === "/react/pay/generic" || path === "/Pay/Generisk") {
    return <GenericPaymentPage user={user} />;
  }
  if (path === "/react/pay/success" || path === "/Pay/Success") {
    return <PaymentStatusPage status="success" />;
  }
  if (path === "/react/pay/kontingent-success") {
    return <PaymentStatusPage status="kontingent-success" />;
  }
  if (path === "/react/pay/cancel" || path === "/Pay/Cancel") {
    return <PaymentStatusPage status="cancel" />;
  }
  const oldProductMatch = path.match(/^\/Pay\/Product\/?([^/]*)$/);
  if (oldProductMatch && oldProductMatch[1]) {
    return <ProductPaymentPage productId={decodeURIComponent(oldProductMatch[1])} user={user} />;
  }
  const productMatch = path.match(/^\/react\/pay\/products\/([^/]+)$/);
  if (productMatch) {
    return <ProductPaymentPage productId={decodeURIComponent(productMatch[1])} user={user} />;
  }
  if (path === "/react/om") {
    return <StaticPage page="about" />;
  }
  if (path === "/react/betingelser" || path === "/Home/Betingelser") {
    return <StaticPage page="terms" />;
  }
  if (path === "/react/cookies" || path === "/Home/Cookies") {
    return <StaticPage page="cookies" />;
  }
  const eventRegistrationsMatch = path.match(/^\/react\/events\/([^/]+)\/registrations$/);
  if (eventRegistrationsMatch) {
    return <EventRegistrationsPage slug={decodeURIComponent(eventRegistrationsMatch[1])} user={user} />;
  }
  if (path.startsWith("/react/events/")) {
    return <ContentDetailPage type="EVENT" slug={path.split("/").pop()} user={user} />;
  }
  if (path.startsWith("/react/news/")) {
    return <ContentDetailPage type="NEWS" slug={path.split("/").pop()} user={user} />;
  }
  if (path === "/react/admin/news") {
    return <AdminContentPage type="NEWS" isAdmin={isAdmin} />;
  }
  if (path === "/react/admin/templates") {
    return <AdminTemplatesPage isAdmin={isAdmin} />;
  }
  if (path === "/react/admin/users" || path === "/Users/Expanded" || path === "/Users/UpdateMass") {
    return <AdminMembersPage isAdmin={isAdmin} />;
  }
  if (path === "/react/admin/messages" || path === "/Messages") {
    return <AdminMessagesPage isAdmin={isAdmin} />;
  }
  if (path === "/react/admin/roles" || path === "/Role" || path === "/Role/Create" || path.startsWith("/Role/Update")) {
    return <AdminRolesPage isAdmin={isAdmin} />;
  }
  const registrationMatch = path.match(/^\/react\/admin\/events\/(\d+)\/registrations$/);
  if (registrationMatch) {
    return <AdminRegistrationsPage contentId={Number(registrationMatch[1])} isReadAdmin={isReadAdmin} />;
  }
  if (path === "/react/admin/events") {
    const editId = Number(new URLSearchParams(route.search).get("edit"));
    return <AdminContentPage type="EVENT" isAdmin={isAdmin} selectedId={Number.isFinite(editId) ? editId : null} />;
  }
  return <FrontPage />;
}

function Footer() {
  return (
    <section className="frontpage-footer-band">
      <div className="page-shell frontpage-footer-inner">
        <div className="frontpage-footer-col frontpage-footer-col-wide">
          <h2>Alumneforeningen GamMa</h2>
          <ul className="frontpage-contact-list">
            <li>Vedtægter: <Link href="/react/betingelser">Link til dem</Link></li>
            <li>CVR: 34637768</li>
            <li>Kontakt: <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a></li>
          </ul>
        </div>
        <div className="frontpage-footer-icons">
          <span className="frontpage-social-list">
            <a className="frontpage-social-link" href="https://www.linkedin.com/company/gamma-math-ucph/" aria-label="LinkedIn">in</a>
            <a className="frontpage-social-link" href="https://www.facebook.com/share/g/1Exv7epudc/" aria-label="Facebook">f</a>
            <a className="frontpage-social-link" href="https://www.instagram.com/gamma_ku_2100" aria-label="Instagram">ig</a>
          </span>
        </div>
      </div>
    </section>
  );
}

createRoot(document.getElementById("root")).render(<App />);
