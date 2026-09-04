import { CalendarDays } from "lucide-react";
import { Link } from "../routes/navigation.jsx";
import { contentMetaLabel, detailPath } from "../utils/format.js";
import { sanitizeHtml } from "../utils/richText.js";

/**
 * Displays a news or event teaser card used on overview and front page sections.
 */
export function ContentCard({ item, variant }) {
  const hasImage = Boolean(item.pictureUrl);
  const image = hasImage ? item.pictureUrl : "/lib/logo_blue.png";
  const isFrontpage = variant === "frontpage";

  return (
    <article className={isFrontpage ? "frontpage-news-card" : "menu-event-card"}>
      <Link className={isFrontpage ? "frontpage-news-link" : "menu-event-link"} href={detailPath(item)} aria-label={`Åbn ${item.title}`}>
        <img className={`${isFrontpage ? "frontpage-news-image" : "menu-event-image"} ${hasImage ? "" : "content-logo-fallback"}`.trim()} src={image} alt="" />
      </Link>
      <div className={isFrontpage ? "frontpage-news-copy" : "menu-event-copy"}>
        <small><CalendarDays size={14} /> {contentMetaLabel(item)}</small>
        {isFrontpage ? (
          <h3><Link className="menu-event-title-link" href={detailPath(item)}>{item.title}</Link></h3>
        ) : (
          <h2><Link className="menu-event-title-link" href={detailPath(item)}>{item.title}</Link></h2>
        )}
        <div className="content-card-rich-excerpt">
          <div dangerouslySetInnerHTML={{ __html: sanitizeHtml(item.summary ?? "") }} />
        </div>
        <div className="tag-row">
          {(item.tags ?? "").split(",").filter(Boolean).slice(0, 3).map((tag) => (
            <span className="tag" key={tag}>{tag.trim()}</span>
          ))}
        </div>
      </div>
    </article>
  );
}
