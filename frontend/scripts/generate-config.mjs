import fs from 'node:fs';
import path from 'node:path';

const apiBaseUrl = (process.env.API_BASE_URL || '').trim().replace(/\/+$/, '');
const distConfigPath = path.resolve(import.meta.dirname, '../dist/frontend/browser/config.js');

if (apiBaseUrl && fs.existsSync(path.dirname(distConfigPath))) {
  const content =
    `// Gerado automaticamente durante o build de produção.\n` +
    `window.__DESANTI_CONFIG__ = { apiBaseUrl: ${JSON.stringify(apiBaseUrl)} };\n`;
  fs.writeFileSync(distConfigPath, content, 'utf8');
  console.log(`[generate-config] config.js atualizado para: ${apiBaseUrl}`);
} else {
  console.log('[generate-config] API_BASE_URL não definida; mantendo config.js padrão.');
}
