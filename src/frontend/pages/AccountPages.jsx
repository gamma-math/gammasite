import { useEffect, useState } from "react";
import { KeyRound, Mail, Save, Trash2, UserCircle } from "lucide-react";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { accountApi } from "../services/api.js";
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
  { href: "/react/account/manage", label: "Profil" },
  { href: "/react/account/manage/email", label: "Email" },
  { href: "/react/account/manage/password", label: "Password" },
  { href: "/react/account/manage/two-factor", label: "To-faktor authentication" },
  { href: "/react/account/manage/personal-data", label: "Privat data" },
  { href: "/react/account/manage/logout", label: "Log ud" }
];

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
      window.location.href = result.redirectUrl || "/react";
    } catch (err) {
      if (err.status === 405) {
        try {
          await legacyIdentityLogin(form, returnUrl);
          window.location.href = returnUrl || "/react";
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
        <Status error={error} />
        <Field label="Email" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} autoComplete="username" required />
        <Field label="Password" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} autoComplete="current-password" required />
        <label className="account-checkbox">
          <input type="checkbox" checked={form.rememberMe} onChange={(event) => setForm({ ...form, rememberMe: event.target.checked })} />
          <span>Husk mig?</span>
        </label>
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Logger ind..." : "Log ind"}</button>
        <div className="account-links">
          <Link href="/react/account/forgot-password">Glemt dit password?</Link>
          <Link href="/react/account/register">Tilmeld dig som ny bruger</Link>
          <Link href="/react/account/resend-email-confirmation">Gensend email-bekræftelse</Link>
        </div>
        <p className="account-consent">Ved login accepteres <Link href="/react/cookies">Cookie</Link>-politik.</p>
      </form>
    </AuthCard>
  );
}

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
        <Status message={message} error={error} />
        <Field label="Email" type="email" value={form.email} onChange={(value) => setForm({ ...form, email: value })} autoComplete="username" required />
        <Field label="Password" type="password" value={form.password} onChange={(value) => setForm({ ...form, password: value })} autoComplete="new-password" required />
        <Field label="Bekræft password" type="password" value={form.confirmPassword} onChange={(value) => setForm({ ...form, confirmPassword: value })} autoComplete="new-password" required />
        <Field label="Navn" value={form.navn} onChange={(value) => setForm({ ...form, navn: value })} autoComplete="name" required />
        <Field label="Telefonnummer" type="tel" value={form.phoneNumber} onChange={(value) => setForm({ ...form, phoneNumber: value })} autoComplete="tel" required />
        <Field label="Årgang (start på studiet)" type="number" value={form.aargang} onChange={(value) => setForm({ ...form, aargang: value })} required />
        <Field label="Beskæftigelse ved arbejdsgiver" value={form.beskaeftigelse} onChange={(value) => setForm({ ...form, beskaeftigelse: value })} autoComplete="organization-title" required />
        <Field label="Adresse" value={form.adresse} onChange={(value) => setForm({ ...form, adresse: value })} required />
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Opretter..." : "Tilmeld"}</button>
        <p className="account-consent"><Link href="/react/betingelser">Brugerbetingelser</Link> og <Link href="/react/cookies">Cookie</Link>-politik accepteres ved tilmelding.</p>
      </form>
    </AuthCard>
  );
}

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

