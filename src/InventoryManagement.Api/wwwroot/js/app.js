const state = { token: sessionStorage.getItem('inventoryToken'), user: null, page: 1, totalPages: 1, search: '', lowStock: false, products: [] };
const $ = (selector) => document.querySelector(selector);
const money = new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });
const number = new Intl.NumberFormat('es-CL');

function decodeToken(token) {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(decodeURIComponent(atob(payload).split('').map(c => `%${c.charCodeAt(0).toString(16).padStart(2, '0')}`).join('')));
  } catch { return null; }
}

async function api(path, options = {}) {
  const headers = { ...(options.headers || {}), Authorization: `Bearer ${state.token}` };
  if (options.body && !(options.body instanceof FormData)) headers['Content-Type'] = 'application/json';
  const response = await fetch(path, { ...options, headers });
  if (response.status === 401) { logout(); throw new Error('Tu sesión expiró.'); }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    throw new Error(problem.title || 'No fue posible completar la operación.');
  }
  return response;
}

function setLoading(visible) { $('#loading').hidden = !visible; }
function toast(message) { const el = $('#toast'); el.textContent = message; el.hidden = false; setTimeout(() => el.hidden = true, 3500); }

function showApp() {
  state.user = decodeToken(state.token);
  if (!state.user || state.user.exp * 1000 <= Date.now()) return logout();
  $('#login-view').hidden = true;
  $('#app-view').hidden = false;
  const name = state.user.name || state.user.email || 'Usuario';
  $('#user-name').textContent = name;
  $('#user-role').textContent = state.user.role || 'Operador';
  $('#user-initial').textContent = name[0].toUpperCase();
  $('#export-button').hidden = state.user.role !== 'Admin';
  $('#new-product-button').hidden = state.user.role !== 'Admin';
  loadDashboard();
}

function logout() {
  sessionStorage.removeItem('inventoryToken');
  state.token = null;
  $('#app-view').hidden = true;
  $('#login-view').hidden = false;
  $('#password').value = '';
}

$('#login-form').addEventListener('submit', async event => {
  event.preventDefault();
  const error = $('#login-error'); error.hidden = true;
  const button = event.submitter; button.disabled = true; button.firstElementChild.textContent = 'Ingresando…';
  try {
    const response = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: $('#email').value, password: $('#password').value }) });
    if (!response.ok) throw new Error('Correo o contraseña incorrectos.');
    const data = await response.json();
    state.token = data.accessToken; sessionStorage.setItem('inventoryToken', state.token); showApp();
  } catch (err) { error.textContent = err.message; error.hidden = false; }
  finally { button.disabled = false; button.firstElementChild.textContent = 'Ingresar al panel'; }
});

