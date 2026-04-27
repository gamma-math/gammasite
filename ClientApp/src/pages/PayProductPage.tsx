import { useEffect, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { apiFetch } from '../api/client';
import { useCurrentUser } from '../hooks/useCurrentUser';

interface ProductDetail {
  id: string;
  name: string;
  description: string;
  priceOre: number | null;
  currency: string | null;
  metadata: Record<string, string>;
}

function formatPrice(ore: number | null, currency: string | null): string {
  if (ore == null) return '—';
  return new Intl.NumberFormat('da-DK', {
    style: 'currency',
    currency: currency?.toUpperCase() ?? 'DKK',
    minimumFractionDigits: 0,
  }).format(ore / 100);
}

export default function PayProductPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!id) return;
    apiFetch<ProductDetail>(`/api/pay/products/${id}`)
      .then(setProduct)
      .catch(err => setError((err as Error).message));
  }, [id]);

  const handleCheckout = async () => {
    if (!product || !user) return;
    setError(null);
    setLoading(true);
    try {
      const data = await apiFetch<{ id: string; url: string }>(
        `/api/Stripe/Product?product=${product.id}&user=${user.id}`,
        { method: 'POST' }
      );
      window.location.href = data.url;
    } catch (err) {
      setError((err as Error).message);
      setLoading(false);
    }
  };

  if (error && !product) return <p>Fejl: {error}</p>;
  if (!product) return <p>Henter produkt...</p>;

  return (
    <>
      <h1>Detaljer om {product.name.toLowerCase()}</h1>
      <button className="btn btn-secondary mb-3" onClick={() => navigate('/pay')}>
        Tilbage
      </button>

      <table className="table table-striped table-bordered mb-3">
        <thead className="table-dark">
          <tr>
            <th>Navn</th>
            <th>Beskrivelse</th>
            <th>Pris</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>{product.name}</td>
            <td>{product.description}</td>
            <td>{formatPrice(product.priceOre, product.currency)}</td>
          </tr>
        </tbody>
      </table>

      {product.metadata['Additional'] && (
        <p>{product.metadata['Additional']}</p>
      )}
      {product.metadata['Conditions'] && (
        <p>Læs <em><Link to={product.metadata['Conditions']}>Betingelser</Link></em></p>
      )}

      {error && <div className="alert alert-danger">{error}</div>}

      <button
        className="btn btn-primary"
        id="checkout-button"
        onClick={handleCheckout}
        disabled={loading || !user}
      >
        {loading ? 'Opretter betaling...' : 'Betal'}
      </button>
    </>
  );
}
