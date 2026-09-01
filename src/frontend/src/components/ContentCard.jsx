import { CalendarDays } from "lucide-react";
import { Link } from "../routes/navigation.jsx";
import { detailPath, formatDate } from "../utils/format.js";

export function ContentCard({ item, variant }) {
  const image = item.pictureUrl || `https://picsum.photos/seed/gamma-${item.type}-${item.id}/920/560`;
  const isFrontpage = variant === "frontpage";

  return (
    <article className={isFrontpage ? "frontpage-news-card" : "menu-event-card"}>
      <Link className={isFrontpage ? "frontpage-news-link" : "menu-event-link"} href={detailPath(item)} aria-label={`Åbn ${item.title}`}>
        <img className={isFrontpage ? "frontpage-news-image" : "menu-event-image"} src={image} alt="" />
      </Link>
      <div className={isFrontpage ? "frontpage-news-copy" : "menu-event-copy"}>
        {!isFrontpage && <small><CalendarDays size={14} /> {item.type === "EVENT" ? formatDate(item.startDate) : formatDate(item.publishedAt)}</small>}
        {isFrontpage ? (
          <h3><Link className="menu-event-title-link" href={detailPath(item)}>{item.title}</Link></h3>
        ) : (
          <h2><Link className="menu-event-title-link" href={detailPath(item)}>{item.title}</Link></h2>
        )}
        <p>{item.summary || item.body || "Ingen beskrivelse endnu."}</p>
        <div className="tag-row">
          <span className="tag tag-kind">{item.type === "EVENT" ? "Arrangement" : "Nyhed"}</span>
          {(item.tags ?? "").split(",").filter(Boolean).slice(0, 3).map((tag) => (
            <span className="tag" key={tag}>{tag.trim()}</span>
          ))}
        </div>
      </div>
    </article>
  );
}
