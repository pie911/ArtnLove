/* main.js — ArtnLove client-side logic
   - fetches artworks from /api/v1/artworks
   - posts artwork metadata when upload form is submitted
   - handles authentication and user management
   - infinite-scroll behavior
   - dark mode toggle
*/

const galleryEl = document.getElementById('gallery');
const loadingEl = document.getElementById('loading');

let page = 0;
const pageSize = 12;
let loading = false;

// API Base URL
const API_BASE = 'http://localhost:5069/api/v1';

// Auth state
let currentUser = null;
let authToken = localStorage.getItem('authToken');

async function fetchArtworks() {
  if (loading) return;
  loading = true;
  if (loadingEl) loadingEl.hidden = false;
  try {
    const res = await fetch(`${API_BASE}/artworks?limit=${pageSize}&offset=${page * pageSize}`);
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
    col.className = 'col-sm-6 col-md-4 col-lg-3 mb-4';
    const card = document.createElement('div');
    card.className = 'card artwork-card h-100';
    const img = document.createElement('img');
    img.className = 'card-img-top';
    img.alt = a.title || 'Artwork';
    img.src = a.mediaUrls && a.mediaUrls[0] ? a.mediaUrls[0] : '/client/css/placeholder.png';
    img.loading = 'lazy';
    const body = document.createElement('div');
    body.className = 'card-body d-flex flex-column';
    body.innerHTML = `
      <h5 class="card-title">${escapeHtml(a.title || 'Untitled')}</h5>
      <p class="card-text text-muted text-truncate">${escapeHtml(a.description || '')}</p>
      <div class="mt-auto">
        <a class="btn btn-primary btn-sm" href="/client/artwork.html?id=${a.id}">
          <i class="bi bi-eye me-1"></i>View
        </a>
        <button class="btn btn-outline-secondary btn-sm ms-1" onclick="likeArtwork('${a.id}')">
          <i class="bi bi-heart me-1"></i>Like
        </button>
      </div>
    `;
    card.appendChild(img);
    card.appendChild(body);
    col.appendChild(card);
    galleryEl.appendChild(col);
  }
}

function escapeHtml(s) {
  if (!s) return '';
  return s.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'<','>':'>','"':'"',"'":'&#39;'})[c]);
}

// Infinite scroll: near bottom -> fetch next page
window.addEventListener('scroll', () => {
  if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 400) {
    fetchArtworks();
  }
});

// Auth functions
function showLoginModal() {
  const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
  loginModal.show();
  document.getElementById('registerModal').style.display = 'none';
}

function showRegisterModal() {
  const registerModal = new bootstrap.Modal(document.getElementById('registerModal'));
  registerModal.show();
  document.getElementById('loginModal').style.display = 'none';
}

function switchToRegister() {
  bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
  showRegisterModal();
}

function switchToLogin() {
  bootstrap.Modal.getInstance(document.getElementById('registerModal')).hide();
  showLoginModal();
}

// Login form handler
document.addEventListener('DOMContentLoaded', function() {
  const loginForm = document.getElementById('loginForm');
  if (loginForm) {
    loginForm.addEventListener('submit', async function(e) {
      e.preventDefault();

      const email = document.getElementById('loginEmail').value;
      const password = document.getElementById('loginPassword').value;

      try {
        const response = await fetch(`${API_BASE}/auth/login`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (response.ok) {
          authToken = data.access_token;
          localStorage.setItem('authToken', authToken);
          currentUser = data.user;
          updateUI();
          bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
          showMessage('Login successful!', 'success');
        } else {
          showMessage(data.message || 'Login failed', 'error');
        }
      } catch (error) {
        console.error('Login error:', error);
        showMessage('Login failed. Please try again.', 'error');
      }
    });
  }

  // Register form handler
  const registerForm = document.getElementById('registerForm');
  if (registerForm) {
    registerForm.addEventListener('submit', async function(e) {
      e.preventDefault();

      const email = document.getElementById('registerEmail').value;
      const password = document.getElementById('registerPassword').value;
      const displayName = document.getElementById('registerDisplayName').value;
      const role = document.getElementById('registerRole').value;
      const bio = document.getElementById('registerBio').value;
      const location = document.getElementById('registerLocation').value;
      const website = document.getElementById('registerWebsite').value;

      try {
        const response = await fetch(`${API_BASE}/auth/register`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            email,
            password,
            displayName,
            role,
            bio: bio || null,
            location: location || null,
            website: website || null
          })
        });

        const data = await response.json();

        if (response.ok) {
          bootstrap.Modal.getInstance(document.getElementById('registerModal')).hide();
          showMessage('Registration successful! Please check your email to confirm your account.', 'success');
          registerForm.reset();
        } else {
          showMessage(data.message || 'Registration failed', 'error');
        }
      } catch (error) {
        console.error('Registration error:', error);
        showMessage('Registration failed. Please try again.', 'error');
      }
    });
  }
});

