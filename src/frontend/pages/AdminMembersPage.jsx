import { Fragment, useEffect, useState } from "react";
import { CircleMinus, CirclePlus } from "lucide-react";
import { AdminLayout } from "../layouts/AdminLayout.jsx";
import { membersApi } from "../services/api.js";
import { formatDate } from "../utils/format.js";
import { Link } from "../routes/navigation.jsx";
import { Pagination, SearchToolbar, SortableHeader, useFilteredMembers, usePagedItems, useSortedMembers } from "./MembersPage.jsx";

const statuses = ["OPRETTET", "BETALT", "SKYLDER", "INAKTIV", "STUDERENDE"];

/**
 * Admin member overview for searching, filtering, and updating membership data.
 */
export function AdminMembersPage({ isAdmin }) {
  const [members, setMembers] = useState([]);
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState({ key: "name", direction: "asc" });
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const [expandedMemberIds, setExpandedMemberIds] = useState(new Set());
  const [mass, setMass] = useState({ from: today(), to: today(1), status: "SKYLDER", excludeActiveSubscriptions: false });
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (isAdmin) {
      load();
    }
  }, [isAdmin]);

  async function load() {
    setMembers(await membersApi.listAdmin());
  }

  async function updateStatus(member, status) {
    const updated = await membersApi.updateStatus(member.id, status);
    setMembers((current) => current.map((item) => item.id === member.id ? { ...updated, hasActiveSubscription: item.hasActiveSubscription } : item));
  }

  async function massUpdate(event) {
    event.preventDefault();
    const result = await membersApi.massUpdateStatus(mass);
    setMessage(`${result.updated} medlemmer blev opdateret.`);
    await load();
  }

  function toggleExpanded(memberId) {
    setExpandedMemberIds((current) => {
      const next = new Set(current);
      if (next.has(memberId)) {
        next.delete(memberId);
      } else {
        next.add(memberId);
      }
      return next;
    });
  }

  const filtered = useFilteredMembers(members, search, ["name", "occupation", "email", "phoneNumber", "status", "membershipPaidAt", "createdAt", "hasActiveSubscription"]);
  const sorted = useSortedMembers(filtered, sort);
  const { currentPage, pageCount, visibleItems } = usePagedItems(sorted, page, pageSize);

  useEffect(() => {
    setPage(1);
  }, [search, pageSize, sort.key, sort.direction]);

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/users" canWrite={false}><p className="status-message status-message-warning">Du har ikke rettigheder til at administrere medlemmer.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/users" canWrite={true}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Medlemmer</p>
        </div>
      </div>
      <form className="admin-mass-card" onSubmit={massUpdate}>
        <div className="admin-mass-fields">
          <label className="admin-field"><span>Kontingentdato fra</span><input type="date" value={mass.from} onChange={(event) => setMass({ ...mass, from: event.target.value })} /></label>
          <label className="admin-field"><span>Kontingentdato til</span><input type="date" value={mass.to} onChange={(event) => setMass({ ...mass, to: event.target.value })} /></label>
          <label className="admin-field"><span>Status</span><select value={mass.status} onChange={(event) => setMass({ ...mass, status: event.target.value })}>{statuses.map((status) => <option key={status}>{status}</option>)}</select></label>
          <button className="profile-button" type="submit">Skift status</button>
        </div>
        <label className="admin-checkbox admin-mass-exclusion"><input type="checkbox" checked={mass.excludeActiveSubscriptions} onChange={(event) => setMass({ ...mass, excludeActiveSubscriptions: event.target.checked })} /><span>Fravælg medlemmer med aktivt abonnement</span></label>
        {message && <p className="status-message status-message-success">{message}</p>}
      </form>
      <SearchToolbar search={search} setSearch={setSearch} pageSize={pageSize} setPageSize={setPageSize} />
      <div className="menu-table-wrap">
        <table className="menu-member-table">
          <thead>
            <tr>
              <th aria-label="Detaljer"></th>
              <SortableHeader label="Navn" sortKey="name" sort={sort} setSort={setSort} />
              <SortableHeader label="Årgang" sortKey="graduationYear" sort={sort} setSort={setSort} />
              <SortableHeader label="Status" sortKey="status" sort={sort} setSort={setSort} />
              <SortableHeader label="Email" sortKey="email" sort={sort} setSort={setSort} />
              <SortableHeader label="Kontingentdato" sortKey="membershipPaidAt" sort={sort} setSort={setSort} />
              <SortableHeader label="Aktivt abonnement" sortKey="hasActiveSubscription" sort={sort} setSort={setSort} />
            </tr>
          </thead>
          <tbody>
            {visibleItems.map((member) => {
              const isExpanded = expandedMemberIds.has(member.id);
              return (
                <Fragment key={member.id}>
                  <tr className="menu-member-main-row">
                    <td className="menu-member-expander-cell">
                      <button className="menu-member-expander" type="button" aria-label={isExpanded ? "Skjul detaljer" : "Vis detaljer"} aria-expanded={isExpanded} onClick={() => toggleExpanded(member.id)}>
                        {isExpanded ? <CircleMinus size={20} /> : <CirclePlus size={20} />}
                      </button>
                    </td>
                    <td>{member.name}</td>
                    <td>{member.graduationYear || ""}</td>
                    <td>
                      <select className="menu-member-status-select" value={member.status} onChange={(event) => updateStatus(member, event.target.value)}>
                        {statuses.map((status) => <option key={status}>{status}</option>)}
                      </select>
                    </td>
                    <td>{member.email}</td>
                    <td>{formatMemberDate(member.membershipPaidAt)}</td>
                    <td>{member.hasActiveSubscription ? "Ja" : "Nej"}</td>
                  </tr>
                  {isExpanded && (
                    <tr className="menu-member-detail-row">
                      <td></td>
                      <td colSpan="6">
                        <dl className="menu-member-details">
                          <div>
                            <dt>Beskæftigelse</dt>
                            <dd>{member.occupation || ""}</dd>
                          </div>
                          <div>
                            <dt>Telefon</dt>
                            <dd>{member.phoneNumber || ""}</dd>
                          </div>
                          <div>
                            <dt>Oprettet</dt>
                            <dd>{formatMemberDate(member.createdAt)}</dd>
                          </div>
                          {member.hasActiveSubscription && (
                            <div>
                              <dt>Abonnement</dt>
                              <dd><Link className="admin-subscription-link" href={`/react/admin/users/${encodeURIComponent(member.id)}/subscriptions`}>Se aktivt abonnement</Link></dd>
                            </div>
                          )}
                        </dl>
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
            {visibleItems.length === 0 && (
              <tr>
                <td colSpan="7">Ingen medlemmer matcher søgningen.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      <Pagination page={currentPage} pageCount={pageCount} total={sorted.length} pageSize={pageSize} setPage={setPage} />
    </AdminLayout>
  );
}

export function AdminMemberSubscriptionsPage({ isAdmin, memberId }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAdmin || !memberId) {
      return;
    }

    setData(null);
    setError("");
    membersApi.subscriptions(memberId)
      .then(setData)
      .catch(() => setError("Abonnementet kunne ikke hentes."));
  }, [isAdmin, memberId]);

  if (!isAdmin) {
    return <AdminLayout active="/react/admin/users" canWrite={false}><p className="status-message status-message-warning">Du har ikke rettigheder til at administrere medlemmer.</p></AdminLayout>;
  }

  return (
    <AdminLayout active="/react/admin/users" canWrite={true}>
      <div className="admin-subscription-detail-header">
        <div>
          <p className="menu-section-title">Medlemmer</p>
          <h1>Aktivt abonnement</h1>
          {data?.memberName && <p className="menu-panel-lead">{data.memberName}{data.memberEmail ? ` · ${data.memberEmail}` : ""}</p>}
        </div>
        <Link className="frontpage-button frontpage-button-secondary" href="/react/admin/users">Tilbage til medlemmer</Link>
      </div>
      {error && <p className="status-message status-message-warning">{error}</p>}
      {!data && !error && <p className="muted">Henter abonnement...</p>}
      {data && data.subscriptions.length === 0 && <p className="status-message status-message-warning">Medlemmet har ikke et aktivt abonnement.</p>}
      {data?.subscriptions.length > 0 && (
        <div className="admin-subscription-detail-list">
          {data.subscriptions.map((subscription) => (
            <article className="admin-subscription-detail-card" key={subscription.providerSubscriptionId}>
              <h2>{subscription.productName || "Abonnement"}</h2>
              <div className="account-manage-card">
                <SubscriptionDetail label="Udbyder" value={subscription.provider} />
                <SubscriptionDetail label="Status" value={subscription.status} />
                <SubscriptionDetail label="Senest betalt" value={formatSubscriptionDate(subscription.lastPaidAt)} />
                <SubscriptionDetail label="Næste opkrævning" value={formatSubscriptionDate(subscription.nextChargeAt)} />
                <SubscriptionDetail label="Nuværende periode fra" value={formatSubscriptionDate(subscription.currentPeriodStart)} />
                <SubscriptionDetail label="Nuværende periode til" value={formatSubscriptionDate(subscription.currentPeriodEnd)} />
                <SubscriptionDetail label="Oprettet" value={formatSubscriptionDate(subscription.created)} />
                <SubscriptionDetail label="Opdateret" value={formatSubscriptionDate(subscription.updated)} />
                <SubscriptionDetail label="Annulleres ved periodens udløb" value={subscription.cancelAtPeriodEnd ? "Ja" : "Nej"} />
                <SubscriptionDetail label="Abonnements-id" value={subscription.providerSubscriptionId} />
                <SubscriptionDetail label="Produkt-id" value={subscription.providerProductId} />
                <SubscriptionDetail label="Pris-id" value={subscription.providerPriceId} />
                {subscription.providerDashboardUrl && (
                  <a className="frontpage-button frontpage-button-secondary admin-subscription-dashboard-link" href={subscription.providerDashboardUrl} target="_blank" rel="noreferrer">Åbn i Stripe</a>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </AdminLayout>
  );
}

function SubscriptionDetail({ label, value }) {
  return (
    <div className="account-readonly-row">
      <span>{label}</span>
      <strong>{value || "—"}</strong>
    </div>
  );
}

function today(offset = 0) {
  const date = new Date();
  date.setDate(date.getDate() + offset);
  return date.toISOString().slice(0, 10);
}

function formatMemberDate(value) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime()) || date.getFullYear() <= 1901) return "";
  return formatDate(value);
}

function formatSubscriptionDate(value) {
  return value ? formatDate(value) : "Ingen dato";
}
