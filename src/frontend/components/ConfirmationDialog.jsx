import { X } from "lucide-react";

export function ConfirmationDialog({
  title,
  message,
  confirmLabel = "Bekræft",
  onConfirm,
  onCancel,
  isBusy = false
}) {
  return (
    <div className="admin-preview-modal-backdrop" role="presentation" onClick={() => !isBusy && onCancel()}>
      <section
        className="admin-recipient-modal"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="admin-preview-modal-header">
          <div>
            <p className="menu-section-title">Bekræft handling</p>
            <h2>{title}</h2>
          </div>
          <button className="admin-preview-close" type="button" aria-label="Luk bekræftelse" onClick={onCancel} disabled={isBusy}>
            <X size={20} />
          </button>
        </div>
        <div className="admin-send-confirm-body">
          <p>{message}</p>
          <div className="menu-editor-actions">
            <button className="frontpage-button frontpage-button-secondary" type="button" onClick={onCancel} disabled={isBusy}>
              Annuller
            </button>
            <button className="profile-button profile-button-danger" type="button" onClick={onConfirm} disabled={isBusy}>
              {isBusy ? "Afmelder..." : confirmLabel}
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