// Check auth status
async function checkAuthStatus() {
  if (!authToken) return;

  try {
    const response = await fetch(`${API_BASE}/auth/me`, {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    if (response.ok) {
      const data = await response.json();
      currentUser = data.user;
      updateUI();
    } else {
      localStorage.removeItem('authToken');
      authToken = null;
    }
  } catch (error) {
    console.error('Auth check error:', error);
  }
}

// Update UI based on auth state
function updateUI() {
  const authButtons = document.getElementById('authButtons');
  const userMenu = document.getElementById('userMenu');
  const userAvatar = document.getElementById('userAvatar');
  const userDisplayName = document.getElementById('userDisplayName');

  if (currentUser && authButtons && userMenu) {
    authButtons.style.display = 'none';
    userMenu.style.display = 'block';
    if (userDisplayName) userDisplayName.textContent = currentUser.display_name || currentUser.email;
    if (userAvatar) userAvatar.textContent = (currentUser.display_name || currentUser.email).charAt(0).toUpperCase();
  } else if (authButtons && userMenu) {
    authButtons.style.display = 'block';
    userMenu.style.display = 'none';
  }
}

// Logout
function logout() {
  localStorage.removeItem('authToken');
  authToken = null;
  currentUser = null;
  updateUI();
  showMessage('Logged out successfully', 'success');
  window.location.href = '/client/index.html';
}

// Check auth and redirect
function checkAuthAndRedirect() {
  if (!currentUser) {
    showLoginModal();
  } else {
    window.location.href = '/client/upload.html';
  }
}

// Artwork actions
function viewArtwork(id) {
  console.log('View artwork:', id);
  // Could implement modal or redirect
}

function likeArtwork(id) {
  if (!currentUser) {
    showLoginModal();
    return;
  }
  console.log('Like artwork:', id);
  // Could implement like functionality
}

// Message display
function showMessage(message, type = 'info') {
  // Simple alert for now, could be replaced with toast notifications
  alert(message);
}

// Dark mode toggle
function toggleDarkMode() {
  const body = document.body;
  const toggleBtn = document.querySelector('.dark-mode-toggle i');

  if (body.classList.contains('dark-mode')) {
    body.classList.remove('dark-mode');
    if (toggleBtn) {
      toggleBtn.classList.remove('bi-sun');
      toggleBtn.classList.add('bi-moon');
    }
    localStorage.setItem('darkMode', 'false');
  } else {
    body.classList.add('dark-mode');
    if (toggleBtn) {
      toggleBtn.classList.remove('bi-moon');
      toggleBtn.classList.add('bi-sun');
    }
    localStorage.setItem('darkMode', 'true');
  }
}

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
      resEl.innerHTML = `<div class="alert alert-warning"><i class="bi bi-exclamation-triangle me-2"></i>Please select a file.</div>`;
      return;
    }

    try {
      // Generate a safe path for upload
      const ext = file.name.split('.').pop();
      const filename = `${Date.now()}-${Math.random().toString(36).slice(2,8)}.${ext}`;
      const path = `uploads/${filename}`;

      // Request signed URL from server
      const presignResp = await fetch(`${API_BASE}/storage/presign`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ bucket: bucket, path: path, expiresInSeconds: 3600, contentType: file.type, contentLength: file.size })
      });
      let presignBody = null;
      if (!presignResp.ok) {
        const txt = await presignResp.text();
        throw new Error('Failed to obtain signed URL: ' + txt);
      } else {
        // Try parse JSON, otherwise keep raw text
        const txt = await presignResp.text();
        try { presignBody = JSON.parse(txt); } catch { presignBody = txt; }
      }
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

      // Construct a public-accessible URL for the stored object (depends on bucket policy).
      // Prefer Supabase project URL from config when available; otherwise fallback to current origin.
      const base = (window.SUPABASE_URL && window.SUPABASE_URL.length) ? window.SUPABASE_URL : window.location.origin;
      const publicUrl = `${base.replace(/\/$/, '')}/storage/v1/object/public/${bucket}/${encodeURIComponent(path)}`;

      // Create artwork metadata record (pointing to stored path)
      const createResp = await fetch(`${API_BASE}/artworks`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, description, mediaUrls: [publicUrl] })
      });
      if (!createResp.ok) throw new Error('Failed to create artwork record');
      const created = await createResp.json();

      resEl.innerHTML = `<div class="alert alert-success"><i class="bi bi-check-circle me-2"></i>Artwork uploaded successfully! <a href="/client/artwork.html?id=${created.id}" class="alert-link">View it here</a></div>`;
      uploadForm.reset();
    } catch (err) {
      console.error(err);
      resEl.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle me-2"></i>${err.message || 'Error during upload'}</div>`;
    }
  });
}

// Initialize when page loads
async function loadConfig() {
  try {
    const res = await fetch(`${API_BASE}/config`);
    if (!res.ok) return;
    const cfg = await res.json();
    if (cfg?.supabaseUrl) window.SUPABASE_URL = cfg.supabaseUrl;
  } catch (e) {
    console.warn('Could not load config', e);
  }
}

document.addEventListener('DOMContentLoaded', async () => {
  await loadConfig();

  // Initialize dark mode
  const darkMode = localStorage.getItem('darkMode') === 'true';
  const toggleBtn = document.querySelector('.dark-mode-toggle i');

  if (darkMode) {
    document.body.classList.add('dark-mode');
    if (toggleBtn) {
      toggleBtn.classList.remove('bi-moon');
      toggleBtn.classList.add('bi-sun');
    }
  }

  if (authToken) {
    await checkAuthStatus();
  }

  // fetch initial page on the gallery
  if (galleryEl) fetchArtworks();
});
