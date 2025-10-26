# Browser Service - Advanced Features

This document provides examples and usage information for the advanced features in the Browser Service.

## Features Overview

The Browser Service now supports four advanced automation features:

1. **Screenshot Capture** - Capture full page or element-specific screenshots
2. **Content Extraction** - Extract text, links, and images from web pages
3. **Form Interaction** - Fill form fields and submit forms programmatically
4. **PDF Generation** - Generate PDFs from web pages with custom settings

## API Endpoints

### 1. Screenshot Capture

**Endpoint:** `POST /browse/screenshot`

Captures a screenshot of a web page or specific element.

#### Request Example (Full Page):
```json
{
  "url": "https://example.com",
  "browserType": "chromium",
  "format": "png",
  "fullPage": true
}
```

#### Request Example (Element):
```json
{
  "url": "https://example.com",
  "browserType": "chromium",
  "format": "jpeg",
  "quality": 80,
  "selector": "#main-content",
  "fullPage": false
}
```

#### Response:
```json
{
  "imageData": "iVBORw0KGgoAAAANSUhEUgAA...",
  "format": "png",
  "url": "https://example.com/",
  "browserType": "chromium",
  "durationMs": 1245
}
```

#### Parameters:
- `url` (required): The URL to navigate to
- `browserType` (optional): "chromium" or "firefox" (default: "chromium")
- `format` (optional): "png" or "jpeg" (default: "png")
- `quality` (optional): 0-100 for JPEG quality
- `selector` (optional): CSS selector for element screenshot
- `fullPage` (optional): Capture full scrollable page (default: false)
- `timeoutMs` (optional): Timeout in milliseconds

---

### 2. Content Extraction

**Endpoint:** `POST /browse/extract`

Extracts text content, links, and images from a web page.

#### Request Example:
```json
{
  "url": "https://example.com",
  "browserType": "chromium",
  "extractText": true,
  "extractLinks": true,
  "extractImages": true
}
```

#### Response:
```json
{
  "text": "Example Domain\nThis domain is for use in illustrative examples...",
  "links": [
    "https://www.iana.org/domains/example"
  ],
  "images": [
    "https://example.com/logo.png"
  ],
  "url": "https://example.com/",
  "browserType": "chromium",
  "durationMs": 892
}
```

#### Parameters:
- `url` (required): The URL to navigate to
- `browserType` (optional): "chromium" or "firefox" (default: "chromium")
- `extractText` (optional): Extract body text (default: true)
- `extractLinks` (optional): Extract all links (default: true)
- `extractImages` (optional): Extract all images (default: true)
- `timeoutMs` (optional): Timeout in milliseconds

---

### 3. Form Interaction

**Endpoint:** `POST /browse/interact`

Fills form fields and optionally submits the form.

#### Request Example:
```json
{
  "url": "https://example.com/form",
  "browserType": "chromium",
  "fields": {
    "#username": "testuser",
    "#password": "testpass",
    "#email": "test@example.com"
  },
  "submitButtonSelector": "#submit-btn",
  "waitForNavigation": true
}
```

#### Response:
```json
{
  "success": true,
  "url": "https://example.com/success",
  "title": "Success Page",
  "content": "<html>...</html>",
  "browserType": "chromium",
  "durationMs": 1523
}
```

#### Parameters:
- `url` (required): The URL to navigate to
- `browserType` (optional): "chromium" or "firefox" (default: "chromium")
- `fields` (required): Dictionary of CSS selectors to values
- `submitButtonSelector` (optional): Selector for submit button
- `waitForNavigation` (optional): Wait for page load after submit (default: true)
- `timeoutMs` (optional): Timeout in milliseconds

---

### 4. PDF Generation

**Endpoint:** `POST /browse/pdf`

Generates a PDF from a web page.

