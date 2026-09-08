import { API_URL } from '../api'
import ProductCard from './ProductCard'

const SKELETON_COUNT = 8

export default function ProductGrid({ products, loading, error, onRetry }) {
  if (loading) {
    return (
      <div className="grid">
        {Array.from({ length: SKELETON_COUNT }, (_, index) => (
          <div key={index} className="card card--skeleton" aria-hidden="true">
            <div className="skeleton skeleton--media" />
            <div className="skeleton skeleton--line" />
            <div className="skeleton skeleton--line skeleton--short" />
            <div className="skeleton skeleton--button" />
          </div>
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <div className="state" role="alert">
        <h2 className="state__title">Não conseguimos carregar as ofertas</h2>
        <p className="state__text">
          Verifique se a API está rodando em <code>{API_URL}</code>.
        </p>
        <button type="button" className="cta cta--inline" onClick={onRetry}>
          Tentar novamente
        </button>
      </div>
    )
  }

  if (products.length === 0) {
    return (
      <div className="state">
        <h2 className="state__title">Nenhuma oferta por aqui</h2>
        <p className="state__text">Tente outra busca ou remova alguns filtros.</p>
      </div>
    )
  }

  return (
    <div className="grid">
      {products.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  )
}
