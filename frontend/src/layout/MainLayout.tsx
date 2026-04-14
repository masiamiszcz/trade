import React, { useState } from 'react';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { Footer } from './Footer';
import { Navigation } from '../components/Navigation';
import './MainLayout.css';


interface MainLayoutProps {
  children: React.ReactNode;
  showSidebar?: boolean;
  sidebarContent?: React.ReactNode;
  isDashboard?: boolean;
}

export const MainLayout: React.FC<MainLayoutProps> = ({
  children,
  showSidebar = false,
  sidebarContent,
  isDashboard = false
}) => {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen);
  };

  return (
    <div className="main-layout">
      <Header>
        <Navigation onMenuClick={showSidebar ? toggleSidebar : undefined} />
      </Header>

      <div className="layout-body">
        {showSidebar && (
          <Sidebar isOpen={sidebarOpen}>
            {sidebarContent}
          </Sidebar>
        )}

        <main className={`main-content ${showSidebar ? 'with-sidebar' : ''}`}>
          <div className={`content-wrapper ${isDashboard ? 'dashboard-fullscreen' : ''}`}>
            {children}
          </div>
        </main>
      </div>

      <Footer />
    </div>
  );
};
