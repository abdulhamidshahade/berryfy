import fs from 'fs';
import path from 'path';

export function removeLocalCatalogImage(imageUrl: string, kind: 'product' | 'category'): void {
  const prefix = `/uploads/${kind}/`;
  if (!imageUrl.startsWith(prefix)) return;
  const filename = imageUrl.slice(prefix.length);
  // Only a single image filename is accepted; directories, URLs and traversal are excluded.
  if (!/^[a-zA-Z0-9_-]+\.(?:jpg|jpeg|png|gif|webp)$/i.test(filename)) return;
  const imagePath = path.join(process.cwd(), 'public', 'uploads', kind, filename);
  if (fs.existsSync(imagePath)) fs.unlinkSync(imagePath);
}
