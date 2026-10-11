# Vendored frontend dependencies

## SheetJS CE (`xlsx`)

`xlsx-0.20.3.tgz` is the official SheetJS Community Edition tarball from
`https://cdn.sheetjs.com/xlsx-0.20.3/xlsx-0.20.3.tgz`.

It is intentionally committed and referenced through `file:vendor/xlsx-0.20.3.tgz`
in `package.json`. This makes `npm ci` independent of the SheetJS CDN and keeps
the finance Excel export reproducible.

SHA-256: `8dc73fc3b00203e72d176e85b50938627c7b086e607c682e8d3c22c02bb99fe8`
