/* main.js — minimal client-side logic for ArtnLove demo
   - fetches artworks from /api/v1/artworks
   - posts artwork metadata when upload form is submitted
   - simple infinite-scroll behavior
*/

const galleryEl = document.getElementById('gallery');
const loadingEl = document.getElementById('loading');

let page = 0;
const pageSize = 12;
let loading = false;

async function fetchArtworks() {
  if (loading) return;
  loading = true;
  if (loadingEl) loadingEl.hidden = false;
  try {
    const res = await fetch(`/api/v1/artworks?limit=${pageSize}&offset=${page * pageSize}`);
    if (!res.ok) throw new Error('Failed to load');
    const items = await res.json();
    renderArtworks(items);
    page += 1;
  } catch (e) {
    console.error(e);
  } finally {
    loading = false;
    if (loadingEl) loadingEl.hidden = true;
  }
}

function renderArtworks(items) {
  if (!galleryEl) return;
  for (const a of items) {
    const col = document.createElement('div');
    col.className = 'col-sm-6 col-md-4 col-lg-3';
    const card = document.createElement('div');
    card.className = 'card h-100';
    const img = document.createElement('img');
    img.className = 'card-img-top';
    img.alt = a.title || 'Artwork';
    img.src = a.mediaUrls && a.mediaUrls[0] ? a.mediaUrls[0] : '/client/css/placeholder.png';
    const body = document.createElement('div');
    body.className = 'card-body';
    body.innerHTML = `<h5 class="card-title">${escapeHtml(a.title || 'Untitled')}</h5>
                      <p class="card-text text-truncate">${escapeHtml(a.description || '')}</p>
                      <a class="btn btn-sm btn-outline-primary" href="/client/artwork.html?id=${a.id}">View</a>`;
    card.appendChild(img);
    card.appendChild(body);
    col.appendChild(card);
    galleryEl.appendChild(col);
  }
}

function escapeHtml(s) {
  if (!s) return '';
  return s.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":"&#39;"})[c]);
}

// Infinite scroll: near bottom -> fetch next page
window.addEventListener('scroll', () => {
  if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 400) {
    fetchArtworks();
  }
});

// If upload form exists, wire submit
const uploadForm = document.getElementById('uploadForm');
if (uploadForm) {
  uploadForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fd = new FormData(uploadForm);
    const file = document.getElementById('imageInput').files[0];
    const bucket = fd.get('bucket') || 'public';
    const title = fd.get('title');
    const description = fd.get('description');
    const resEl = document.getElementById('result');

    if (!file) {
      resEl.innerHTML = `<div class="alert alert-warning">Please select a file.</div>`;
      return;
    }

    try {
      // Generate a safe path for upload
      const ext = file.name.split('.').pop();
      const filename = `${Date.now()}-${Math.random().toString(36).slice(2,8)}.${ext}`;
      const path = `uploads/${filename}`;

      // Request signed URL from server
      const presignResp = await fetch('/api/v1/storage/presign', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ bucket: bucket, path: path, expiresInSeconds: 3600, contentType: file.type, contentLength: file.size })
      });
      if (!presignResp.ok) throw new Error('Failed to obtain signed URL');

      const presignBody = await presignResp.json();
      // Supabase sometimes returns a raw URL string or a JSON with signedURL — try common shapes
      let signedUrl = null;
      if (typeof presignBody === 'string') signedUrl = presignBody;
      else if (presignBody?.signedURL) signedUrl = presignBody.signedURL;
      else if (presignBody?.signed) signedUrl = presignBody.signed;
      else {
        // Try object with url property
        signedUrl = Object.values(presignBody)[0];
      }

      if (!signedUrl) throw new Error('Signed URL not returned from server');

      // Upload file directly to Storage using PUT
      const uploadResp = await fetch(signedUrl, {
        method: 'PUT',
        headers: { 'Content-Type': file.type },
        body: file
      });
      if (!uploadResp.ok) throw new Error('Upload failed');

      // Construct a public-accessible URL for the stored object (depends on bucket policy). Adjust if bucket is private.
      const publicUrl = `${window.location.origin}/storage/v1/object/public/${bucket}/${encodeURIComponent(path)}`;

      // Create artwork metadata record (pointing to stored path)
      const createResp = await fetch('/api/v1/artworks', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, description, mediaUrls: [publicUrl] })
      });
      if (!createResp.ok) throw new Error('Failed to create artwork record');
      const created = await createResp.json();

      resEl.innerHTML = `<div class="alert alert-success">Uploaded and created artwork (id: ${created.id})</div>`;
    } catch (err) {
      console.error(err);
      resEl.innerHTML = `<div class="alert alert-danger">${err.message || 'Error during upload'}</div>`;
    }
  });
}

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
  // fetch initial page on the gallery
  if (galleryEl) fetchArtworks();
});
