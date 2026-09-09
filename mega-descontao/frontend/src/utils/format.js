const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

export function formatPrice(value) {
  return currencyFormatter.format(value ?? 0)
}

// Cores das lojas que o site cobre hoje. Loja sem entrada aqui cai no cinza do fallback,
// então adicionar um marketplace novo não quebra o card — só perde a cor da marca.
const STORE_STYLES = {
  'Mercado Livre': { background: '#ffe600', color: '#2d3277' },
  Shopee: { background: '#ee4d2d', color: '#ffffff' },
}

export function storeStyle(store) {
  return STORE_STYLES[store] ?? { background: '#1f2937', color: '#ffffff' }
}
