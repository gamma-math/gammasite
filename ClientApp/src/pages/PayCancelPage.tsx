import { Link } from 'react-router-dom';

export default function PayCancelPage() {
  return (
    <>
      <h1>Køb annulleret</h1>
      <p>Dit køb blev annulleret.</p>
      <Link className="btn btn-secondary" to="/pay">Til oversigten</Link>
    </>
  );
}
