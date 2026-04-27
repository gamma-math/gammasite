import { Link } from 'react-router-dom';

export default function PaySuccessPage() {
  return (
    <>
      <h1>Køb gennemført</h1>
      <p>Dit køb blev gennemført med success!</p>
      <Link className="btn btn-secondary" to="/pay">Til oversigten</Link>
    </>
  );
}
