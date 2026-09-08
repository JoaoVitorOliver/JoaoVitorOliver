export default function Header({ search, onSearchChange }) {
  return (
    <header className="header">
      <div className="container header__inner">
        <div className="brand">
          <span className="brand__mark" aria-hidden="true">
            %
          </span>
          <span className="brand__text">
            <strong>Mega Descontão</strong>
            <small>as melhores promoções dos maiores marketplaces</small>
          </span>
        </div>

        <div className="search">
          <span className="search__icon" aria-hidden="true">
            ⌕
          </span>
          <input
            type="search"
            className="search__input"
            placeholder="O que você está procurando?"
            aria-label="Buscar ofertas"
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
          />
        </div>
      </div>
    </header>
  )
}
