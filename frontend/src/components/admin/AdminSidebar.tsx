
import { useNavigate } from 'react-router-dom';
import './AdminSidebar.css';

interface AdminSidebarProps {
  activeTab: 'dashboard' | 'approvals' | 'instruments' | 'audit-logs' | 'users';
  onTabChange: (tab: 'dashboard' | 'approvals' | 'instruments' | 'audit-logs' | 'users') => void;
  onLogout: () => void;
}

export const AdminSidebar: React.FC<AdminSidebarProps> = ({ activeTab, onTabChange, onLogout }) => {
  const navigate = useNavigate();

  const menuItems = [
    { id: 'dashboard', label: '📊 Dashboard', icon: '📊' },
    { id: 'approvals', label: '✅ Zatwierdzenia', icon: '✅' },
    { id: 'instruments', label: '🛠️ Instrumenty', icon: '🛠️' },
    { id: 'audit-logs', label: '📋 Historia', icon: '📋' },
    { id: 'users', label: '👥 Użytkownicy', icon: '👥' },
  ] as const;

  return (
    <aside className="admin-sidebar">
      <div className="sidebar-header">
        <h2>🔐 Admin Panel</h2>
      </div>

      <nav className="sidebar-nav">
        {menuItems.map((item) => (
          <button
            key={item.id}
            className={`nav-item ${activeTab === item.id ? 'active' : ''}`}
            onClick={() => onTabChange(item.id)}
          >
            <span className="nav-icon">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </button>
        ))}
      </nav>

      <div className="sidebar-footer">
        <button className="btn-logout" onClick={onLogout}>
          🚪 Wyloguj się
        </button>
      </div>
    </aside>
  );
};
