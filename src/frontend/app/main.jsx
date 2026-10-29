import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { Instagram, Menu, UserCircle } from "lucide-react";
import {
  AccountManagePage,
  AccountEventsPage,
  ForgotPasswordPage,
  LoginPage,
  RegisterPage,
  ResendEmailConfirmationPage,
} from "../pages/AccountPages.jsx";
import {
  AdminContentEditorPage,
  AdminContentPage,
} from "../pages/AdminContentPage.jsx";
import {
  FinanceAdminBudgetDetailPage,
  FinanceAdminBudgetsPage,
  FinanceAdminAccountsPage,
  FinanceAdminCashierPostingsPage,
  FinanceAdminImportPage,
  FinanceAdminOverviewPage,
  FinanceAdminPostingGroupDetailPage,
  FinanceAdminPostingGroupsPage,
  FinanceAdminPostingDetailPage,
  FinanceAdminPostingsPage,
} from "../pages/AdminFinancePage.jsx";
import { AdminMemberSubscriptionsPage, AdminMembersPage } from "../pages/AdminMembersPage.jsx";
import { AdminMessagesPage } from "../pages/AdminMessagesPage.jsx";
import {
  AdminRolesEditorPage,
  AdminRolesPage,
} from "../pages/AdminRolesPage.jsx";
import {
  AdminTemplateEditorPage,
  AdminTemplatesPage,
} from "../pages/AdminTemplatesPage.jsx";
import { AdminProductsPage } from "../pages/AdminProductsPage.jsx";
import { AdminProductEditorPage } from "../pages/AdminProductsPage.jsx";
import { AdminSubscriptionsPage } from "../pages/AdminSubscriptionsPage.jsx";
import { AdminSubscriptionDetailPage } from "../pages/AdminSubscriptionDetailPage.jsx";
import { CalendarPage } from "../pages/CalendarPage.jsx";
import { ContentDetailPage } from "../pages/ContentDetailPage.jsx";
import { EventRegistrationsPage } from "../pages/EventRegistrationsPage.jsx";
import { FinancePage } from "../pages/FinancePage.jsx";
import { FrontPage } from "../pages/FrontPage.jsx";
import { LibraryPage } from "../pages/LibraryPage.jsx";
import { MembersPage } from "../pages/MembersPage.jsx";
import {
  EventPaymentPage,
  PaymentPage,
  PaymentStatusPage,
} from "../pages/PaymentPage.jsx";
import { StaticPage } from "../pages/StaticPage.jsx";
import { Link } from "../routes/navigation.jsx";
import { setMobileMenuCollapsed } from "../utils/mobileMenu.js";
import { meApi } from "../services/api.js";
import { AccessContext, adminPermissionForPath, canAccessAdmin, firstAdminPath, hasAnyPermission, hasPermission, permissions } from "../utils/access.js";
import "../styles/app.css";

/**
 * Tracks the current browser path for the lightweight client-side router.
 */
function useRoute() {
  const [route, setRoute] = useState({
    path: window.location.pathname,
    search: window.location.search,
  });

  useEffect(() => {
    const update = () =>
      setRoute({
        path: window.location.pathname,
        search: window.location.search,
      });
    window.addEventListener("popstate", update);
    window.addEventListener("gammasite:navigate", update);
    return () => {
      window.removeEventListener("popstate", update);
      window.removeEventListener("gammasite:navigate", update);
    };
  }, []);

  return route;
}

/**
 * Loads the signed-in user once so navigation and page access can react to roles.
 */
function useCurrentUser() {
  const [user, setUser] = useState({
    isLoading: true,
    isAuthenticated: false,
    roles: [],
  });

  useEffect(() => {
    let active = true;
    meApi
      .get()
      .then((result) => {
        if (active) {
          setUser({ ...result, isLoading: false });
        }
      })
      .catch(() => {
        if (active) {
          setUser({ isLoading: false, isAuthenticated: false, roles: [] });
        }
      });
    return () => {
      active = false;
    };
  }, []);

  return user;
}

/**
 * Root React application that composes the shared chrome and current route page.
 */
