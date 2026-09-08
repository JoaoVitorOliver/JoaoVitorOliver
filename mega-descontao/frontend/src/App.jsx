import { useState } from 'react'
import FilterBar from './components/FilterBar'
import Footer from './components/Footer'
import Header from './components/Header'
import OfferGrid from './components/OfferGrid'
import Pagination from './components/Pagination'
import { useDebouncedValue } from './hooks/useDebouncedValue'
import { useFilterOptions } from './hooks/useFilterOptions'
import { useOffers } from './hooks/useOffers'

const PAGE_SIZE = 12
const DEFAULT_SORT = 'recentes'

export default function App() {
  const [searchInput, setSearchInput] = useState('')
  const [store, setStore] = useState('')
  const [category, setCategory] = useState('')
  const [sort, setSort] = useState(DEFAULT_SORT)
  const [page, setPage] = useState(1)

  const search = useDebouncedValue(searchInput)
  const { stores, categories } = useFilterOptions()
  const { items, total, totalPages, loading, error, reload } = useOffers({
    search,
    store,
    category,
    sort,
    page,
    pageSize: PAGE_SIZE,
  })

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
      <Header search={searchInput} onSearchChange={changeFilter(setSearchInput)} />

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
          {loading
            ? 'Buscando ofertas...'
            : `${total} ${total === 1 ? 'oferta encontrada' : 'ofertas encontradas'}`}
        </p>

        <OfferGrid offers={items} loading={loading} error={error} onRetry={reload} />

        <Pagination page={page} totalPages={totalPages} onChange={changePage} />
      </main>

      <Footer />
    </div>
  )
}
