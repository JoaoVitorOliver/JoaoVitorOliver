const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(/\/$/, '')

async function getJson(path, signal) {
  const response = await fetch(`${API_URL}${path}`, { signal })

  if (!response.ok) {
    throw new Error(`A API respondeu ${response.status}`)
  }

  return response.json()
}

export function fetchOffers({ search, store, category, sort, page, pageSize }, signal) {
  const params = new URLSearchParams()

  if (search) params.set('search', search)
  if (store) params.set('store', store)
  if (category) params.set('category', category)
  if (sort) params.set('sort', sort)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))

  return getJson(`/api/offers?${params}`, signal)
}

export function fetchFilterOptions(signal) {
  return getJson('/api/offers/filters', signal)
}

/// O clique nunca vai direto para o marketplace: passa pela API para ser contabilizado.
export function buildGoUrl(offerId) {
  return `${API_URL}/api/go/${offerId}`
}
