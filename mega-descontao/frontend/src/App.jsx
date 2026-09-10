import { useEffect, useState } from 'react'
import FilterBar from './components/FilterBar'
import Footer from './components/Footer'
import Header from './components/Header'
import Pagination from './components/Pagination'
import ProductGrid from './components/ProductGrid'
import AuthDialog from './auth/AuthDialog'
import { useAuth } from './auth/useAuth'
import { useDebouncedValue } from './hooks/useDebouncedValue'
import { useFilterOptions } from './hooks/useFilterOptions'
import { useProducts } from './hooks/useProducts'

const PAGE_SIZE = 12
const DEFAULT_SORT = 'recentes'

export default function App() {
  const [searchInput, setSearchInput] = useState('')
  const [store, setStore] = useState('')
  const [category, setCategory] = useState('')
  const [sort, setSort] = useState(DEFAULT_SORT)
  const [page, setPage] = useState(1)

  const [authAberto, setAuthAberto] = useState(false)

  const { user, entrar, cadastrar, sair } = useAuth()
  const search = useDebouncedValue(searchInput)
  const { stores, categories } = useFilterOptions()
  const { items, total, totalPages, loading, error, reload } = useProducts({
    search,
    store,
    category,
    sort,
    page,
    pageSize: PAGE_SIZE,
  })

  // Se o catálogo encolheu (filtro novo, oferta expirada), volta para a última página real
  // em vez de deixar o usuário preso numa página vazia.
  useEffect(() => {
    if (!loading && totalPages > 0 && page > totalPages) {
      setPage(totalPages)
    }
  }, [loading, totalPages, page])

  function changeFilter(setter) {
    return (value) => {
      setter(value)
      setPage(1)
    }
  }

  function clearFilters() {
    setSearchInput('')
    setStore('')
    setCategory('')
    setSort(DEFAULT_SORT)
    setPage(1)
  }

  function changePage(nextPage) {
    setPage(nextPage)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const hasActiveFilters = Boolean(searchInput || store || category) || sort !== DEFAULT_SORT

  return (
    <div className="app">
      <Header
        search={searchInput}
        onSearchChange={changeFilter(setSearchInput)}
        user={user}
        onEntrar={() => setAuthAberto(true)}
        onSair={sair}
      />

      <AuthDialog
        open={authAberto}
        onClose={() => setAuthAberto(false)}
        onLogin={entrar}
        onRegister={cadastrar}
      />

      <main className="container main">
        <FilterBar
          stores={stores}
          categories={categories}
          store={store}
          category={category}
          sort={sort}
          onStoreChange={changeFilter(setStore)}
          onCategoryChange={changeFilter(setCategory)}
          onSortChange={changeFilter(setSort)}
          onClear={clearFilters}
          hasActiveFilters={hasActiveFilters}
        />

        <p className="results" aria-live="polite">
          {error
            ? 'Não foi possível carregar as ofertas.'
            : loading
              ? 'Buscando ofertas...'
              : `${total} ${total === 1 ? 'oferta encontrada' : 'ofertas encontradas'}`}
        </p>

        <ProductGrid products={items} loading={loading} error={error} onRetry={reload} />

        <Pagination page={page} totalPages={totalPages} loading={loading} onChange={changePage} />
      </main>

      <Footer />
    </div>
  )
}
