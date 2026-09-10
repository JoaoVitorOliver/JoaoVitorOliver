import { API_URL } from '../api'

const KEY_STORAGE = 'megadescontao.adminKey'

// sessionStorage e não localStorage: a chave some quando a aba fecha. É a chave que dá
// acesso de escrita ao catálogo — não deve ficar guardada no navegador para sempre.
export function getAdminKey() {
  try {
    return sessionStorage.getItem(KEY_STORAGE) ?? ''
  } catch {
    return ''
  }
}

export function setAdminKey(key) {
  try {
    if (key) {
      sessionStorage.setItem(KEY_STORAGE, key)
    } else {
      sessionStorage.removeItem(KEY_STORAGE)
    }
  } catch {
    // Navegador com armazenamento bloqueado: a chave vale só para esta sessão em memória.
  }
}

async function request(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    // Manda o cookie de sessão: quem entrou como administrador não precisa de chave.
    credentials: 'include',
    headers: {
      'X-Admin-Key': getAdminKey(),
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      ...options.headers,
    },
  })

  if (response.status === 401) {
    throw new Error('Chave administrativa inválida.')
  }

  if (response.status === 503) {
    throw new Error('A administração está desligada: configure Admin:ApiKey no servidor.')
  }

  if (!response.ok) {
    throw new Error(`A API respondeu ${response.status}`)
  }

  return response.status === 204 ? null : response.json()
}

export function fetchAdminProducts(page = 1, pageSize = 100) {
  return request(`/api/admin/products?page=${page}&pageSize=${pageSize}`)
}

export function enrichProducts(items) {
  return request('/api/admin/products/enrich', {
    method: 'POST',
    body: JSON.stringify(items),
  })
}
