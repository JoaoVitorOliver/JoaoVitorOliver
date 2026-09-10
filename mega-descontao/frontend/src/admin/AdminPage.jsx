import { useCallback, useEffect, useState } from 'react'
import { formatPrice } from '../utils/format'
import { useAuth } from '../auth/useAuth'
import { enrichProducts, fetchAdminProducts, getAdminKey, setAdminKey } from './adminApi'

/// Tela de curadoria: completa o que o CSV de afiliado não traz — foto e preço de antes.
/// Fica de pé até a Open API passar a trazer esses campos sozinha.
export default function AdminPage() {
  const { user, checking } = useAuth()
  const [key, setKey] = useState(getAdminKey())
  const [authenticated, setAuthenticated] = useState(Boolean(getAdminKey()))
  const [products, setProducts] = useState([])
  const [edits, setEdits] = useState({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [result, setResult] = useState(null)
  const [onlyIncomplete, setOnlyIncomplete] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const data = await fetchAdminProducts()
      setProducts(data.items)
      setEdits({})
      setAuthenticated(true)
    } catch (err) {
      setError(err.message)
      setAuthenticated(false)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (authenticated || user?.isAdmin) {
      load()
    }
  }, [authenticated, user, load])

  function entrar(event) {
    event.preventDefault()
    setAdminKey(key.trim())
    setAuthenticated(true)
  }

  function editar(productId, campo, valor) {
    setEdits((atual) => ({
      ...atual,
      [productId]: { ...atual[productId], [campo]: valor },
    }))
  }

  async function salvar() {
    const items = Object.entries(edits)
      .map(([productId, campos]) => {
        const original = campos.originalPrice?.toString().trim().replace(',', '.')

        return {
          productId: Number(productId),
          imageUrl: campos.imageUrl?.trim() || null,
          originalPrice: original ? Number(original) : null,
        }
      })
      .filter((item) => item.imageUrl || item.originalPrice)

    if (items.length === 0) {
      return
    }

    setLoading(true)
    setError(null)

    try {
      setResult(await enrichProducts(items))
      await load()
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const incompleto = (p) => !p.imageUrl || !p.offers.some((o) => o.originalPrice)
  const visiveis = onlyIncomplete ? products.filter(incompleto) : products
  const pendentes = Object.keys(edits).length

  if (checking) {
    return <div className="admin admin--login" />
  }

  // Quem entrou como administrador não precisa de chave nenhuma: a sessão já autoriza.
  if (!authenticated && !user?.isAdmin) {
    return (
      <div className="admin admin--login">
        <form className="admin__login" onSubmit={entrar}>
          <h1 className="admin__title">Curadoria</h1>
          <p className="admin__hint">
            Entre com sua conta de administrador na vitrine, ou informe a chave configurada no
            servidor.
          </p>
          <input
            type="password"
            className="search__input"
            value={key}
            onChange={(e) => setKey(e.target.value)}
            placeholder="Chave administrativa"
            autoFocus
          />
          <button type="submit" className="cta">
            Entrar
          </button>
          {error && <p className="admin__error">{error}</p>}
        </form>
      </div>
    )
  }

  return (
    <div className="admin">
      <header className="admin__header">
        <div>
          <h1 className="admin__title">Curadoria do catálogo</h1>
          <p className="admin__hint">
            Complete a foto e o preço de antes — o CSV de afiliado não traz esses dois campos.
          </p>
        </div>

        <div className="admin__actions">
          <label className="admin__toggle">
            <input
              type="checkbox"
              checked={onlyIncomplete}
              onChange={(e) => setOnlyIncomplete(e.target.checked)}
            />
            Só os incompletos
          </label>

          <a className="link-button" href="#/">
            ← Voltar à vitrine
          </a>

          <button type="button" className="cta" onClick={salvar} disabled={loading || pendentes === 0}>
            {loading ? 'Salvando...' : `Salvar ${pendentes || ''}`.trim()}
          </button>
        </div>
      </header>

      {error && <p className="admin__error">{error}</p>}

      {result && (
        <div className="admin__result">
          <strong>
            {result.imagesUpdated} foto(s) e {result.pricesUpdated} preço(s) atualizados.
          </strong>
          {result.warnings.map((w) => (
            <p key={w} className="admin__warning">
              {w}
            </p>
          ))}
        </div>
      )}

      <p className="results">
        {visiveis.length} produto(s) {onlyIncomplete ? 'sem foto ou sem preço de antes' : 'no catálogo'}
      </p>

      <div className="admin__list">
        {visiveis.map((product) => {
          const offer = product.offers[0]
          const edit = edits[product.id] ?? {}
          const preview = edit.imageUrl ?? product.imageUrl

          return (
            <article key={product.id} className="admin__row">
              <div className="admin__thumb">
                {preview ? (
                  <img src={preview} alt="" loading="lazy" />
                ) : (
                  <span aria-hidden="true">🏷️</span>
                )}
              </div>

              <div className="admin__info">
                <strong className="admin__product">{product.title}</strong>
                <span className="admin__meta">
                  {offer ? `${offer.storeName} · ${formatPrice(offer.currentPrice)}` : 'sem oferta'}
                  {offer?.originalPrice ? ` · de ${formatPrice(offer.originalPrice)}` : ''}
                </span>
              </div>

              <label className="admin__field">
                <span className="field__label">URL da foto</span>
                <input
                  type="url"
                  value={edit.imageUrl ?? product.imageUrl ?? ''}
                  placeholder="https://..."
                  onChange={(e) => editar(product.id, 'imageUrl', e.target.value)}
                />
              </label>

              <label className="admin__field admin__field--price">
                <span className="field__label">Preço de antes</span>
                <input
                  type="text"
                  inputMode="decimal"
                  value={edit.originalPrice ?? offer?.originalPrice ?? ''}
                  placeholder={offer ? `maior que ${offer.currentPrice}` : ''}
                  onChange={(e) => editar(product.id, 'originalPrice', e.target.value)}
                />
              </label>
            </article>
          )
        })}
      </div>

      {visiveis.length === 0 && !loading && (
        <div className="state">
          <h2 className="state__title">Tudo completo</h2>
          <p className="state__text">Todos os produtos têm foto e preço de antes.</p>
        </div>
      )}
    </div>
  )
}
