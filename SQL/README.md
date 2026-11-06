# SQL compatibility files

The canonical database schema definitions are now maintained under
[`src/database/`](../src/database/). This folder is retained temporarily for
legacy SQL files and compatibility with existing database tooling.

New schema changes should be made in:

- [`src/database/mysql/`](../src/database/mysql/)
- [`src/database/postgres/`](../src/database/postgres/)

Test seed data is maintained separately under:

- [`src/database/seeds/mysql/test/`](../src/database/seeds/mysql/test/)
- [`src/database/seeds/postgres/test/`](../src/database/seeds/postgres/test/)

Do not place production database backups or real user data in this repository.
Backups should be stored externally with appropriate retention and access
controls.
