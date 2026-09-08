import OfferCard from './OfferCard'

const SKELETON_COUNT = 8

export default function OfferGrid({ offers, loading, error, onRetry }) {
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
      <div className="state">
        <h2 className="state__title">Não conseguimos carregar as ofertas</h2>
        <p className="state__text">
          Verifique se a API está rodando em <code>{import.meta.env.VITE_API_URL ?? 'http://localhost:5080'}</code>.
        </p>
        <button type="button" className="cta cta--inline" onClick={onRetry}>
          Tentar novamente
        </button>
      </div>
    )
  }

  if (offers.length === 0) {
    return (
      <div className="state">
        <h2 className="state__title">Nenhuma oferta por aqui</h2>
        <p className="state__text">Tente outra busca ou remova alguns filtros.</p>
      </div>
    )
  }

  return (
    <div className="grid">
      {offers.map((offer) => (
        <OfferCard key={offer.id} offer={offer} />
      ))}
    </div>
  )
}
