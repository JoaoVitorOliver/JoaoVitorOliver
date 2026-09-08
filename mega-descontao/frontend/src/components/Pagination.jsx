export default function Pagination({ page, totalPages, loading, onChange }) {
  if (totalPages <= 1) {
    return null
  }

  // Enquanto a busca está no ar, totalPages ainda é o do resultado anterior. Travar os
  // botões evita levar o usuário para uma página que não existe mais no novo filtro.
  return (
    <nav className="pagination" aria-label="Paginação">
      <button
        type="button"
        className="pagination__button"
        onClick={() => onChange(page - 1)}
        disabled={loading || page <= 1}
      >
        ← Anterior
      </button>

      <span className="pagination__status">
        Página {page} de {totalPages}
      </span>

      <button
        type="button"
        className="pagination__button"
        onClick={() => onChange(page + 1)}
        disabled={loading || page >= totalPages}
      >
        Próxima →
      </button>
    </nav>
  )
}