async function loadDashboard() {
  setLoading(true);
  try {
    const data = await (await api('/api/dashboard')).json();
    $('#metrics').innerHTML = [
      ['Valor del inventario', money.format(data.inventoryValue), `${number.format(data.totalStockUnits)} unidades`, true],
      ['Productos activos', number.format(data.productCount), 'Catálogo registrado'],
      ['Stock crítico', number.format(data.lowStockCount), 'Requieren atención'],
      ['Órdenes pendientes', number.format(data.pendingPurchaseOrders), `${number.format(data.receivedPurchaseOrdersThisMonth)} recibidas este mes`]
    ].map(([label, value, note, featured]) => `<article class="metric ${featured ? 'featured' : ''}">${featured ? '<i class="accent"></i>' : ''}<span class="label">${label}</span><strong>${value}</strong><small>${note}</small></article>`).join('');
    $('#low-stock-list').innerHTML = data.lowStockProducts.length ? data.lowStockProducts.map(p => `<div class="stock-row"><div class="product-name"><span class="product-symbol">${escapeHtml(p.name[0])}</span><div><strong>${escapeHtml(p.name)}</strong><small>${escapeHtml(p.sku)} · ${escapeHtml(p.categoryName || 'Sin categoría')}</small></div></div><div class="stock-value"><strong>${p.currentStock} un.</strong><small>Mínimo ${p.minimumStock}</small></div></div>`).join('') : '<p class="empty">No hay productos con stock crítico.</p>';
    $('#movement-list').innerHTML = data.recentMovements.length ? data.recentMovements.map(m => `<div class="timeline-item"><strong>${m.quantity > 0 ? '+' : ''}${m.quantity} · ${escapeHtml(m.productName)}</strong><p>${escapeHtml(m.reason)}</p><time>${new Date(m.createdAtUtc).toLocaleString('es-CL')}</time></div>`).join('') : '<p class="empty">Todavía no hay movimientos.</p>';
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

async function loadProducts() {
  setLoading(true);
  try {
    const params = new URLSearchParams({ page: state.page, pageSize: 10 });
    if (state.search) params.set('search', state.search);
    if (state.lowStock) params.set('lowStock', 'true');
    const data = await (await api(`/api/products?${params}`)).json();
    state.totalPages = Math.max(1, data.totalPages); state.products = data.items;
    $('#products-body').innerHTML = data.items.length ? data.items.map(p => `<tr><td><div class="product-name"><span class="product-symbol">${escapeHtml(p.name[0])}</span><div><strong>${escapeHtml(p.name)}</strong><small>${escapeHtml(p.sku)}</small></div></div></td><td>${escapeHtml(p.categoryName || 'Sin categoría')}</td><td class="price">${money.format(p.price)}</td><td>${number.format(p.currentStock)} / mín. ${number.format(p.minimumStock)}</td><td><span class="badge ${p.isLowStock ? 'danger' : ''}">${p.isLowStock ? 'Stock bajo' : 'Disponible'}</span></td><td><div class="row-actions"><button class="stock-button" data-stock-id="${p.id}">Ajustar stock</button></div></td></tr>`).join('') : '<tr><td colspan="6" class="empty">No encontramos productos para estos filtros.</td></tr>';
    $('#product-count').textContent = `${number.format(data.totalCount)} producto${data.totalCount === 1 ? '' : 's'}`;
    $('#page-number').textContent = `${data.page} de ${state.totalPages}`;
    $('#previous-page').disabled = state.page <= 1; $('#next-page').disabled = state.page >= state.totalPages;
    document.querySelectorAll('[data-stock-id]').forEach(button => button.addEventListener('click', () => { const product = state.products.find(x => x.id === button.dataset.stockId); if (product) openStockDialog(product.id, product.name); }));
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

function changeView(view) {
  document.querySelectorAll('.nav-item[data-view]').forEach(item => item.classList.toggle('active', item.dataset.view === view));
  $('#dashboard-view').hidden = view !== 'dashboard'; $('#products-view').hidden = view !== 'products';
  $('#page-eyebrow').textContent = view === 'dashboard' ? 'PANEL GENERAL' : 'CATÁLOGO';
  $('#page-title').textContent = view === 'dashboard' ? 'Resumen de inventario' : 'Productos';
  if (view === 'products') loadProducts(); else loadDashboard();
}

function escapeHtml(value) { const el = document.createElement('span'); el.textContent = value ?? ''; return el.innerHTML; }
async function loadCatalogOptions() {
  const [categories, suppliers] = await Promise.all([api('/api/categories').then(r => r.json()), api('/api/suppliers').then(r => r.json())]);
  $('#product-category').innerHTML = '<option value="">Selecciona una categoría</option>' + categories.map(x => `<option value="${x.id}">${escapeHtml(x.name)}</option>`).join('');
  $('#product-supplier').innerHTML = '<option value="">Sin proveedor</option>' + suppliers.map(x => `<option value="${x.id}">${escapeHtml(x.name)}</option>`).join('');
}
async function openProductDialog() { try { await loadCatalogOptions(); $('#product-form').reset(); $('#product-dialog').showModal(); } catch (err) { toast(err.message); } }
function openStockDialog(id, name) { const form = $('#stock-form'); form.reset(); form.elements.productId.value = id; $('#stock-product-name').textContent = name; $('#stock-dialog').showModal(); }
function closeDialog(id) { document.getElementById(id).close(); }
function formError(form, message = '') { const error = form.querySelector('[data-form-error]'); error.textContent = message; error.hidden = !message; }

$('#product-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const data = new FormData(form);
    await api('/api/products', { method: 'POST', body: JSON.stringify({ sku:data.get('sku'), name:data.get('name'), price:Number(data.get('price')), minimumStock:Number(data.get('minimumStock')), description:data.get('description') || null, categoryId:data.get('categoryId'), supplierId:data.get('supplierId') || null }) });
    closeDialog('product-dialog'); state.page = 1; await loadProducts(); toast('Producto creado correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});
$('#stock-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const data = new FormData(form); const quantity = Number(data.get('quantity')); if (!Number.isInteger(quantity) || quantity === 0) throw new Error('La cantidad debe ser un número entero distinto de cero.');
    await api(`/api/products/${data.get('productId')}/stock-movements`, { method:'POST', body:JSON.stringify({ quantity, reason:data.get('reason') }) });
    closeDialog('stock-dialog'); await loadProducts(); toast('Movimiento registrado correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});
document.querySelectorAll('[data-view]').forEach(button => button.addEventListener('click', () => changeView(button.dataset.view)));
document.querySelectorAll('[data-go-products]').forEach(button => button.addEventListener('click', () => changeView('products')));
$('#logout-button').addEventListener('click', logout);
$('#refresh-button').addEventListener('click', () => $('#products-view').hidden ? loadDashboard() : loadProducts());
let searchTimer; $('#product-search').addEventListener('input', event => { clearTimeout(searchTimer); searchTimer = setTimeout(() => { state.search = event.target.value.trim(); state.page = 1; loadProducts(); }, 350); });
$('#low-stock-filter').addEventListener('change', event => { state.lowStock = event.target.checked; state.page = 1; loadProducts(); });
$('#previous-page').addEventListener('click', () => { if (state.page > 1) { state.page--; loadProducts(); } });
$('#next-page').addEventListener('click', () => { if (state.page < state.totalPages) { state.page++; loadProducts(); } });
$('#export-button').addEventListener('click', async () => { try { const params = new URLSearchParams(); if (state.search) params.set('search', state.search); if (state.lowStock) params.set('lowStock', 'true'); const response = await api(`/api/reports/inventory.csv?${params}`); const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'inventario.csv'; anchor.click(); URL.revokeObjectURL(url); } catch (err) { toast(err.message); } });
$('#new-product-button').addEventListener('click', openProductDialog);
document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => closeDialog(button.dataset.closeDialog)));

if (state.token) showApp();
