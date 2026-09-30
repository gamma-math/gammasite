# Backend API manual tests

These commands assume the site is running on `https://localhost:5001` with the local Docker MySQL database.

## Login as test admin

```powershell
$cookie = Join-Path $env:TEMP 'gammasite-admin-cookies.txt'
$loginHtml = Join-Path $env:TEMP 'gammasite-login.html'
curl.exe -k -s -c $cookie https://localhost:5001/Identity/Account/Login -o $loginHtml
$token = Select-String -Path $loginHtml -Pattern 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1
curl.exe -k -s -i -b $cookie -c $cookie -X POST https://localhost:5001/Identity/Account/Login --data-urlencode "Input.Email=admin.test@gamma.local" --data-urlencode "Input.Password=Admin123!Test" --data-urlencode "__RequestVerificationToken=$token" --data-urlencode "Input.RememberMe=false"
```

## Create content

```powershell
$body = @{
    title = 'API test event'
    slug = "api-test-event-$(Get-Date -Format yyyyMMddHHmmss)"
    summary = 'Oprettet via API test'
    body = 'Backend verifikation'
    pictureUrl = 'https://example.com/event.jpg'
    tags = 'event,test'
    type = 'EVENT'
    status = 'PUBLISHED'
    startDate = '2026-10-01T18:00:00Z'
    endDate = '2026-10-01T21:00:00Z'
    location = 'Kobenhavn'
    links = @(@{ label = 'Facebook'; url = 'https://example.com/facebook'; type = 'FACEBOOK'; sortOrder = 1 })
} | ConvertTo-Json -Depth 5
$content = $body | curl.exe -k -s -b $cookie -H "Content-Type: application/json" -X POST https://localhost:5001/api/content -d "@-" | ConvertFrom-Json
$content
```

## Fetch content

```powershell
curl.exe -k -s "https://localhost:5001/api/content/$($content.id)"
curl.exe -k -s "https://localhost:5001/api/content/slug/$($content.slug)"
```

## Register for event

```powershell
'{"registrationType":"ATTENDEE","responseText":"Kommer via API test"}' | curl.exe -k -s -b $cookie -H "Content-Type: application/json" -X POST "https://localhost:5001/api/content/$($content.id)/registrations" -d "@-"
```

## Fetch registrations as admin

```powershell
curl.exe -k -s -b $cookie "https://localhost:5001/api/content/$($content.id)/registrations"
```

## Create and preview email template

```powershell
$templateBody = @{
    name = 'API event invitation'
    subject = 'Hej {{UserName}} - {{EventTitle}}'
    preheader = 'Tilmeld dig {{EventTitle}}'
    htmlBody = '<h1>{{EventTitle}}</h1><p>{{ContentBlocks}}</p><a href="{{EventRegisterUrl}}">Tilmeld</a>'
    textBody = '{{EventTitle}}: {{EventRegisterUrl}}'
    templateType = 'EVENT'
    isActive = $true
} | ConvertTo-Json
$template = $templateBody | curl.exe -k -s -b $cookie -H "Content-Type: application/json" -X POST https://localhost:5001/api/email-templates -d "@-" | ConvertFrom-Json

$previewBody = @{
    values = @{
        UserName = 'Ada'
        EventTitle = 'API test event'
        EventRegisterUrl = 'https://gam-ma.dk/events/api-test-event'
        ContentBlocks = 'Velkommen til backend-testen'
    }
} | ConvertTo-Json -Depth 4
$previewBody | curl.exe -k -s -b $cookie -H "Content-Type: application/json" -X POST "https://localhost:5001/api/email-templates/$($template.id)/preview" -d "@-"
```