function App() {
  const route = useRoute();
  const user = useCurrentUser();
  const isAdmin = canAccessAdmin(user);
  const isReadAdmin = isAdmin;
  const adminPath = firstAdminPath(user);
  const access = {
    contentEdit: hasPermission(user, permissions.contentEdit),
    registrationsEdit: hasPermission(user, permissions.registrationsEdit),
    emailTemplatesEdit: hasPermission(user, permissions.emailTemplatesEdit),
    messagesEdit: hasPermission(user, permissions.messagesEdit),
    rolesEdit: hasPermission(user, permissions.rolesEdit),
    financeView: hasAnyPermission(user, [permissions.financeViewAll, permissions.financeEditAll]),
    financeEdit: hasPermission(user, permissions.financeEditAll),
    paymentsEditAll: hasPermission(user, permissions.paymentsEditAll)
  };

  return (
    <AccessContext.Provider value={user}>
      <div className="app-shell">
        <Header user={user} isReadAdmin={isReadAdmin} adminPath={adminPath} />
        <main>{renderRoute(route, user, isAdmin, access)}</main>
        <Footer />
      </div>
    </AccessContext.Provider>
  );
}

/**
 * Shared site header used across public, account, and admin React pages.
 */
function Header({ user, isReadAdmin, adminPath }) {
  return (
    <header className="site-header frontpage-header">
      <div className="page-shell">
        <div className="frontpage-topbar">
          <Link className="brand" href="/">
            <img src="/lib/logo_blue.png" alt="GamMa logo" />
          </Link>

          <nav className="frontpage-topbar-actions" aria-label="Navigation">
            <div className="frontpage-left-actions">
              <Link
                className="frontpage-button frontpage-button-secondary"
                href="/events"
                onClick={() => setMobileMenuCollapsed(false)}
              >
                <Menu size={16} />
                Menu
              </Link>
              {isReadAdmin && (
                <Link
                  className="frontpage-button frontpage-button-secondary"
                  href={adminPath}
                  onClick={() => setMobileMenuCollapsed(false)}
                >
                  Admin
                </Link>
              )}
            </div>
            <div className="frontpage-right-actions">
              {user.isLoading ? (
                <span
                  className="frontpage-auth-placeholder"
                  aria-label="Henter brugerstatus"
                />
              ) : user.isAuthenticated ? (
                <Link
                  className="frontpage-profile-link frontpage-profile-link-default"
                  href="/account/manage"
                  aria-label="Profil"
                  onClick={() => setMobileMenuCollapsed(false)}
                >
                  <UserCircle size={23} />
                </Link>
              ) : (
                <>
                  <Link
                    className="frontpage-button frontpage-button-secondary"
                    href="/account/login"
                  >
                    Login
                  </Link>
                  <Link
                    className="frontpage-button frontpage-button-primary"
                    href="/account/register"
                  >
                    Bliv medlem
                  </Link>
                </>
              )}
            </div>
          </nav>
        </div>
      </div>
    </header>
  );
}

/**
 * Maps legacy and React URLs to their active React page components.
 */
