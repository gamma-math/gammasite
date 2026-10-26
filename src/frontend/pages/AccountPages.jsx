import { useEffect, useMemo, useState } from "react";
import { KeyRound, Mail, Save, Trash2, UserCircle } from "lucide-react";
import { ConfirmationDialog } from "../components/ConfirmationDialog.jsx";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Pagination, SearchToolbar, SortableHeader, usePagedItems } from "./MembersPage.jsx";
import { accountApi } from "../services/api.js";
import { financeApi } from "../services/financeApi.js";
import { Link, navigate } from "../routes/navigation.jsx";

const initialRegister = {
  email: "",
  password: "",
  confirmPassword: "",
  navn: "",
  phoneNumber: "",
  aargang: "",
  beskaeftigelse: "",
  adresse: ""
};

const manageItems = [
  { href: "/account/manage", label: "Profil" },
  { href: "/account/manage/email", label: "Email" },
  { href: "/account/manage/password", label: "Password" },
  { href: "/account/manage/two-factor", label: "To-faktor authentication" },
  { href: "/account/manage/subscriptions", label: "Mine betalingsaftaler" },
  { href: "/account/manage/finance", label: "Mine transaktioner" },
  { href: "/account/manage/personal-data", label: "Privat data" },
  { href: "/account/manage/logout", label: "Log ud" }
];

/**
 * React login form that posts to the legacy Identity login endpoint.
 */
export function LoginPage() {
  const [form, setForm] = useState({ email: "", password: "", rememberMe: false });
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const returnUrl = getReturnUrl();

  async function submit(event) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      const result = await accountApi.login({ ...form, returnUrl });
      window.location.href = result.redirectUrl || "/";
    } catch (err) {
      if (err.status === 405) {
        try {
          await legacyIdentityLogin(form, returnUrl);
          window.location.href = returnUrl || "/";
          return;
        } catch (legacyError) {
          setError(legacyError.message);
          return;
        }
      }
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard title="Log ind" subtitle="Indtast dine login-oplysninger.">
      <form className="account-form" onSubmit={submit}>
        <Field label="Email" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} autoComplete="username" required />
        <Field label="Password" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} autoComplete="current-password" required />
        <label className="account-checkbox">
          <input type="checkbox" checked={form.rememberMe} onChange={(event) => setForm({ ...form, rememberMe: event.target.checked })} />
          <span>Husk mig?</span>
        </label>
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Logger ind..." : "Log ind"}</button>
        <Status error={error} />
        <div className="account-links">
          <Link href="/account/forgot-password">Glemt dit password?</Link>
          <Link href="/account/register">Tilmeld dig som ny bruger</Link>
          <Link href="/account/resend-email-confirmation">Gensend email-bekræftelse</Link>
        </div>
        <p className="account-consent">Ved login accepteres <Link href="/cookies">Cookie</Link>-politik.</p>
      </form>
    </AuthCard>
  );
}

/**
 * React registration form for new members.
 */
export function RegisterPage() {
  const [form, setForm] = useState(initialRegister);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const returnUrl = getReturnUrl();

  async function submit(event) {
    event.preventDefault();
    setError("");
    setMessage("");
    setIsSubmitting(true);
    try {
      const result = await accountApi.register({ ...form, aargang: Number(form.aargang), returnUrl });
      if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
        return;
      }
      setMessage(result.message || "Din bruger er oprettet. Tjek din email.");
      setForm(initialRegister);
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard title="Opret ny bruger" wide>
      <form className="account-form account-form-compact" onSubmit={submit}>
        <Field label="Email*" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} autoComplete="username" required />
        <Field label="Password*" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} autoComplete="new-password" required />
        <Field label="Bekræft password*" type="password" value={form.confirmPassword} onChange={(value) => setForm({ ...form, confirmPassword: value })} autoComplete="new-password" required />
        <Field label="Navn*" value={form.navn} onChange={(value) => setForm({ ...form, navn: value })} autoComplete="name" required />
        <Field label="Telefonnummer*" type="tel" value={form.phoneNumber} onChange={(value) => setForm({ ...form, phoneNumber: value })} autoComplete="tel" required />
        <Field label="Årgang* (start på studiet)" type="number" value={form.aargang} onChange={(value) => setForm({ ...form, aargang: value })} required />
        <Field label="Beskæftigelse" value={form.beskaeftigelse} onChange={(value) => setForm({ ...form, beskaeftigelse: value })} autoComplete="organization-title" />
        <Field label="Adresse" value={form.adresse} onChange={(value) => setForm({ ...form, adresse: value })} />
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Opretter..." : "Tilmeld"}</button>
        <Status message={message} error={error} />
        <p className="account-consent"><Link href="/betingelser">Brugerbetingelser</Link> og <Link href="/cookies">Cookie</Link>-politik accepteres ved tilmelding.</p>
      </form>
    </AuthCard>
  );
}

