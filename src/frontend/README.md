# GamMaSite Frontend

This folder contains the React/Vite application used for the current public, member, account, and admin user experience.

The frontend is built as static assets and copied into `src/backend/GamMaSite/wwwroot/react-assets/`, where the ASP.NET Core React shell serves it.

## Structure

Only tracked project folders/files are listed here:

```text
frontend/
|-- app/                           Root React app, shared header/footer, user loading, and route mapping.
|-- components/                    Reusable UI components such as content cards.
|-- layouts/                       Shared layouts for member-facing and admin pages.
|-- pages/                         Route-level pages for public, account, admin, payment, member, calendar, and event flows.
|-- routes/                        Lightweight navigation helpers used instead of a full router package.
|-- services/                      Browser API client for ASP.NET Core endpoints.
|-- styles/                        Main application stylesheet.
|-- utils/                         Shared formatting, avatar, and rich-text helpers.
|-- index.html                     Vite HTML entry file.
|-- package.json                   Frontend dependencies and scripts.
|-- package-lock.json              Locked frontend dependency versions.
`-- vite.config.js                 Vite configuration and backend asset output path.
```

## Routing

The app uses a small custom router in `app/main.jsx` and `routes/navigation.jsx`.

* Internal `/react/*` links update browser history without a full reload.
* Selected legacy URLs are also mapped to React pages by the backend and frontend router.
* The backend remains responsible for direct requests, authentication cookies, CSRF tokens, and API authorization.

## Build

```bash
cd src/frontend
npm install
npm run build
```

The build output is written to:

```text
../backend/GamMaSite/wwwroot/react-assets/
```

Commit source changes and the built React assets when deployment expects prebuilt frontend files.

## Documentation Style

Code comments should stay short and describe purpose/responsibility. Avoid inline comments that repeat the code.