function renderRoute(route, user, isAdmin, access) {
  const path = route.path;

  if (path === "/admin" || path === "/admin/") {
    return <AdminRedirectPage path={firstAdminPath(user)} />;
  }

  const requiredAdminPermission = adminPermissionForPath(path);
  if (requiredAdminPermission && !hasPermission(user, requiredAdminPermission)) {
    return <AdminRedirectPage path={firstAdminPath(user)} />;
  }

  if (path === "/" || path === "/Home" || path === "/Home/Index") {
    return <FrontPage user={user} />;
  }
  if (
    path === "/events" ||
    path === "/arrangementer" ||
    path === "/Home/Arrangementer"
  ) {
    return <FrontPage mode="EVENT" title="Begivenheder" user={user} />;
  }
  if (path === "/news") {
    return <FrontPage mode="NEWS" title="Nyheder" user={user} />;
  }
  if (path === "/members" || path === "/Users") {
    return <MembersPage user={user} />;
  }
  if (path === "/calendar" || path === "/Calendar") {
    return <CalendarPage user={user} />;
  }
  if (path === "/Library") {
    return <LibraryPage user={user} search={route.search} />;
  }
  if (path === "/pay" || path === "/Pay" || path === "/Pay/Index") {
    return <PaymentPage user={user} />;
  }
  if (path === "/finance") {
    return <FinancePage user={user} />;
  }
  if (path === "/pay/success" || path === "/Pay/Success") {
    const query = new URLSearchParams(route.search);
    return <PaymentStatusPage status="success" session={query.get("session")} reference={query.get("reference")} user={user} />;
  }
  if (path === "/pay/kontingent-success") {
    return <PaymentStatusPage status="kontingent-success" user={user} />;
  }
  if (path === "/pay/cancel" || path === "/Pay/Cancel") {
    return <PaymentStatusPage status="cancel" user={user} />;
  }
  const eventPaymentSuccessMatch = path.match(/^\/events\/([^/]+)\/pay\/success$/);
  if (eventPaymentSuccessMatch) {
    return <PaymentStatusPage status="event-success" eventSlug={decodeURIComponent(eventPaymentSuccessMatch[1])} reference={new URLSearchParams(route.search).get("reference")} user={user} />;
  }
  const eventPaymentMatch = path.match(/^\/events\/([^/]+)\/pay\/(\d+)$/);
  if (eventPaymentMatch) {
    return <EventPaymentPage eventSlug={decodeURIComponent(eventPaymentMatch[1])} offerId={Number(eventPaymentMatch[2])} user={user} />;
  }
  if (path === "/om") {
    return <StaticPage page="about" />;
  }
  if (path === "/betingelser" || path === "/Home/Betingelser") {
    return <StaticPage page="terms" />;
  }
  if (path === "/cookies" || path === "/Home/Cookies") {
    return <StaticPage page="cookies" />;
  }
  if (path === "/account/login") {
    return <LoginPage />;
  }
  if (path === "/account/register") {
    return <RegisterPage />;
  }
  if (path === "/account/forgot-password") {
    return <ForgotPasswordPage />;
  }
  if (path === "/account/resend-email-confirmation") {
    return <ResendEmailConfirmationPage />;
  }
  if (path === "/account/manage") {
    return <AccountManagePage user={user} section="profile" />;
  }
  if (path === "/account/events") {
    return <AccountEventsPage user={user} />;
  }
  if (path === "/account/manage/email") {
    return <AccountManagePage user={user} section="email" />;
  }
  if (path === "/account/manage/password") {
    return <AccountManagePage user={user} section="password" />;
  }
  if (path === "/account/manage/two-factor") {
    return <AccountManagePage user={user} section="two-factor" />;
  }
  if (path === "/account/manage/subscriptions") {
    return <AccountManagePage user={user} section="subscriptions" />;
  }
  if (path === "/account/manage/finance") {
    return <AccountManagePage user={user} section="finance" />;
  }
  if (path === "/account/manage/personal-data") {
    return <AccountManagePage user={user} section="personal-data" />;
  }
  if (path === "/account/manage/delete-personal-data") {
    return <AccountManagePage user={user} section="delete-personal-data" />;
  }
  if (path === "/account/manage/logout") {
    return <AccountManagePage user={user} section="logout" />;
  }
  const eventRegistrationsMatch = path.match(
    /^\/events\/([^/]+)\/registrations$/,
  );
  if (eventRegistrationsMatch) {
    return (
      <EventRegistrationsPage
        slug={decodeURIComponent(eventRegistrationsMatch[1])}
        user={user}
      />
    );
  }
  if (path.startsWith("/events/")) {
    return (
      <ContentDetailPage
        type="EVENT"
        slug={path.split("/").pop()}
        user={user}
      />
    );
  }
  if (path.startsWith("/news/")) {
    return (
      <ContentDetailPage type="NEWS" slug={path.split("/").pop()} user={user} />
    );
  }
  if (path === "/admin/news") {
    return <AdminContentPage type="NEWS" isAdmin={access.contentEdit} />;
  }
  const adminProductEditorMatch = path.match(
    /^\/admin\/products\/(\d+)\/edit$/,
  );
  if (adminProductEditorMatch) {
    return (
      <AdminProductEditorPage
        isAdmin={access.paymentsEditAll}
        offerId={Number(adminProductEditorMatch[1])}
      />
    );
  }
  if (path === "/admin/products") {
    return <AdminProductsPage isAdmin={access.paymentsEditAll} />;
  }
  if (path === "/admin/subscriptions") {
    return <AdminSubscriptionsPage isAdmin={access.paymentsEditAll} />;
  }
  const adminSubscriptionDetailMatch = path.match(/^\/admin\/subscription\/(\d+)$/);
  if (adminSubscriptionDetailMatch) {
    return (
      <AdminSubscriptionDetailPage
        isAdmin={access.paymentsEditAll}
        subscriptionId={Number(adminSubscriptionDetailMatch[1])}
      />
    );
  }
  if (path === "/admin/finance") {
    return <FinanceAdminOverviewPage isAdmin={access.financeView} search={route.search} />;
  }
  if (path === "/admin/finance/postings") {
    return <FinanceAdminPostingsPage isAdmin={access.financeView} canWrite={access.financeEdit} search={route.search} />;
  }
  if (path === "/admin/finance/postings/edit") {
    return (
      <FinanceAdminCashierPostingsPage
        isAdmin={access.financeEdit}
        search={route.search}
      />
    );
  }
  if (path === "/admin/finance/csv-import") {
    return <FinanceAdminImportPage isAdmin={access.financeEdit} />;
  }
  if (path === "/admin/finance/budgets") {
    return <FinanceAdminBudgetsPage isAdmin={access.financeEdit} />;
  }
  if (path === "/admin/finance/chart-of-accounts") {
    return <FinanceAdminAccountsPage isAdmin={access.financeEdit} />;
  }
  if (path === "/admin/finance/posting-groups") {
    return <FinanceAdminPostingGroupsPage isAdmin={access.financeEdit} />;
  }
  const adminFinancePostingGroupMatch = path.match(
    /^\/admin\/finance\/posting-groups\/([^/]+)$/,
  );
  if (adminFinancePostingGroupMatch) {
    return (
      <FinanceAdminPostingGroupDetailPage
        isAdmin={access.financeEdit}
        id={decodeURIComponent(adminFinancePostingGroupMatch[1])}
      />
    );
  }
  const adminFinanceBudgetMatch = path.match(
    /^\/admin\/finance\/budgets\/([^/]+)$/,
  );
  if (adminFinanceBudgetMatch) {
    return (
      <FinanceAdminBudgetDetailPage
        isAdmin={access.financeEdit}
        id={decodeURIComponent(adminFinanceBudgetMatch[1])}
      />
    );
  }
  const adminFinancePostingMatch = path.match(
    /^\/admin\/finance\/postings\/([^/]+)$/,
  );
  if (adminFinancePostingMatch) {
    return (
      <FinanceAdminPostingDetailPage
        isAdmin={access.financeView}
        canWrite={access.financeEdit}
        id={decodeURIComponent(adminFinancePostingMatch[1])}
      />
    );
  }
  const adminNewsEditorMatch = path.match(
    /^\/admin\/news\/(new|\d+\/edit)$/,
  );
  if (adminNewsEditorMatch) {
    const itemId =
      adminNewsEditorMatch[1] === "new"
        ? null
        : Number(adminNewsEditorMatch[1].split("/")[0]);
    return (
      <AdminContentEditorPage type="NEWS" isAdmin={access.contentEdit} itemId={itemId} />
    );
  }
  if (path === "/admin/templates") {
    return <AdminTemplatesPage isAdmin={access.emailTemplatesEdit} />;
  }
  const adminTemplateEditorMatch = path.match(
    /^\/admin\/templates\/(new|\d+\/edit)$/,
  );
  if (adminTemplateEditorMatch) {
    const templateId =
      adminTemplateEditorMatch[1] === "new"
        ? null
        : Number(adminTemplateEditorMatch[1].split("/")[0]);
    return (
      <AdminTemplateEditorPage isAdmin={access.emailTemplatesEdit} templateId={templateId} />
    );
  }
  if (
    /^\/admin\/users\/[^/]+\/subscriptions$/.test(path)
  ) {
    const memberId = decodeURIComponent(path.split("/")[4]);
    return <AdminMemberSubscriptionsPage isAdmin={access.rolesEdit} memberId={memberId} />;
  }
  if (
    path === "/admin/users" ||
    path === "/Users/Expanded" ||
    path === "/Users/UpdateMass"
  ) {
    return <AdminMembersPage isAdmin={access.rolesEdit} />;
  }
  if (path === "/admin/messages" || path === "/Messages") {
    return <AdminMessagesPage isAdmin={access.messagesEdit} />;
  }
  if (path === "/admin/roles/new" || path === "/Role/Create") {
    return <AdminRolesEditorPage isAdmin={access.rolesEdit} roleId={null} />;
  }
  const adminRoleEditorMatch = path.match(
    /^\/admin\/roles\/([^/]+)\/edit$/,
  );
  if (adminRoleEditorMatch) {
    return (
      <AdminRolesEditorPage
        isAdmin={access.rolesEdit}
        roleId={decodeURIComponent(adminRoleEditorMatch[1])}
      />
    );
  }
  const oldRoleUpdateMatch = path.match(/^\/Role\/Update\/?([^/]*)$/);
  if (oldRoleUpdateMatch && oldRoleUpdateMatch[1]) {
    return (
      <AdminRolesEditorPage
        isAdmin={access.rolesEdit}
        roleId={decodeURIComponent(oldRoleUpdateMatch[1])}
      />
    );
  }
  if (path === "/admin/roles" || path === "/Role") {
    return <AdminRolesPage isAdmin={access.rolesEdit} />;
  }
  if (path === "/admin/events") {
    return <AdminContentPage type="EVENT" isAdmin={access.contentEdit} />;
  }
  const adminEventEditorMatch = path.match(
    /^\/admin\/events\/(new|\d+\/edit)$/,
  );
  if (adminEventEditorMatch) {
    const itemId =
      adminEventEditorMatch[1] === "new"
        ? null
        : Number(adminEventEditorMatch[1].split("/")[0]);
    return (
      <AdminContentEditorPage type="EVENT" isAdmin={access.contentEdit} itemId={itemId} />
    );
  }
  return <FrontPage />;
}