/**
 * Account recovery page for starting a password reset email flow.
 */
export function ForgotPasswordPage() {
  return (
    <EmailActionPage
      title="Glemt dit kodeord?"
      subtitle="Indtast din email, så sender vi et link til nulstilling."
      buttonLabel="Nulstil kodeord"
      submit={accountApi.forgotPassword}
    />
  );
}

/**
 * Account recovery page for resending the email confirmation message.
 */
export function ResendEmailConfirmationPage() {
  return (
    <EmailActionPage
      title="Gensend email-bekræftelse"
      subtitle="Indtast din email, så sender vi bekræftelseslinket igen."
      buttonLabel="Gensend"
      submit={accountApi.resendEmailConfirmation}
    />
  );
}

/**
 * React account management hub for profile, email, password, 2FA, and data actions.
 */
export function AccountManagePage({ user, section = "profile" }) {
  const [profile, setProfile] = useState(null);
  const [profileForm, setProfileForm] = useState(null);
  const [email, setEmail] = useState("");
  const [passwordForm, setPasswordForm] = useState({ oldPassword: "", newPassword: "", confirmPassword: "" });
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [financeLastUpdated, setFinanceLastUpdated] = useState(null);

  useEffect(() => {
    if (!user.isAuthenticated) {
      return;
    }

    accountApi.profile().then((result) => {
      setProfile(result);
      setProfileForm({
        navn: result.navn ?? "",
        adresse: result.adresse ?? "",
        aargang: result.aargang ?? "",
        phoneNumber: result.phoneNumber ?? "",
        beskaeftigelse: result.beskaeftigelse ?? "",
        isStudent: Boolean(result.isStudent) || result.status === "STUDERENDE",
        visibility: Boolean(result.visibility)
      });
      setEmail(result.email ?? "");
    }).catch((err) => setError(err.message));
  }, [user.isAuthenticated]);

  useEffect(() => {
    setMessage("");
    setError("");
    if (section === "finance") {
      setFinanceLastUpdated(null);
    }
  }, [section]);

  if (!user.isAuthenticated) {
    return <AccountRequired />;
  }

  const isFinanceSection = section === "finance";
  const isSubscriptionsSection = section === "subscriptions";

  return (
    <MenuLayout active={`/account/manage${section === "profile" ? "" : `/${section}`}`} title="Profil" isAuthenticated extraItems={manageItems} includeDefaultItems={false}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Min konto</p>
          <p className="menu-panel-lead menu-panel-lead-inline">
            {isFinanceSection
              ? "Se dine egne transaktioner i GamMa."
              : isSubscriptionsSection
                ? "Se dine betalingsaftaler hos GamMa."
              : "Skift dine brugerindstillinger og vælg, hvilke oplysninger andre medlemmer kan se."}
          </p>
        </div>
        {isFinanceSection && (
          <p className="account-finance-last-updated">Senest opdateret<br /><strong>{financeLastUpdated ?? "Henter..."}</strong></p>
        )}
      </div>
      <section className="account-manage-card">
        {section === "profile" && profileForm && (
          <ProfileForm form={profileForm} setForm={setProfileForm} profile={profile} isSubmitting={isSubmitting} onSubmit={async (event) => {
            event.preventDefault();
            await runSubmit(setMessage, setError, setIsSubmitting, async () => {
              const result = await accountApi.updateProfile({ ...profileForm, aargang: Number(profileForm.aargang) });
              setProfile((current) => ({
                ...current,
                status: result.status ?? current?.status,
                isStudent: result.isStudent ?? current?.isStudent,
                membershipPaidAt: result.membershipPaidAt ?? current?.membershipPaidAt
              }));
              setProfileForm((current) => ({
                ...current,
                isStudent: result.isStudent ?? current?.isStudent
              }));
              setMessage(result.message);
            });
          }} />
        )}
        {section === "email" && profile && (
          <EmailForm profile={profile} email={email} setEmail={setEmail} isSubmitting={isSubmitting} onSendVerification={async () => {
            await runSubmit(setMessage, setError, setIsSubmitting, async () => {
              const result = await accountApi.sendVerificationEmail();
              setMessage(result.message);
            });
          }} onSubmit={async (event) => {
            event.preventDefault();
            await runSubmit(setMessage, setError, setIsSubmitting, async () => {
              const result = await accountApi.changeEmail(email);
              setMessage(result.message);
            });
          }} />
        )}
        {section === "password" && (
          <PasswordForm form={passwordForm} setForm={setPasswordForm} isSubmitting={isSubmitting} onSubmit={async (event) => {
            event.preventDefault();
            await runSubmit(setMessage, setError, setIsSubmitting, async () => {
              const result = await accountApi.changePassword(passwordForm);
              setMessage(result.message);
              setPasswordForm({ oldPassword: "", newPassword: "", confirmPassword: "" });
            });
          }} />
        )}
        {section === "two-factor" && <TwoFactorPanel />}
        {section === "subscriptions" && <PaymentSubscriptionsPanel />}
        {section === "finance" && <FinancePostingsPanel setLastUpdated={setFinanceLastUpdated} />}
        {section === "personal-data" && <PersonalDataPanel />}
        {section === "delete-personal-data" && <DeletePersonalDataPanel />}
        {section === "logout" && <LogoutPanel />}
        <Status message={message} error={error} />
      </section>
    </MenuLayout>
  );
}

