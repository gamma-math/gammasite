import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiFetch } from '../api/client';

interface Product {
  id: string;
  name: string;
  description: string;
  priceOre: number | null;
  currency: string | null;
}

interface ProductsResponse {
  publicKey: string;
  products: Product[];
}

function formatPrice(ore: number | null, currency: string | null): string {
  if (ore == null) return '—';
  const amount = ore / 100;
  return new Intl.NumberFormat('da-DK', {
    style: 'currency',
    currency: currency?.toUpperCase() ?? 'DKK',
    minimumFractionDigits: 0,
  }).format(amount);
}

export default function PayPage() {
  const [data, setData] = useState<ProductsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<ProductsResponse>('/api/pay/products')
      .then(setData)
      .catch(err => setError((err as Error).message));
  }, []);

  if (error) return <p>Fejl: {error}</p>;
  if (!data) return <p>Henter produkter...</p>;

  return (
    <>
      <h1>Produkter</h1>
      <table className="table table-striped table-bordered">
        <thead className="table-dark">
          <tr>
            <th>Navn</th>
            <th>Beskrivelse</th>
            <th>Pris</th>
            <th>Produkt</th>
          </tr>
        </thead>
        <tbody>
          {data.products.map(p => (
            <tr key={p.id}>
              <td>{p.name}</td>
              <td>{p.description}</td>
              <td>{formatPrice(p.priceOre, p.currency)}</td>
              <td>
                <Link className="btn btn-sm btn-primary" to={`/app/pay/product/${p.id}`}>
                  Vælg
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
