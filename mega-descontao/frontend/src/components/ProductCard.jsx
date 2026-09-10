import { useState } from 'react'
import { buildGoUrl } from '../api'
import { formatPrice, storeStyle } from '../utils/format'

export default function ProductCard({ product }) {
  const [imageFailed, setImageFailed] = useState(false)

  const offer = product.bestOffer
  const hasDiscount = offer.discountPercentage > 0
  const otherStores = product.offerCount - 1

  return (
    <article className="card">
      <div className="card__media">
        {imageFailed || !product.imageUrl ? (
          <div className="card__media-fallback" aria-hidden="true">
            🏷️
          </div>
        ) : (
          <img
            src={product.imageUrl}
            alt={product.title}
            loading="lazy"
            onError={() => setImageFailed(true)}
          />
        )}

        {hasDiscount ? (
          <span className="badge badge--discount">-{offer.discountPercentage}%</span>
        ) : (
          offer.isLowestIn30Days && (
            <span className="badge badge--lowest" title="Menor preço observado nos últimos 30 dias">
              menor preço 30d
            </span>
          )
        )}

        <span className="badge badge--store" style={storeStyle(offer.store.name)}>
          {offer.store.name}
        </span>
      </div>

      <div className="card__body">
        <span className="card__category">{product.category.name}</span>
        <h3 className="card__title" title={product.title}>
          {product.title}
        </h3>

        <div className="card__prices">
          {offer.originalPrice != null && (
            <span className="card__price-old">{formatPrice(offer.originalPrice)}</span>
          )}
          <span className="card__price">{formatPrice(offer.currentPrice)}</span>
        </div>
      </div>

      <div className="card__footer">
        <a
          className="cta"
          href={buildGoUrl(offer.offerId)}
          target="_blank"
          rel="noopener noreferrer nofollow sponsored"
        >
          Aproveitar oferta
        </a>

        <span className="card__clicks">
          {otherStores > 0 && (
            <>
              também em {otherStores === 1 ? 'mais 1 loja' : `mais ${otherStores} lojas`}
              {offer.clickCount > 0 && ' · '}
            </>
          )}
          {offer.clickCount > 0 && `${offer.clickCount} cliques`}
        </span>
      </div>
    </article>
  )
}
