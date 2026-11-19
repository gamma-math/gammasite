import React, { useEffect, useState } from "react";
import { X } from "lucide-react";
import { contactApi } from "../services/api.js";
import { contactTypes, getContactTopicLabel } from "../services/contactForm.js";

export function ContactModal({ open, onClose, email = "" }) {
  const [form, setForm] = useState({ email: "", contactType: "Arrangement", topic: "", message: "" });
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (open) {
      setForm({ email: email || "", contactType: "Arrangement", topic: "", message: "" });
      setStatus("");
    }
  }, [open, email]);

  useEffect(() => {
    if (!open) return undefined;
    const onKeyDown = (event) => event.key === "Escape" && onClose();
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  const update = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }));
  const submit = async (event) => {
    event.preventDefault();
    setBusy(true);
    setStatus("");
    try {
      const result = await contactApi.send(form);
      setStatus(result.message || "Tak for din henvendelse. Vi har sendt en bekræftelse til din e-mail.");
      setForm((current) => ({ ...current, topic: "", message: "" }));
    } catch (error) {
      setStatus(error.message || "Henvendelsen kunne ikke sendes lige nu. Prøv igen senere.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="contact-modal-backdrop" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="contact-modal" role="dialog" aria-modal="true" aria-labelledby="contact-modal-title">
        <button type="button" className="contact-modal-close" onClick={onClose} aria-label="Luk kontaktformular"><X size={22} /></button>
        <h2 id="contact-modal-title">Kontakt GamMa</h2>
        <form onSubmit={submit}>
          <label>E-mail<input type="email" autoComplete="email" required maxLength="254" value={form.email} onChange={update("email")} /></label>
          <label>Henvendelse<select value={form.contactType} onChange={update("contactType")}>{contactTypes.map((item) => <option key={item}>{item}</option>)}</select></label>
          <label>{getContactTopicLabel(form.contactType)}<input required maxLength="200" value={form.topic} onChange={update("topic")} placeholder={form.contactType === "Arrangement" ? "Skriv arrangementets titel" : "Skriv et emne"} /></label>
          <label>Besked<textarea required maxLength="5000" rows="5" value={form.message} onChange={update("message")} placeholder="Skriv din besked her..." /></label>
          <p className="contact-modal-note">Vi sender en bekræftelse til din e-mail.</p>
          {status && <p className="contact-modal-status" role="status">{status}</p>}
          <div className="contact-modal-actions"><button type="button" className="contact-button-secondary" onClick={onClose}>Annuller</button><button type="submit" className="contact-button-primary" disabled={busy}>{busy ? "Sender…" : "Send besked"}</button></div>
        </form>
      </section>
    </div>
  );
}
