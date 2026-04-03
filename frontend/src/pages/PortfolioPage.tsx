import React from 'react';
import { useAuth } from '../hooks/useAuth';


interface PortfolioPosition {
  symbol: string;
  quantity: number;
  currentPrice: number;
  allocation: string;
}

const samplePortfolio: PortfolioPosition[] = [
  { symbol: 'AAPL', quantity: 12, currentPrice: 174.82, allocation: '24%' },
  { symbol: 'MSFT', quantity: 8, currentPrice: 329.74, allocation: '18%' },
  { symbol: 'TSLA', quantity: 6, currentPrice: 241.12, allocation: '14%' },
  { symbol: 'NVDA', quantity: 4, currentPrice: 915.45, allocation: '16%' },
];

export const PortfolioPage: React.FC = () => {
  const auth = useAuth();

  return (
    <div className="portfolio-page">
      <div className="portfolio-header">
        <div>
          <h1>Portfolio</h1>
          <p>Witaj, jesteś zalogowany jako użytkownik z tokenem.</p>
        </div>
        <p className="portfolio-token">Token: {auth.token ? auth.token.slice(0, 24) + '...' : 'brak'}</p>
      </div>

      <section className="portfolio-summary">
        <h2>Twoje pozycje</h2>
        <div className="portfolio-grid">
          {samplePortfolio.map((position) => (
            <article key={position.symbol} className="portfolio-card">
              <header>
                <h3>{position.symbol}</h3>
                <span>{position.allocation}</span>
              </header>
              <div className="portfolio-details">
                <p>Ilość: {position.quantity}</p>
                <p>Cena: ${position.currentPrice.toFixed(2)}</p>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
};
