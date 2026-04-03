import React, { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { apiService } from '../services/ApiService';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { MarketAsset } from '../types';


export const MarketPage: React.FC = () => {
  const [searchSymbol, setSearchSymbol] = useState('');
  const [searchedAsset, setSearchedAsset] = useState<MarketAsset | null>(null);
  const [searchError, setSearchError] = useState<string | null>(null);

  const { data: assets, loading, error, refetch } = useApi<MarketAsset[]>(
    () => apiService.getAllAssets()
  );

  const handleSearch = async () => {
    if (!searchSymbol.trim()) {
      setSearchError('Please enter a symbol');
      return;
    }

    setSearchError(null);
    const response = await apiService.getAssetBySymbol(searchSymbol.toUpperCase());

    if (response.error) {
      setSearchError(response.error.message);
      setSearchedAsset(null);
    } else {
      setSearchedAsset(response.data || null);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading market data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        message={`Failed to load market data: ${error.message}`}
        onRetry={refetch}
      />
    );
  }

  return (
    <div className="market-page">
      <div className="market-header">
        <h1>Market Data</h1>
        <button className="refresh-button" onClick={refetch}>
          Refresh
        </button>
      </div>

      {/* Search Section */}
      <div className="search-section">
        <h2>Search Asset</h2>
        <div className="search-container">
          <input
            type="text"
            placeholder="Enter symbol (e.g., AAPL, NVDA)"
            value={searchSymbol}
            onChange={(e) => setSearchSymbol(e.target.value.toUpperCase())}
            onKeyPress={handleKeyPress}
            className="search-input"
          />
          <button onClick={handleSearch} className="search-button">
            Search
          </button>
        </div>
        {searchError && <p className="search-error">{searchError}</p>}

        {searchedAsset && (
          <div className="searched-asset">
            <h3>Search Result</h3>
            <AssetCard asset={searchedAsset} />
          </div>
        )}
      </div>

      {/* All Assets Section */}
      <div className="assets-section">
        <h2>All Assets</h2>
        {assets && assets.length > 0 ? (
          <div className="assets-grid">
            {assets.map((asset) => (
              <AssetCard key={asset.symbol} asset={asset} />
            ))}
          </div>
        ) : (
          <p className="no-data">No market data available</p>
        )}
      </div>
    </div>
  );
};

interface AssetCardProps {
  asset: MarketAsset;
}

const AssetCard: React.FC<AssetCardProps> = ({ asset }) => {
  const changeColor = asset.changePercent >= 0 ? 'positive' : 'negative';

  return (
    <div className="asset-card">
      <div className="asset-header">
        <h3 className="asset-symbol">{asset.symbol}</h3>
        <span className={`asset-change ${changeColor}`}>
          {asset.changePercent >= 0 ? '+' : ''}{asset.changePercent.toFixed(2)}%
        </span>
      </div>

      <div className="asset-details">
        <p className="asset-name">{asset.name}</p>
        <div className="asset-price">
          <span className="price-value">
            {asset.currency} {asset.price.toFixed(2)}
          </span>
        </div>
      </div>
    </div>
  );
};