#### Request Example (A4 with margins):
```json
{
  "url": "https://example.com",
  "format": "A4",
  "marginTop": "20mm",
  "marginRight": "15mm",
  "marginBottom": "20mm",
  "marginLeft": "15mm",
  "printBackground": true
}
```

#### Request Example (Custom dimensions):
```json
{
  "url": "https://example.com",
  "width": "210mm",
  "height": "297mm",
  "printBackground": true
}
```

#### Response:
```json
{
  "pdfData": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PC9U...",
  "url": "https://example.com/",
  "durationMs": 1876,
  "sizeBytes": 45678
}
```

#### Parameters:
- `url` (required): The URL to navigate to
- `format` (optional): Page format (e.g., "A4", "Letter")
- `width` (optional): Custom page width (e.g., "210mm")
- `height` (optional): Custom page height (e.g., "297mm")
- `marginTop` (optional): Top margin (e.g., "10mm")
- `marginRight` (optional): Right margin
- `marginBottom` (optional): Bottom margin
- `marginLeft` (optional): Left margin
- `printBackground` (optional): Print background graphics (default: true)
- `timeoutMs` (optional): Timeout in milliseconds

**Note:** PDF generation only works with Chromium browser.

---

## Usage Examples

### Example 1: Taking Screenshots for Different Viewports

```bash
# Mobile viewport
curl -X POST http://localhost:5000/browse/screenshot \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "browserType": "chromium",
    "format": "png",
    "fullPage": true
  }'
```

### Example 2: Extracting All Links from a Page

```bash
curl -X POST http://localhost:5000/browse/extract \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "extractText": false,
    "extractLinks": true,
    "extractImages": false
  }'
```

### Example 3: Automated Form Submission

```bash
curl -X POST http://localhost:5000/browse/interact \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com/login",
    "fields": {
      "#username": "testuser",
      "#password": "secret123"
    },
    "submitButtonSelector": "button[type=submit]",
    "waitForNavigation": true
  }'
```

### Example 4: Generating PDF Reports

```bash
curl -X POST http://localhost:5000/browse/pdf \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com/report",
    "format": "A4",
    "marginTop": "20mm",
    "marginBottom": "20mm",
    "printBackground": true
  }' > report.pdf
```

---

## Error Handling

All endpoints return standard HTTP status codes:

- **200 OK**: Successful operation
- **400 Bad Request**: Validation error (invalid URL, browser type, etc.)
- **408 Request Timeout**: Operation timed out
- **500 Internal Server Error**: Unexpected error during processing

### Example Error Response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Url": ["URL must be a valid HTTP or HTTPS URL"],
    "Format": ["Format must be 'png' or 'jpeg'"]
  }
}
```

---

## Performance Considerations

1. **Screenshot Capture**
   - Full page screenshots take longer than viewport screenshots
   - JPEG format produces smaller files but with quality loss
   - Element screenshots are faster than full page captures

2. **Content Extraction**
   - Disable unused extraction types for better performance
   - Large pages with many elements may take longer to process

3. **Form Interaction**
   - Form submission with navigation may take several seconds
   - Consider setting appropriate timeout values for slow-loading pages

4. **PDF Generation**
   - PDF generation is CPU-intensive
   - Large or complex pages will take longer to render
   - Only Chromium browser supports PDF generation

---

## Browser Support

| Feature | Chromium | Firefox |
|---------|----------|---------|
| Screenshot | ✅ | ✅ |
| Content Extraction | ✅ | ✅ |
| Form Interaction | ✅ | ✅ |
| PDF Generation | ✅ | ❌ |

---

## Configuration

The Browser Service can be configured via `appsettings.json`:

```json
{
  "Browser": {
    "Headless": true,
    "Timeout": 30000,
    "MaxPoolSize": 5
  }
}
```

- `Headless`: Run browsers in headless mode (default: true)
- `Timeout`: Default timeout for operations in milliseconds (default: 30000)
- `MaxPoolSize`: Maximum number of concurrent browser instances (default: 5)
