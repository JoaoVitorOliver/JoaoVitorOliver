const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

export function formatPrice(value) {
  return currencyFormatter.format(value ?? 0)
}

const STORE_STYLES = {
  'Mercado Livre': { background: '#ffe600', color: '#2d3277' },
  Shopee: { background: '#ee4d2d', color: '#ffffff' },
  Temu: { background: '#fb7701', color: '#ffffff' },
  Amazon: { background: '#232f3e', color: '#ff9900' },
  AliExpress: { background: '#e62e04', color: '#ffffff' },
  Magalu: { background: '#0086ff', color: '#ffffff' },
}

export function storeStyle(store) {
  return STORE_STYLES[store] ?? { background: '#1f2937', color: '#ffffff' }
}
