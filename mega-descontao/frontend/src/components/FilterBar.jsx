const SORT_OPTIONS = [
  { value: 'recentes', label: 'Mais recentes' },
  { value: 'maior-desconto', label: 'Maior desconto' },
  { value: 'menor-preco', label: 'Menor preço' },
  { value: 'maior-preco', label: 'Maior preço' },
  { value: 'populares', label: 'Mais clicadas' },
]

export default function FilterBar({
  stores,
  categories,
  store,
  category,
  sort,
  onStoreChange,
  onCategoryChange,
  onSortChange,
  onClear,
  hasActiveFilters,
}) {
  return (
    <section className="filters" aria-label="Filtros">
      <div className="filters__stores" role="group" aria-label="Filtrar por loja">
        <button
          type="button"
          className={`chip ${store === '' ? 'chip--active' : ''}`}
          onClick={() => onStoreChange('')}
        >
          Todas as lojas
        </button>

        {stores.map((name) => (
          <button
            key={name}
            type="button"
            className={`chip ${store === name ? 'chip--active' : ''}`}
            onClick={() => onStoreChange(name)}
          >
            {name}
          </button>
        ))}
      </div>

      <div className="filters__selects">
        <label className="field">
          <span className="field__label">Categoria</span>
          <select value={category} onChange={(event) => onCategoryChange(event.target.value)}>
            <option value="">Todas</option>
            {categories.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </label>

        <label className="field">
          <span className="field__label">Ordenar por</span>
          <select value={sort} onChange={(event) => onSortChange(event.target.value)}>
            {SORT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        {hasActiveFilters && (
          <button type="button" className="link-button" onClick={onClear}>
            Limpar filtros
          </button>
        )}
      </div>
    </section>
  )
}