function PaymentSubscriptionsPanel() {
  const [subscriptions, setSubscriptions] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [deletingId, setDeletingId] = useState("");
  const [subscriptionToCancel, setSubscriptionToCancel] = useState(null);

  useEffect(() => {
    let active = true;
    accountApi.subscriptions()
      .then((result) => {
        if (active) {
          setSubscriptions(Array.isArray(result) ? result : []);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message);
        }
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const requestCancelSubscription = (subscription) => {
    if (!subscription.providerSubscriptionId || deletingId) {
      return;
    }

    setError("");
    setSubscriptionToCancel(subscription);
  };

  const cancelSubscription = async () => {
    const subscription = subscriptionToCancel;
    if (!subscription?.providerSubscriptionId || deletingId) {
      return;
    }

    setDeletingId(subscription.providerSubscriptionId);
    try {
      const result = await accountApi.cancelSubscription(subscription.providerSubscriptionId);
      setSubscriptions((current) => current.map((item) => item.providerSubscriptionId === subscription.providerSubscriptionId
        ? { ...item, status: result?.status || "canceled" }
        : item));
    } catch (err) {
      setError(err.message);
    } finally {
      setDeletingId("");
      setSubscriptionToCancel(null);
    }
  };

  const subscription = subscriptions[0];

  return (
    <section className="account-subscriptions-panel">
      {isLoading && <p className="muted">Henter dine betalingsaftaler...</p>}
      {error && <p className="status-message status-message-error">{error}</p>}
      {!isLoading && !error && (
        subscription ? (
          <>
            <div className="account-subscription-overview-heading">
              <div>
                <p className="menu-section-title">Betalingsaftale</p>
                <h3>{subscription.productName || "Betalingsaftale"}</h3>
                <p>{subscription.provider || "—"} · {subscription.status || "—"}</p>
              </div>
              <span className={subscriptionStatusClassName(subscription.status)}>{subscription.status || "—"}</span>
            </div>

            <dl className="account-subscription-overview-grid">
              <SubscriptionDetailItem label="Produkt" value={subscription.productName} />
              <SubscriptionDetailItem label="Pris" value={formatSubscriptionAmount(subscription.amountMinor, subscription.currency)} />
              <SubscriptionDetailItem label="Udbyder" value={subscription.provider} />
              <SubscriptionDetailItem label="Status" value={subscription.status} />
              <SubscriptionDetailItem label="Senest betalt" value={formatSubscriptionDate(subscription.lastPaidAt)} />
              <SubscriptionDetailItem label="Næste opkrævning" value={formatSubscriptionDate(subscription.nextChargeAt)} />
              <SubscriptionDetailItem label="Oprettet" value={formatSubscriptionDate(subscription.created)} />
              <SubscriptionDetailItem label="Opdateret" value={formatSubscriptionDate(subscription.updated)} />
            </dl>

            <div className="account-subscription-overview-actions">
              <SubscriptionCancelButton subscription={subscription} deletingId={deletingId} onRequestCancel={requestCancelSubscription} />
            </div>
          </>
        ) : (
          <p className="muted">Du har ingen betalingsaftale endnu.</p>
        )
      )}

      {subscriptionToCancel && (
        <ConfirmationDialog
          title="Annuller abonnement"
          message={`Er du sikker på, at du vil annullere abonnementet${subscriptionToCancel.productName ? ` for ${subscriptionToCancel.productName}` : ""}? Aftalen annulleres, og fremtidige opkrævninger stoppes.`}
          confirmLabel="Ja, annuller abonnement"
          busyLabel="Annullerer..."
          onConfirm={cancelSubscription}
          onCancel={() => !deletingId && setSubscriptionToCancel(null)}
          isBusy={Boolean(deletingId)}
        />
      )}
    </section>
  );
}

function SubscriptionDetailItem({ label, value }) {
  return (
    <div className="account-subscription-overview-item">
      <dt>{label}</dt>
      <dd>{value || "—"}</dd>
    </div>
  );
}

function subscriptionStatusClassName(status) {
  const normalizedStatus = String(status || "").toLowerCase();
  if (["canceled", "cancelled", "expired"].includes(normalizedStatus)) {
    return "account-subscription-status account-subscription-status-terminal";
  }
  if (["pending", "past_due", "incomplete"].includes(normalizedStatus)) {
    return "account-subscription-status account-subscription-status-pending";
  }
  return "account-subscription-status";
}

function SubscriptionCancelButton({ subscription, deletingId, onRequestCancel }) {
  return (
    <button
      className="account-subscription-delete-button"
      type="button"
      disabled={!subscription.providerSubscriptionId || subscription.status === "canceled" || deletingId === subscription.providerSubscriptionId}
      onClick={() => onRequestCancel(subscription)}
    >
      {deletingId === subscription.providerSubscriptionId ? "Annullerer..." : subscription.status === "canceled" ? "Annulleret" : "Annuller"}
    </button>
  );
}

function ProfileForm({ form, setForm, profile, isSubmitting, onSubmit }) {
  const isPaid = profile.status === "BETALT";
  const isStudentStatusLocked = profile.status === "STUDERENDE" || isPaid;
  const [fieldErrors, setFieldErrors] = useState({});

  const validateField = (field, value) => {
    const normalizedValue = String(value ?? "").trim();
    if ((field === "navn" || field === "phoneNumber") && !normalizedValue) {
      return field === "navn" ? "Navn må ikke være tomt." : "Telefon må ikke være tom.";
    }

    if (field === "aargang") {
      if (!normalizedValue) {
        return "Årgang må ikke være tom.";
      }

      const year = Number(normalizedValue);
      if (!Number.isInteger(year) || year < 1849 || year > 2026) {
        return "Årgang skal være mellem 1849 og 2026.";
      }
    }

    return "";
  };

  const updateField = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
    setFieldErrors((current) => ({ ...current, [field]: validateField(field, value) }));
  };

  const handleSubmit = (event) => {
    const nextErrors = {
      navn: validateField("navn", form.navn),
      aargang: validateField("aargang", form.aargang),
      phoneNumber: validateField("phoneNumber", form.phoneNumber)
    };
    const errors = Object.fromEntries(Object.entries(nextErrors).filter(([, error]) => error));

    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      event.preventDefault();
      return;
    }

    onSubmit(event);
  };

  return (
    <form className="account-form account-manage-form" onSubmit={handleSubmit} noValidate>
      <Field label="Brugernavn" value={profile.username ?? ""} disabled />
      <Field name="navn" label="Navn" value={form.navn} onChange={(value) => updateField("navn", value)} onBlur={(event) => updateField("navn", event.target.value)} required error={fieldErrors.navn} />
      <Field label="Adresse" value={form.adresse} onChange={(value) => setForm({ ...form, adresse: value })} />
      <Field name="aargang" label="Årgang" type="number" value={form.aargang} onChange={(value) => updateField("aargang", value)} onBlur={(event) => updateField("aargang", event.target.value)} min="1849" max="2026" required error={fieldErrors.aargang} />
      <Field name="phoneNumber" label="Telefon" type="tel" value={form.phoneNumber} onChange={(value) => updateField("phoneNumber", value)} onBlur={(event) => updateField("phoneNumber", event.target.value)} required error={fieldErrors.phoneNumber} />
      <Field label="Beskæftigelse" value={form.beskaeftigelse} onChange={(value) => setForm({ ...form, beskaeftigelse: value })} />
      <Field label="Brugerstatus" value={profile.status ?? ""} disabled />
      <label className="account-checkbox">
        <input type="checkbox" checked={Boolean(form.isStudent)} disabled={isStudentStatusLocked} onChange={(event) => setForm({ ...form, isStudent: event.target.checked })} />
        <span>
          Jeg er stadig studerende
          {isPaid && <small className="account-checkbox-help">Studerende-status kan ikke ændres, når medlemskabet er betalt.</small>}
        </span>
      </label>
      <label className="account-checkbox">
        <input type="checkbox" checked={form.visibility} onChange={(event) => setForm({ ...form, visibility: event.target.checked })} />
        <span>Brugerinfo synlig for medlemmer</span>
      </label>
      <button className="profile-button" type="submit" disabled={isSubmitting}><Save size={16} /> {isSubmitting ? "Gemmer..." : "Gem"}</button>
    </form>
  );
}

function FinancePostingsPanel({ setLastUpdated }) {
  const [postings, setPostings] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [sort, setSort] = useState({ key: "date", direction: "desc" });
  const [search, setSearch] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [page, setPage] = useState(1);
  const [isTableExpanded, setIsTableExpanded] = useState(false);
  const filtered = useFilteredPostings(postings, search);
  const sorted = useMemo(() => sortPostings(filtered, sort), [filtered, sort]);
  const { currentPage, pageCount, visibleItems } = usePagedItems(sorted, page, pageSize);

  useEffect(() => {
    if (!isTableExpanded) return undefined;
    const previousOverflow = document.body.style.overflow;
    const closeOnEscape = (event) => {
      if (event.key === "Escape") setIsTableExpanded(false);
    };
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", closeOnEscape);
    };
  }, [isTableExpanded]);

  useEffect(() => {
    let active = true;
    setIsLoading(true);
    setError("");
    financeApi.postings()
      .then((result) => {
        if (active) {
          setPostings(result.postings ?? []);
          setLastUpdated(result.lastUpdated ?? null);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message);
        }
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [setLastUpdated]);

  useEffect(() => {
    setPage(1);
  }, [search, pageSize, sort.key, sort.direction]);

  if (isLoading) {
    return <p className="muted">Henter dine transaktioner...</p>;
  }

  if (error) {
    return <p className="status-message status-message-error">{error}</p>;
  }

  return (
    <>
      <div className={`account-finance-table-container${isTableExpanded ? " is-expanded" : ""}`}>
        <div className="account-finance-table-toolbar">
          <SearchToolbar search={search} setSearch={setSearch} pageSize={pageSize} setPageSize={setPageSize} searchPlaceholder="Tekst" />
          <button
            className={`finance-table-expand-button${isTableExpanded ? " is-close" : ""}`}
            type="button"
            onClick={() => setIsTableExpanded((current) => !current)}
            title={isTableExpanded ? "Luk udvidet tabel" : "Udvid tabel"}
            aria-label={isTableExpanded ? "Luk udvidet tabel" : "Udvid tabel"}
          >
            <span aria-hidden="true">{isTableExpanded ? "\u00d7" : "\u26f6"}</span>
            {isTableExpanded && <span>Luk tabel</span>}
          </button>
        </div>
      <div className="menu-table-wrap account-finance-table-wrap">
        <table className="menu-member-table account-finance-table">
          <thead>
            <tr>
              <SortableHeader label="Dato" sortKey="date" sort={sort} setSort={setSort} />
              <SortableHeader label="Beløb" sortKey="amount" sort={sort} setSort={setSort} />
              <SortableHeader label="Type" sortKey="type" sort={sort} setSort={setSort} />
              <SortableHeader label="Tekst" sortKey="text" sort={sort} setSort={setSort} />
              <SortableHeader label="Kilde" sortKey="sourceType" sort={sort} setSort={setSort} />
              <SortableHeader label="Kategori" sortKey="postingGroup" sort={sort} setSort={setSort} />
            </tr>
          </thead>
          <tbody>
            {visibleItems.map((posting) => (
              <tr key={posting.id}>
                <td>{formatPostingDate(posting.date)}</td>
                <td className={posting.amount < 0 ? "account-finance-amount is-negative" : "account-finance-amount is-positive"}>{formatPostingAmount(posting.amount)}</td>
                <td>{postingType(posting.amount)}</td>
                <td>{posting.text}</td>
                <td><span className={`account-finance-source ${posting.sourceType === "MobilePay" ? "is-mobilepay" : "is-bank"}`}>{posting.sourceType}</span></td>
                <td>{posting.postingGroup || "—"}</td>
              </tr>
            ))}
            {visibleItems.length === 0 && (
              <tr>
                <td colSpan="6">{postings.length === 0 ? "Der er ingen transaktioner knyttet til din bruger endnu." : "Ingen transaktioner matcher søgningen."}</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
        <Pagination page={currentPage} pageCount={pageCount} total={sorted.length} pageSize={pageSize} setPage={setPage} />
      </div>
    </>
  );
}

function EmailForm({ profile, email, setEmail, isSubmitting, onSubmit, onSendVerification }) {
  return (
    <form className="account-form account-manage-form" onSubmit={onSubmit}>
      <div className="account-readonly-row">
        <span>Nuværende email</span>
        <strong>{profile.email}</strong>
        <small>{profile.isEmailConfirmed ? "Bekræftet" : "Ikke bekræftet"}</small>
      </div>
      {!profile.isEmailConfirmed && (
        <button className="frontpage-button frontpage-button-secondary" type="button" onClick={onSendVerification} disabled={isSubmitting}>
          Send bekræftelses-mail
        </button>
      )}
      <Field label="Ny email" type="email" value={email} onChange={setEmail} autoComplete="email" required />
      <button className="profile-button" type="submit" disabled={isSubmitting}><Mail size={16} /> {isSubmitting ? "Sender..." : "Ændr email"}</button>
    </form>
  );
}

function PasswordForm({ form, setForm, isSubmitting, onSubmit }) {
  const [fieldErrors, setFieldErrors] = useState({});

  const validate = (values) => {
    const errors = {};
    const oldPassword = String(values.oldPassword ?? "");
    const newPassword = String(values.newPassword ?? "");
    const confirmPassword = String(values.confirmPassword ?? "");

    if (!oldPassword) {
      errors.oldPassword = "Nuværende password må ikke være tomt.";
    }
    if (!newPassword) {
      errors.newPassword = "Nyt password må ikke være tomt.";
    }
    if (!confirmPassword) {
      errors.confirmPassword = "Bekræft nyt password må ikke være tomt.";
    } else if (newPassword !== confirmPassword) {
      errors.confirmPassword = "Nyt password og bekræftelsen matcher ikke.";
    }

    return errors;
  };

  const updateField = (field, value) => {
    const nextForm = { ...form, [field]: value };
    setForm(nextForm);
    setFieldErrors(validate(nextForm));
  };

  const handleSubmit = (event) => {
    const errors = validate(form);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      event.preventDefault();
      return;
    }

    onSubmit(event);
  };

  return (
    <form className="account-form account-manage-form" onSubmit={handleSubmit} noValidate>
      <Field name="oldPassword" label="Nuværende password" type="password" value={form.oldPassword} onChange={(value) => updateField("oldPassword", value)} onBlur={(event) => updateField("oldPassword", event.target.value)} autoComplete="current-password" required error={fieldErrors.oldPassword} />
      <Field name="newPassword" label="Nyt password" type="password" value={form.newPassword} onChange={(value) => updateField("newPassword", value)} onBlur={(event) => updateField("newPassword", event.target.value)} autoComplete="new-password" required error={fieldErrors.newPassword} />
      <Field name="confirmPassword" label="Bekræft nyt password" type="password" value={form.confirmPassword} onChange={(value) => updateField("confirmPassword", value)} onBlur={(event) => updateField("confirmPassword", event.target.value)} autoComplete="new-password" required error={fieldErrors.confirmPassword} />
      <button className="profile-button" type="submit" disabled={isSubmitting}><KeyRound size={16} /> {isSubmitting ? "Opdaterer..." : "Opdater password"}</button>
    </form>
  );
}

function TwoFactorPanel() {
  return (
    <div className="account-form account-manage-form">
      <p className="account-help-text">To-faktor authentication giver din konto et ekstra lag sikkerhed. Du kan opsætte en authenticator app eller nulstille opsætningen.</p>
      <div className="account-action-row">
        <a className="frontpage-button frontpage-button-primary" href="/Identity/Account/Manage/EnableAuthenticator">Indstil authentication app</a>
        <a className="frontpage-button frontpage-button-secondary" href="/Identity/Account/Manage/ResetAuthenticator">Reset authentication app</a>
      </div>
    </div>
  );
}

function PersonalDataPanel() {
  return (
    <div className="account-form account-manage-form">
      <p className="account-help-text">Din profil indeholder de oplysninger, du har givet til GamMa. Du kan hente dine data eller gå videre til sletning af kontoen.</p>
      <div className="account-action-row">
        <a className="frontpage-button frontpage-button-primary" href="/api/account/personal-data">Hent data</a>
        <Link className="frontpage-button frontpage-button-secondary" href="/account/manage/delete-personal-data">Slet bruger</Link>
      </div>
    </div>
  );
}

function DeletePersonalDataPanel() {
  const [password, setPassword] = useState("");
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function deleteAccount(event) {
    event.preventDefault();
    if (!confirmDelete) {
      setError("Bekræft venligst at du vil slette brugeren permanent.");
      return;
    }

    setError("");
    setIsSubmitting(true);
    try {
      const result = await accountApi.deleteAccount(password);
      window.location.href = result?.redirectUrl || "/";
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="account-form account-manage-form" onSubmit={deleteAccount}>
      <div className="account-danger-panel">
        <strong>Sletning er permanent.</strong>
        <span>Din bruger og dine personlige oplysninger fjernes, og handlingen kan ikke fortrydes.</span>
      </div>
      <Field label="Password" type="password" value={password} onChange={setPassword} autoComplete="current-password" required />
      <label className="account-checkbox">
        <input type="checkbox" checked={confirmDelete} onChange={(event) => setConfirmDelete(event.target.checked)} />
        <span>Jeg forstår, at min bruger slettes permanent</span>
      </label>
      <button className="profile-button profile-button-danger" type="submit" disabled={isSubmitting}>
        <Trash2 size={16} /> {isSubmitting ? "Sletter..." : "Slet bruger"}
      </button>
      <Status error={error} />
    </form>
  );
}

function LogoutPanel() {
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function logout() {
    setError("");
    setIsSubmitting(true);
    try {
      const result = await accountApi.logout();
      window.location.href = result?.redirectUrl || "/";
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="account-form account-manage-form">
      <p className="account-help-text">Log ud af din bruger.</p>
      <button className="profile-button" type="button" onClick={logout} disabled={isSubmitting}>
        {isSubmitting ? "Logger ud..." : "Log ud"}
      </button>
      <Status error={error} />
    </div>
  );
}

/**
 * Shared one-field account form used by email recovery flows.
 */
function EmailActionPage({ title, subtitle, buttonLabel, submit }) {
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function onSubmit(event) {
    event.preventDefault();
    await runSubmit(setMessage, setError, setIsSubmitting, async () => {
      const result = await submit(email);
      setMessage(result.message);
    });
  }

  return (
    <AuthCard title={title} subtitle={subtitle}>
      <form className="account-form" onSubmit={onSubmit}>
        <Field label="Email" type="email" value={email} onChange={setEmail} autoComplete="username" required />
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Sender..." : buttonLabel}</button>
        <Status message={message} error={error} />
      </form>
    </AuthCard>
  );
}

function AuthCard({ title, subtitle, wide = false, children }) {
  return (
    <main className="account-auth-main">
      <section className={`account-card ${wide ? "account-card-wide" : ""}`}>
        <div className="account-card-copy">
          <h1>{title}</h1>
          {subtitle && <p>{subtitle}</p>}
        </div>
        {children}
      </section>
    </main>
  );
}

function formatPostingDate(value) {
  if (!value) {
    return "";
  }

  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("da-DK", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(date);
}

function formatSubscriptionDate(value) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("da-DK", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Europe/Copenhagen"
  }).format(date);
}

function formatSubscriptionAmount(amountMinor, currency) {
  if (amountMinor === null || amountMinor === undefined || amountMinor === "") {
    return "—";
  }

  const amount = Number(amountMinor);
  if (!Number.isFinite(amount)) {
    return "—";
  }

  return new Intl.NumberFormat("da-DK", {
    style: "currency",
    currency: String(currency || "DKK").toUpperCase(),
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount / 100);
}

function formatPostingAmount(value) {
  const amount = Number(value ?? 0);
  const formatted = new Intl.NumberFormat("da-DK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(Math.abs(amount));

  return amount < 0 ? `(${formatted} kr.)` : `+${formatted} kr.`;
}

function postingType(value) {
  const amount = Number(value ?? 0);
  return amount < 0 ? "Betaling" : amount > 0 ? "Refusion" : "—";
}

function useFilteredPostings(postings, search) {
  return useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return postings;
    }

    return postings.filter((posting) => [
      formatPostingDate(posting.date),
      formatPostingAmount(posting.amount),
      postingType(posting.amount),
      posting.text,
      posting.sourceType,
      posting.account,
      posting.postingGroup
    ].some((value) => String(value ?? "").toLowerCase().includes(term)));
  }, [postings, search]);
}

function sortPostings(postings, sort) {
  return [...postings].sort((left, right) => {
    const result = comparePostingValues(left[sort.key], right[sort.key], sort.key);
    return sort.direction === "asc" ? result : -result;
  });
}

function comparePostingValues(left, right, key) {
  if (key === "amount") {
    return Number(left ?? 0) - Number(right ?? 0);
  }

  if (key === "date") {
    return new Date(left || 0).getTime() - new Date(right || 0).getTime();
  }

  if (key === "type") {
    return postingType(left).localeCompare(postingType(right), "da-DK", { sensitivity: "base" });
  }

  return String(left ?? "").localeCompare(String(right ?? ""), "da-DK", { sensitivity: "base" });
}

function Field({ name, label, value, onChange, type = "text", disabled = false, required = false, error = "", ...props }) {
  const errorId = name ? name + "-error" : undefined;

  return (
    <label className={"account-field" + (error ? " has-error" : "")}>
      <span>{label}{required && <span aria-hidden="true"> *</span>}</span>
      <input id={name} name={name} type={type} value={value} onChange={(event) => onChange?.(event.target.value)} disabled={disabled} required={required} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} placeholder={label} {...props} />
      {error && <small id={errorId} className="account-field-error">{error}</small>}
    </label>
  );
}

function Status({ message, error }) {
  return (
    <>
      {message && <p className="status-message status-message-success">{message}</p>}
      {error && <p className="status-message status-message-error">{error}</p>}
    </>
  );
}

function AccountRequired() {
  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section">
        <h1>Profil</h1>
        <p className="muted">Du skal være logget ind for at administrere din bruger.</p>
        <Link className="frontpage-button frontpage-button-primary" href="/account/login">Login</Link>
      </section>
    </main>
  );
}

async function runSubmit(setMessage, setError, setIsSubmitting, action) {
  setMessage("");
  setError("");
  setIsSubmitting(true);
  try {
    await action();
  } catch (err) {
    setError(err.message);
  } finally {
    setIsSubmitting(false);
  }
}

function getReturnUrl() {
  const params = new URLSearchParams(window.location.search);
  return params.get("returnUrl") ?? params.get("ReturnUrl") ?? "/";
}

async function legacyIdentityLogin(form, returnUrl) {
  const loginUrl = `/Identity/Account/Login?ReturnUrl=${encodeURIComponent(returnUrl || "/")}`;
  const pageResponse = await fetch(loginUrl, { credentials: "same-origin" });
  const html = await pageResponse.text();
  const token = new DOMParser()
    .parseFromString(html, "text/html")
    .querySelector("input[name='__RequestVerificationToken']")
    ?.getAttribute("value");

  if (!token) {
    throw new Error("Login endpointet er ikke aktivt endnu. Genstart den lokale ASP.NET app og prøv igen.");
  }

  const body = new URLSearchParams();
  body.set("Input.Email", form.email);
  body.set("Input.Password", form.password);
  body.set("Input.RememberMe", form.rememberMe ? "true" : "false");
  body.set("__RequestVerificationToken", token);

  const response = await fetch(loginUrl, {
    method: "POST",
    credentials: "same-origin",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body
  });

  if (!response.ok) {
    throw new Error("Login fejlede. Prøv igen.");
  }

  if (new URL(response.url).pathname.toLowerCase().includes("/identity/account/login")) {
    throw new Error("Login fejlede. Prøv igen.");
  }
}
