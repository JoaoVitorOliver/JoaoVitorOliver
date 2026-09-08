import { useState } from 'react'
import { buildGoUrl } from '../api'
import { formatPrice, storeStyle } from '../utils/format'

export default function OfferCard({ offer }) {
  const [imageFailed, setImageFailed] = useState(false)
  const hasDiscount = offer.discountPercentage > 0

  return (
    <article className="card">
      <div className="card__media">
        {imageFailed || !offer.imageUrl ? (
          <div className="card__media-fallback" aria-hidden="true">
            🏷️
          </div>
        ) : (
          <img
            src={offer.imageUrl}
            alt={offer.title}
            loading="lazy"
            onError={() => setImageFailed(true)}
          />
        )}

        {hasDiscount && <span className="badge badge--discount">-{offer.discountPercentage}%</span>}

        <span className="badge badge--store" style={storeStyle(offer.store)}>
          {offer.store}
        </span>
      </div>

      <div className="card__body">
        <span className="card__category">{offer.category}</span>
        <h3 className="card__title" title={offer.title}>
          {offer.title}
        </h3>

        <div className="card__prices">
          {offer.originalPrice != null && (
            <span className="card__price-old">{formatPrice(offer.originalPrice)}</span>
          )}
          <span className="card__price">{formatPrice(offer.price)}</span>
        </div>
      </div>

      <div className="card__footer">
        <a
          className="cta"
          href={buildGoUrl(offer.id)}
          target="_blank"
          rel="noopener noreferrer nofollow sponsored"
        >
          Aproveitar oferta
        </a>

        {offer.clickCount > 0 && (
          <span className="card__clicks">{offer.clickCount} cliques</span>
        )}
      </div>
    </article>
  )
}
