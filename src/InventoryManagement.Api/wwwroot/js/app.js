const state = { token: sessionStorage.getItem('inventoryToken'), user: null, page: 1, totalPages: 1, search: '', lowStock: false, products: [], categories: [], suppliers: [], orderProducts: [], orderSuppliers: [], auditPage: 1, auditTotalPages: 1 };
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
  $('#categories-nav').hidden = state.user.role !== 'Admin';
  $('#suppliers-nav').hidden = state.user.role !== 'Admin';
  $('#new-order-button').hidden = state.user.role !== 'Admin';
  $('#audit-nav').hidden = state.user.role !== 'Admin';
  $('#users-nav').hidden = state.user.role !== 'Admin';
  loadNotificationCount();
  changeView('dashboard');
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

const orderStatus = { Pending: 'Pendiente', Received: 'Recibida', Cancelled: 'Cancelada' };

async function loadOrders() {
  setLoading(true);
  try {
    const status = $('#order-status-filter').value;
    const query = status ? `?status=${encodeURIComponent(status)}` : '';
    const orders = await (await api(`/api/purchase-orders${query}`)).json();
    $('#orders-list').innerHTML = orders.length ? orders.map(order => {
      const canReceive = order.status === 'Pending';
      const canCancel = canReceive && state.user.role === 'Admin';
      return `<article class="order-card"><div><div class="order-heading"><strong>${escapeHtml(order.number)}</strong><span class="status-badge ${order.status.toLowerCase()}">${orderStatus[order.status] || escapeHtml(order.status)}</span></div><p>${escapeHtml(order.supplierName)} · ${order.items.length} producto${order.items.length === 1 ? '' : 's'}</p><small>Creada ${new Date(order.createdAtUtc).toLocaleString('es-CL')}</small></div><div class="order-summary"><strong>${money.format(order.total)}</strong><div class="order-actions">${canReceive ? `<button class="order-action primary" data-receive-order="${order.id}">Recibir</button>` : ''}${canCancel ? `<button class="order-action danger" data-cancel-order="${order.id}">Cancelar</button>` : ''}</div></div></article>`;
    }).join('') : '<p class="empty">No hay órdenes para este filtro.</p>';
    document.querySelectorAll('[data-receive-order]').forEach(button => button.addEventListener('click', () => updateOrder(button.dataset.receiveOrder, 'receive', 'Orden recibida e inventario actualizado.')));
    document.querySelectorAll('[data-cancel-order]').forEach(button => button.addEventListener('click', () => updateOrder(button.dataset.cancelOrder, 'cancel', 'Orden cancelada correctamente.')));
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

async function loadNotificationCount() {
  try {
    const notifications = await (await api('/api/notifications?unreadOnly=true')).json();
    const badge = $('#notification-count'); badge.textContent = notifications.length; badge.hidden = notifications.length === 0;
  } catch (err) { toast(err.message); }
}

async function openNotifications() {
  try {
    const notifications = await (await api('/api/notifications')).json();
    $('#notifications-list').innerHTML = notifications.length ? notifications.map(item => `<article class="notification-item ${item.isRead ? '' : 'unread'}"><span class="notification-icon">!</span><div><strong>${escapeHtml(item.productName)}</strong><p>Quedan ${number.format(item.currentStock)} unidades; el mínimo configurado es ${number.format(item.minimumStock)}.</p><time>${new Date(item.createdAtUtc).toLocaleString('es-CL')}</time></div>${item.isRead ? '' : `<button class="mark-read" data-read-notification="${item.id}">Marcar leída</button>`}</article>`).join('') : '<p class="empty">No tienes notificaciones de stock.</p>';
    document.querySelectorAll('[data-read-notification]').forEach(button => button.addEventListener('click', () => markNotificationRead(button.dataset.readNotification)));
    if (!$('#notifications-dialog').open) $('#notifications-dialog').showModal();
  } catch (err) { toast(err.message); }
}

async function markNotificationRead(id) {
  try { await api(`/api/notifications/${id}/read`, { method: 'POST' }); await openNotifications(); await loadNotificationCount(); } catch (err) { toast(err.message); }
}

async function loadAuditLogs() {
  setLoading(true);
  try {
    const params = new URLSearchParams({ page: state.auditPage, pageSize: 20 });
    const method = $('#audit-method-filter').value; if (method) params.set('method', method);
    const data = await (await api(`/api/audit-logs?${params}`)).json();
    state.auditTotalPages = Math.max(1, data.totalPages);
    $('#audit-body').innerHTML = data.items.length ? data.items.map(item => `<tr><td>${new Date(item.createdAtUtc).toLocaleString('es-CL')}</td><td><strong>${escapeHtml(item.userName)}</strong></td><td><span class="method-badge">${escapeHtml(item.httpMethod)}</span> <small>${escapeHtml(item.path)}</small></td><td><span class="result-code ${item.statusCode >= 400 ? 'error' : ''}">${item.statusCode}</span></td><td>${number.format(item.durationMilliseconds)} ms</td><td>${escapeHtml(item.ipAddress || '—')}</td></tr>`).join('') : '<tr><td colspan="6" class="empty">Todavía no hay operaciones para este filtro.</td></tr>';
    $('#audit-count').textContent = `${number.format(data.totalCount)} registro${data.totalCount === 1 ? '' : 's'}`;
    $('#audit-page-number').textContent = `${data.page} de ${state.auditTotalPages}`;
    $('#audit-previous').disabled = state.auditPage <= 1; $('#audit-next').disabled = state.auditPage >= state.auditTotalPages;
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

async function loadUsers() {
  setLoading(true);
  try {
    const users = await (await api('/api/auth/users')).json();
    $('#users-body').innerHTML = users.map(user => { const isCurrent = user.id === state.user.sub; return `<tr><td><div class="product-name"><span class="product-symbol">${escapeHtml(user.displayName[0])}</span><div><strong>${escapeHtml(user.displayName)}${isCurrent ? ' (tú)' : ''}</strong><small>${escapeHtml(user.email)}</small></div></div></td><td><span class="badge">${user.role === 'Admin' ? 'Administrador' : 'Operador'}</span></td><td><span class="badge ${user.isActive ? '' : 'danger'}">${user.isActive ? 'Activo' : 'Inactivo'}</span></td><td>${new Date(user.createdAtUtc).toLocaleDateString('es-CL')}</td><td><div class="access-controls">${isCurrent ? '<small class="muted">Sesión actual</small>' : `<select data-user-role="${user.id}" data-user-is-active="${user.isActive}"><option value="Operator" ${user.role === 'Operator' ? 'selected' : ''}>Operador</option><option value="Admin" ${user.role === 'Admin' ? 'selected' : ''}>Administrador</option></select><button class="access-toggle ${user.isActive ? '' : 'activate'}" data-user-active="${user.id}" data-active="${user.isActive}">${user.isActive ? 'Desactivar' : 'Activar'}</button>`}</div></td></tr>`; }).join('');
    document.querySelectorAll('[data-user-role]').forEach(select => select.addEventListener('change', () => updateUserAccess(select.dataset.userRole, select.value, select.dataset.userIsActive === 'true')));
    document.querySelectorAll('[data-user-active]').forEach(button => button.addEventListener('click', () => { const role = document.querySelector(`[data-user-role="${button.dataset.userActive}"]`).value; updateUserAccess(button.dataset.userActive, role, button.dataset.active !== 'true'); }));
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

async function updateUserAccess(id, role, isActive) {
  try { await api(`/api/auth/users/${id}/access`, { method: 'PUT', body: JSON.stringify({ role, isActive }) }); await loadUsers(); toast('Acceso actualizado correctamente.'); } catch (err) { toast(err.message); await loadUsers(); }
}

async function loadCategories() {
  setLoading(true);
  try {
    state.categories = await (await api('/api/categories')).json();
    $('#categories-body').innerHTML = state.categories.length ? state.categories.map(item => `<tr><td><div class="product-name"><span class="product-symbol">${escapeHtml(item.name[0])}</span><strong>${escapeHtml(item.name)}</strong></div></td><td>${escapeHtml(item.description || '—')}</td><td>${new Date(item.createdAtUtc).toLocaleDateString('es-CL')}</td><td><div class="catalog-actions"><button data-edit-category="${item.id}">Editar</button><button class="danger" data-delete-category="${item.id}">Eliminar</button></div></td></tr>`).join('') : '<tr><td colspan="4" class="empty">Todavía no hay categorías registradas.</td></tr>';
    document.querySelectorAll('[data-edit-category]').forEach(button => button.addEventListener('click', () => openCategoryDialog(state.categories.find(x => x.id === button.dataset.editCategory))));
    document.querySelectorAll('[data-delete-category]').forEach(button => button.addEventListener('click', () => deleteCatalogItem('categories', button.dataset.deleteCategory, 'categoría', loadCategories)));
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

async function loadSuppliers() {
  setLoading(true);
  try {
    state.suppliers = await (await api('/api/suppliers')).json();
    $('#suppliers-body').innerHTML = state.suppliers.length ? state.suppliers.map(item => `<tr><td><div class="product-name"><span class="product-symbol">${escapeHtml(item.name[0])}</span><strong>${escapeHtml(item.name)}</strong></div></td><td>${escapeHtml(item.email || '—')}</td><td>${escapeHtml(item.phone || '—')}</td><td>${new Date(item.createdAtUtc).toLocaleDateString('es-CL')}</td><td><div class="catalog-actions"><button data-edit-supplier="${item.id}">Editar</button><button class="danger" data-delete-supplier="${item.id}">Eliminar</button></div></td></tr>`).join('') : '<tr><td colspan="5" class="empty">Todavía no hay proveedores registrados.</td></tr>';
    document.querySelectorAll('[data-edit-supplier]').forEach(button => button.addEventListener('click', () => openSupplierDialog(state.suppliers.find(x => x.id === button.dataset.editSupplier))));
    document.querySelectorAll('[data-delete-supplier]').forEach(button => button.addEventListener('click', () => deleteCatalogItem('suppliers', button.dataset.deleteSupplier, 'proveedor', loadSuppliers)));
  } catch (err) { toast(err.message); }
  finally { setLoading(false); }
}

function openCategoryDialog(category = null) {
  const form = $('#category-form'); form.reset(); formError(form);
  form.elements.id.value = category?.id || '';
  form.elements.name.value = category?.name || '';
  form.elements.description.value = category?.description || '';
  $('#category-dialog-title').textContent = category ? 'Editar categoría' : 'Crear categoría';
  $('#category-dialog').showModal();
}

function openSupplierDialog(supplier = null) {
  const form = $('#supplier-form'); form.reset(); formError(form);
  form.elements.id.value = supplier?.id || '';
  form.elements.name.value = supplier?.name || '';
  form.elements.email.value = supplier?.email || '';
  form.elements.phone.value = supplier?.phone || '';
  $('#supplier-dialog-title').textContent = supplier ? 'Editar proveedor' : 'Crear proveedor';
  $('#supplier-dialog').showModal();
}

async function deleteCatalogItem(resource, id, label, reload) {
  if (!confirm(`¿Confirmas que deseas eliminar este ${label}?`)) return;
  try { await api(`/api/${resource}/${id}`, { method: 'DELETE' }); await reload(); toast(`${label[0].toUpperCase()}${label.slice(1)} eliminado correctamente.`); }
  catch (err) { toast(err.message); }
}

function changeView(view) {
  document.querySelectorAll('.nav-item[data-view]').forEach(item => item.classList.toggle('active', item.dataset.view === view));
  $('#dashboard-view').hidden = view !== 'dashboard'; $('#products-view').hidden = view !== 'products'; $('#categories-view').hidden = view !== 'categories'; $('#suppliers-view').hidden = view !== 'suppliers'; $('#orders-view').hidden = view !== 'orders'; $('#audit-view').hidden = view !== 'audit'; $('#users-view').hidden = view !== 'users';
  const headings = { dashboard: ['PANEL GENERAL', 'Resumen de inventario'], products: ['CATÁLOGO', 'Productos'], categories: ['CATÁLOGO', 'Categorías'], suppliers: ['ABASTECIMIENTO', 'Proveedores'], orders: ['ABASTECIMIENTO', 'Órdenes de compra'], audit: ['SEGURIDAD Y CONTROL', 'Auditoría de operaciones'], users: ['ADMINISTRACIÓN', 'Usuarios y accesos'] };
  $('#page-eyebrow').textContent = headings[view][0];
  $('#page-title').textContent = headings[view][1];
  if (view === 'products') loadProducts(); else if (view === 'categories') loadCategories(); else if (view === 'suppliers') loadSuppliers(); else if (view === 'orders') loadOrders(); else if (view === 'audit') loadAuditLogs(); else if (view === 'users') loadUsers(); else loadDashboard();
}

function escapeHtml(value) { const el = document.createElement('span'); el.textContent = value ?? ''; return el.innerHTML; }
async function loadCatalogOptions() {
  const [categories, suppliers] = await Promise.all([api('/api/categories').then(r => r.json()), api('/api/suppliers').then(r => r.json())]);
  $('#product-category').innerHTML = '<option value="">Selecciona una categoría</option>' + categories.map(x => `<option value="${x.id}">${escapeHtml(x.name)}</option>`).join('');
  $('#product-supplier').innerHTML = '<option value="">Sin proveedor</option>' + suppliers.map(x => `<option value="${x.id}">${escapeHtml(x.name)}</option>`).join('');
}
async function openProductDialog() { try { await loadCatalogOptions(); $('#product-form').reset(); $('#product-dialog').showModal(); } catch (err) { toast(err.message); } }
function openStockDialog(id, name) { const form = $('#stock-form'); form.reset(); form.elements.productId.value = id; $('#stock-product-name').textContent = name; $('#stock-dialog').showModal(); }
async function openOrderDialog() {
  try {
    const [suppliers, products] = await Promise.all([api('/api/suppliers').then(r => r.json()), api('/api/products?pageSize=100').then(r => r.json())]);
    state.orderSuppliers = suppliers; state.orderProducts = products.items;
    const form = $('#order-form'); form.reset(); formError(form);
    $('#order-supplier').innerHTML = '<option value="">Selecciona un proveedor</option>' + suppliers.map(x => `<option value="${x.id}">${escapeHtml(x.name)}</option>`).join('');
    $('#order-lines').innerHTML = ''; addOrderLine(); updateOrderTotal(); $('#order-dialog').showModal();
  } catch (err) { toast(err.message); }
}
function availableOrderProducts() { const supplierId = $('#order-supplier').value; return state.orderProducts.filter(x => !supplierId || !x.supplierId || x.supplierId === supplierId); }
function addOrderLine() {
  const line = document.createElement('div'); line.className = 'order-line';
  line.innerHTML = `<label>Producto<select name="productId" required></select></label><label>Cantidad<input name="quantity" type="number" min="1" step="1" value="1" required></label><label>Costo unitario<input name="unitCost" type="number" min="0.01" step="0.01" required></label><button type="button" class="remove-line" aria-label="Quitar producto">×</button>`;
  $('#order-lines').append(line); refreshOrderProductOptions();
  line.querySelector('[name="productId"]').addEventListener('change', event => { const product = state.orderProducts.find(x => x.id === event.target.value); if (product) line.querySelector('[name="unitCost"]').value = product.price; updateOrderTotal(); });
  line.querySelectorAll('input').forEach(input => input.addEventListener('input', updateOrderTotal));
  line.querySelector('.remove-line').addEventListener('click', () => { if ($('#order-lines').children.length > 1) line.remove(); updateOrderTotal(); });
}
function refreshOrderProductOptions() {
  const options = availableOrderProducts();
  document.querySelectorAll('#order-lines [name="productId"]').forEach(select => { const selected = select.value; select.innerHTML = '<option value="">Selecciona un producto</option>' + options.map(x => `<option value="${x.id}">${escapeHtml(x.name)} (${escapeHtml(x.sku)})</option>`).join(''); if (options.some(x => x.id === selected)) select.value = selected; });
}
function updateOrderTotal() { const total = [...document.querySelectorAll('.order-line')].reduce((sum, line) => sum + Number(line.querySelector('[name="quantity"]').value || 0) * Number(line.querySelector('[name="unitCost"]').value || 0), 0); $('#order-total').textContent = money.format(total); }
async function updateOrder(id, action, message) {
  if (!confirm(`¿Confirmas que deseas ${action === 'receive' ? 'recibir' : 'cancelar'} esta orden?`)) return;
  try { await api(`/api/purchase-orders/${id}/${action}`, { method: 'POST' }); await loadOrders(); toast(message); } catch (err) { toast(err.message); }
}
function closeDialog(id) { document.getElementById(id).close(); }
function formError(form, message = '') { const error = form.querySelector('[data-form-error]'); error.textContent = message; error.hidden = !message; }

$('#category-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const data = new FormData(form); const id = data.get('id');
    await api(id ? `/api/categories/${id}` : '/api/categories', { method: id ? 'PUT' : 'POST', body: JSON.stringify({ name: data.get('name'), description: data.get('description') || null }) });
    closeDialog('category-dialog'); await loadCategories(); toast(id ? 'Categoría actualizada correctamente.' : 'Categoría creada correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});

$('#supplier-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const data = new FormData(form); const id = data.get('id');
    await api(id ? `/api/suppliers/${id}` : '/api/suppliers', { method: id ? 'PUT' : 'POST', body: JSON.stringify({ name: data.get('name'), email: data.get('email') || null, phone: data.get('phone') || null }) });
    closeDialog('supplier-dialog'); await loadSuppliers(); toast(id ? 'Proveedor actualizado correctamente.' : 'Proveedor creado correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});

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
    closeDialog('stock-dialog'); await loadProducts(); await loadNotificationCount(); toast('Movimiento registrado correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});
$('#order-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const items = [...form.querySelectorAll('.order-line')].map(line => ({ productId: line.querySelector('[name="productId"]').value, quantity: Number(line.querySelector('[name="quantity"]').value), unitCost: Number(line.querySelector('[name="unitCost"]').value) }));
    if (new Set(items.map(x => x.productId)).size !== items.length) throw new Error('No puedes repetir un producto en la misma orden.');
    await api('/api/purchase-orders', { method: 'POST', body: JSON.stringify({ supplierId: form.elements.supplierId.value, items }) });
    closeDialog('order-dialog'); await loadOrders(); toast('Orden de compra creada correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});
$('#user-form').addEventListener('submit', async event => {
  event.preventDefault(); const form = event.currentTarget; formError(form); const submit = event.submitter; submit.disabled = true;
  try {
    const data = new FormData(form);
    await api('/api/auth/register', { method: 'POST', body: JSON.stringify({ email: data.get('email'), displayName: data.get('displayName'), password: data.get('password'), role: data.get('role') }) });
    closeDialog('user-dialog'); await loadUsers(); toast('Usuario creado correctamente.');
  } catch (err) { formError(form, err.message); } finally { submit.disabled = false; }
});
document.querySelectorAll('[data-view]').forEach(button => button.addEventListener('click', () => changeView(button.dataset.view)));
document.querySelectorAll('[data-go-products]').forEach(button => button.addEventListener('click', () => changeView('products')));
$('#logout-button').addEventListener('click', logout);
$('#refresh-button').addEventListener('click', () => { if (!$('#users-view').hidden) loadUsers(); else if (!$('#audit-view').hidden) loadAuditLogs(); else if (!$('#orders-view').hidden) loadOrders(); else if (!$('#suppliers-view').hidden) loadSuppliers(); else if (!$('#categories-view').hidden) loadCategories(); else if (!$('#products-view').hidden) loadProducts(); else loadDashboard(); });
let searchTimer; $('#product-search').addEventListener('input', event => { clearTimeout(searchTimer); searchTimer = setTimeout(() => { state.search = event.target.value.trim(); state.page = 1; loadProducts(); }, 350); });
$('#low-stock-filter').addEventListener('change', event => { state.lowStock = event.target.checked; state.page = 1; loadProducts(); });
$('#previous-page').addEventListener('click', () => { if (state.page > 1) { state.page--; loadProducts(); } });
$('#next-page').addEventListener('click', () => { if (state.page < state.totalPages) { state.page++; loadProducts(); } });
$('#export-button').addEventListener('click', async () => { try { const params = new URLSearchParams(); if (state.search) params.set('search', state.search); if (state.lowStock) params.set('lowStock', 'true'); const response = await api(`/api/reports/inventory.csv?${params}`); const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'inventario.csv'; anchor.click(); URL.revokeObjectURL(url); } catch (err) { toast(err.message); } });
$('#new-product-button').addEventListener('click', openProductDialog);
$('#new-category-button').addEventListener('click', () => openCategoryDialog());
$('#new-supplier-button').addEventListener('click', () => openSupplierDialog());
$('#new-order-button').addEventListener('click', openOrderDialog);
$('#new-user-button').addEventListener('click', () => { const form = $('#user-form'); form.reset(); formError(form); $('#user-dialog').showModal(); });
$('#notifications-button').addEventListener('click', openNotifications);
$('#add-order-line').addEventListener('click', addOrderLine);
$('#order-supplier').addEventListener('change', refreshOrderProductOptions);
$('#order-status-filter').addEventListener('change', loadOrders);
$('#audit-method-filter').addEventListener('change', () => { state.auditPage = 1; loadAuditLogs(); });
$('#audit-previous').addEventListener('click', () => { if (state.auditPage > 1) { state.auditPage--; loadAuditLogs(); } });
$('#audit-next').addEventListener('click', () => { if (state.auditPage < state.auditTotalPages) { state.auditPage++; loadAuditLogs(); } });
document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => closeDialog(button.dataset.closeDialog)));

if (state.token) showApp();