function AdminRedirectPage({ path }) {
  useEffect(() => {
    if (path) {
      window.history.replaceState({}, "", path);
      window.dispatchEvent(new Event("gammasite:navigate"));
    }
  }, [path]);

  return <p className="muted">Henter Admin...</p>;
}

/**
 * Shared footer with association contact and social links.
 */
function Footer() {
  return (
    <section className="frontpage-footer-band">
      <div className="page-shell frontpage-footer-inner">
        <div className="frontpage-footer-col frontpage-footer-col-wide">
          <h2>Alumneforeningen GamMa</h2>
          <ul className="frontpage-contact-list">
            <li>
              Betingelser og vedtægter: <Link href="/betingelser">Link til dem</Link>
            </li>
            <li>CVR: 34637768</li>
            <li>
              Kontakt:{" "}
              <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a>
            </li>
          </ul>
        </div>
        <div className="frontpage-footer-icons">
          <span className="frontpage-social-list">
            <a
              className="frontpage-social-link"
              href="https://www.linkedin.com/company/gamma-math-ucph/"
              aria-label="LinkedIn"
            >
              in
            </a>
            <a
              className="frontpage-social-link"
              href="https://www.facebook.com/share/g/1Exv7epudc/"
              aria-label="Facebook"
            >
              f
            </a>
            <a
              className="frontpage-social-link"
              href="https://www.instagram.com/gamma_ku_2100"
              aria-label="Instagram"
            >
              <Instagram size={18} />
            </a>
          </span>
        </div>
      </div>
    </section>
  );
}

createRoot(document.getElementById("root")).render(<App />);
