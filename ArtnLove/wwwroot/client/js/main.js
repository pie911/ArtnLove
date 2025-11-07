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
    const payload = {
      title: fd.get('title'),
      description: fd.get('description')
      // In production, you would obtain a signed upload URL and upload file directly to Supabase Storage.
    };

    const resEl = document.getElementById('result');
    try {
      const res = await fetch('/api/v1/artworks', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (!res.ok) throw new Error('Create failed');
      const data = await res.json();
      resEl.innerHTML = `<div class="alert alert-success">Created artwork (id: ${data.id})</div>`;
    } catch (err) {
      console.error(err);
      resEl.innerHTML = `<div class="alert alert-danger">Error creating artwork</div>`;
    }
  });
}

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
  // fetch initial page on the gallery
  if (galleryEl) fetchArtworks();
});