export function AccountManagePage({ user, section = "profile" }) {
  const [profile, setProfile] = useState(null);
  const [profileForm, setProfileForm] = useState(null);
  const [email, setEmail] = useState("");
  const [passwordForm, setPasswordForm] = useState({ oldPassword: "", newPassword: "", confirmPassword: "" });
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

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
        visibility: Boolean(result.visibility)
      });
      setEmail(result.email ?? "");
    }).catch((err) => setError(err.message));
  }, [user.isAuthenticated]);

  if (!user.isAuthenticated) {
    return <AccountRequired />;
  }

  return (
    <MenuLayout active={`/react/account/manage${section === "profile" ? "" : `/${section}`}`} title="Profil" isAuthenticated extraItems={manageItems} includeDefaultItems={false}>
      <div className="menu-panel-header">
        <div>
          <p className="menu-section-title">Min konto</p>
          <h1>Administrér din bruger</h1>
          <p className="menu-panel-lead menu-panel-lead-inline">Skift dine brugerindstillinger og vælg, hvilke oplysninger andre medlemmer kan se.</p>
        </div>
      </div>
      <section className="account-manage-card">
        <Status message={message} error={error} />
        {section === "profile" && profileForm && (
          <ProfileForm form={profileForm} setForm={setProfileForm} profile={profile} isSubmitting={isSubmitting} onSubmit={async (event) => {
            event.preventDefault();
            await runSubmit(setMessage, setError, setIsSubmitting, async () => {
              const result = await accountApi.updateProfile({ ...profileForm, aargang: Number(profileForm.aargang) });
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
        {section === "personal-data" && <PersonalDataPanel />}
        {section === "delete-personal-data" && <DeletePersonalDataPanel />}
        {section === "logout" && <LogoutPanel />}
      </section>
    </MenuLayout>
  );
}

function ProfileForm({ form, setForm, profile, isSubmitting, onSubmit }) {
  return (
    <form className="account-form account-manage-form" onSubmit={onSubmit}>
      <Field label="Brugernavn" value={profile.username ?? ""} disabled />
      <Field label="Navn" value={form.navn} onChange={(value) => setForm({ ...form, navn: value })} />
      <Field label="Adresse" value={form.adresse} onChange={(value) => setForm({ ...form, adresse: value })} />
      <Field label="Årgang" type="number" value={form.aargang} onChange={(value) => setForm({ ...form, aargang: value })} />
      <Field label="Telefon" type="tel" value={form.phoneNumber} onChange={(value) => setForm({ ...form, phoneNumber: value })} />
      <Field label="Beskæftigelse" value={form.beskaeftigelse} onChange={(value) => setForm({ ...form, beskaeftigelse: value })} />
      <Field label="Brugerstatus" value={profile.status ?? ""} disabled />
      <label className="account-checkbox">
        <input type="checkbox" checked={form.visibility} onChange={(event) => setForm({ ...form, visibility: event.target.checked })} />
        <span>Brugerinfo synlig for medlemmer</span>
      </label>
      <button className="profile-button" type="submit" disabled={isSubmitting}><Save size={16} /> {isSubmitting ? "Gemmer..." : "Gem"}</button>
    </form>
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
  return (
    <form className="account-form account-manage-form" onSubmit={onSubmit}>
      <Field label="Nuværende password" type="password" value={form.oldPassword} onChange={(value) => setForm({ ...form, oldPassword: value })} autoComplete="current-password" required />
      <Field label="Nyt password" type="password" value={form.newPassword} onChange={(value) => setForm({ ...form, newPassword: value })} autoComplete="new-password" required />
      <Field label="Bekræft nyt password" type="password" value={form.confirmPassword} onChange={(value) => setForm({ ...form, confirmPassword: value })} autoComplete="new-password" required />
      <button className="profile-button" type="submit" disabled={isSubmitting}><KeyRound size={16} /> {isSubmitting ? "Opdaterer..." : "Opdater password"}</button>
    </form>
  );
}

function TwoFactorPanel() {
  return (
    <div className="account-form account-manage-form">
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
        <Link className="frontpage-button frontpage-button-secondary" href="/react/account/manage/delete-personal-data">Slet bruger</Link>
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
      window.location.href = result?.redirectUrl || "/react";
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="account-form account-manage-form" onSubmit={deleteAccount}>
      <Status error={error} />
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
      window.location.href = result?.redirectUrl || "/react";
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="account-form account-manage-form">
      <Status error={error} />
      <p className="account-help-text">Log ud af din GamMa-bruger på denne enhed.</p>
      <button className="profile-button" type="button" onClick={logout} disabled={isSubmitting}>
        {isSubmitting ? "Logger ud..." : "Log ud"}
      </button>
    </div>
  );
}

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
        <Status message={message} error={error} />
        <Field label="Email" type="email" value={email} onChange={setEmail} autoComplete="username" required />
        <button className="profile-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Sender..." : buttonLabel}</button>
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

function Field({ label, value, onChange, type = "text", disabled = false, ...props }) {
  return (
    <label className="account-field">
      <span>{label}</span>
      <input type={type} value={value} onChange={(event) => onChange?.(event.target.value)} disabled={disabled} placeholder={label} {...props} />
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
        <Link className="frontpage-button frontpage-button-primary" href="/react/account/login">Login</Link>
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
  return params.get("returnUrl") ?? params.get("ReturnUrl") ?? "/react";
}

async function legacyIdentityLogin(form, returnUrl) {
  const loginUrl = `/Identity/Account/Login?ReturnUrl=${encodeURIComponent(returnUrl || "/react")}`;
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
